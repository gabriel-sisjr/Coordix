using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Coordix.Implementation
{
	/// <summary>
	/// Centralized handler execution implementation that handles handler lookup, caching, and invocation.
	/// This class is the single point of reflection for handler execution, eliminating scattered reflection
	/// throughout the codebase. All handler invocation goes through this class.
	/// </summary>
	public class HandlerExecutor : IHandlerExecutor
	{
		private readonly IServiceProvider _provider;

		/// <summary>
		/// Cache for MethodInfo objects to avoid repeated reflection calls.
		/// Key: Handler type, Value: Handle method's MethodInfo.
		/// </summary>
		private static readonly ConcurrentDictionary<Type, MethodInfo> _methodInfoCache = new ConcurrentDictionary<Type, MethodInfo>();

		/// <summary>
		/// Cache for compiled delegates for request handlers without return value.
		/// Key: Handler type, Value: Compiled delegate for invoking the handler.
		/// </summary>
		private static readonly ConcurrentDictionary<Type, Func<object, IRequest, CancellationToken, Task>> _requestHandlerDelegates = new ConcurrentDictionary<Type, Func<object, IRequest, CancellationToken, Task>>();

		/// <summary>
		/// Cache for compiled delegates for request handlers with return value.
		/// Key: Handler type, Value: Compiled delegate for invoking the handler.
		/// </summary>
		private static readonly ConcurrentDictionary<Type, Delegate> _requestHandlerWithResponseDelegates = new ConcurrentDictionary<Type, Delegate>();

		/// <summary>
		/// Cache for compiled delegates for notification handlers.
		/// Key: Handler type, Value: Compiled delegate for invoking the handler.
		/// </summary>
		private static readonly ConcurrentDictionary<Type, Func<object, INotification, CancellationToken, Task>> _notificationHandlerDelegates = new ConcurrentDictionary<Type, Func<object, INotification, CancellationToken, Task>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="HandlerExecutor"/> class using the given service provider.
		/// </summary>
		/// <param name="provider">The service provider used to resolve handler instances.</param>
		public HandlerExecutor(IServiceProvider provider)
		{
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}

		/// <inheritdoc />
		public async Task<TResponse> ExecuteRequestHandler<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
		{
			var requestType = request.GetType();
			var responseType = typeof(TResponse);
			var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);

			var handler = _provider.GetService(handlerType);

			if (handler is null)
			{
				throw new InvalidOperationException($"Handler not found for {requestType.Name}");
			}

			var response = await InvokeRequestHandlerWithResponse<TResponse>(handlerType, handler, request, cancellationToken);
			return response;
		}

		/// <inheritdoc />
		public async Task ExecuteRequestHandler(IRequest request, CancellationToken cancellationToken = default)
		{
			var requestType = request.GetType();
			var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);

			var handler = _provider.GetService(handlerType);

			if (handler is null)
			{
				throw new InvalidOperationException($"Handler not found for {requestType.Name}");
			}

			await InvokeRequestHandler(handlerType, handler, request, cancellationToken);
		}

		/// <inheritdoc />
		public async Task ExecuteNotificationHandler<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
			where TNotification : INotification
		{
			var notificationType = notification.GetType();
			var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
			var handlers = _provider.GetServices(handlerType).ToArray();
			var tasksHandlers = handlers.Where(h => h != null).Select(handler => InvokeNotificationHandler(handlerType, handler!, notification, cancellationToken));

			await Task.WhenAll(tasksHandlers);
		}

		/// <summary>
		/// Gets or creates a cached MethodInfo for the Handle method of a handler type.
		/// This method ensures that reflection is performed only once per handler type,
		/// with subsequent calls returning the cached MethodInfo for improved performance.
		/// </summary>
		/// <param name="handlerType">The handler type to get the MethodInfo for.</param>
		/// <returns>The cached MethodInfo for the Handle method.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the Handle method is not found on the handler type.</exception>
		private static MethodInfo GetHandleMethodInfo(Type handlerType)
		{
			return _methodInfoCache.GetOrAdd(handlerType, type => type.GetMethod("Handle")
					?? throw new InvalidOperationException($"Handle method not found on {type.Name}"));
		}

		/// <summary>
		/// Gets or creates a cached delegate for invoking a request handler without return value.
		/// </summary>
		private static Func<object, IRequest, CancellationToken, Task> GetRequestHandlerDelegate(Type handlerType)
				=> _requestHandlerDelegates.GetOrAdd(handlerType, CreateRequestHandlerDelegate);

		/// <summary>
		/// Creates a strongly-typed delegate for invoking a request handler without return value.
		/// Uses Expression Trees to compile a delegate that directly invokes the handler's Handle method,
		/// avoiding the overhead of MethodInfo.Invoke on every call.
		/// </summary>
		/// <param name="handlerType">The handler type to create a delegate for.</param>
		/// <returns>A compiled delegate that can invoke the handler's Handle method.</returns>
		private static Func<object, IRequest, CancellationToken, Task> CreateRequestHandlerDelegate(Type handlerType)
		{
			var methodInfo = GetHandleMethodInfo(handlerType);
			var handlerParam = Expression.Parameter(typeof(object), "handler");
			var requestParam = Expression.Parameter(typeof(IRequest), "request");
			var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

			// Extract the concrete request type from IRequestHandler<TRequest>
			var requestType = handlerType.GetGenericArguments()[0];

			var castHandler = Expression.Convert(handlerParam, handlerType);
			var castRequest = Expression.Convert(requestParam, requestType);
			var call = Expression.Call(castHandler, methodInfo, castRequest, cancellationTokenParam);
			var lambda = Expression.Lambda<Func<object, IRequest, CancellationToken, Task>>(call, handlerParam, requestParam, cancellationTokenParam);

			return lambda.Compile();
		}

		/// <summary>
		/// Gets or creates a cached delegate for invoking a request handler with return value.
		/// </summary>
		private static Func<object, IRequest<TResponse>, CancellationToken, Task<TResponse>> GetRequestHandlerWithResponseDelegate<TResponse>(Type handlerType)
				=> (Func<object, IRequest<TResponse>, CancellationToken, Task<TResponse>>)_requestHandlerWithResponseDelegates.GetOrAdd(handlerType, _ => CreateRequestHandlerWithResponseDelegate<TResponse>(handlerType));

		/// <summary>
		/// Creates a strongly-typed delegate for invoking a request handler with return value.
		/// Uses Expression Trees to compile a delegate that directly invokes the handler's Handle method,
		/// avoiding the overhead of MethodInfo.Invoke on every call.
		/// </summary>
		/// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
		/// <param name="handlerType">The handler type to create a delegate for.</param>
		/// <returns>A compiled delegate that can invoke the handler's Handle method and return the response.</returns>
		private static Func<object, IRequest<TResponse>, CancellationToken, Task<TResponse>> CreateRequestHandlerWithResponseDelegate<TResponse>(Type handlerType)
		{
			var methodInfo = GetHandleMethodInfo(handlerType);
			var handlerParam = Expression.Parameter(typeof(object), "handler");
			var requestParam = Expression.Parameter(typeof(IRequest<TResponse>), "request");
			var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

			// Extract the concrete request type from IRequestHandler<TRequest, TResponse>
			var requestType = handlerType.GetGenericArguments()[0];

			var castHandler = Expression.Convert(handlerParam, handlerType);
			var castRequest = Expression.Convert(requestParam, requestType);
			var call = Expression.Call(castHandler, methodInfo, castRequest, cancellationTokenParam);
			var lambda = Expression.Lambda<Func<object, IRequest<TResponse>, CancellationToken, Task<TResponse>>>(call, handlerParam, requestParam, cancellationTokenParam);

			return lambda.Compile();
		}

		/// <summary>
		/// Gets or creates a cached delegate for invoking a notification handler.
		/// </summary>
		private static Func<object, INotification, CancellationToken, Task> GetNotificationHandlerDelegate(Type handlerType)
				=> _notificationHandlerDelegates.GetOrAdd(handlerType, CreateNotificationHandlerDelegate);

		/// <summary>
		/// Creates a strongly-typed delegate for invoking a notification handler.
		/// Uses Expression Trees to compile a delegate that directly invokes the handler's Handle method,
		/// avoiding the overhead of MethodInfo.Invoke on every call.
		/// </summary>
		/// <param name="handlerType">The handler type to create a delegate for.</param>
		/// <returns>A compiled delegate that can invoke the handler's Handle method.</returns>
		private static Func<object, INotification, CancellationToken, Task> CreateNotificationHandlerDelegate(Type handlerType)
		{
			var methodInfo = GetHandleMethodInfo(handlerType);
			var handlerParam = Expression.Parameter(typeof(object), "handler");
			var notificationParam = Expression.Parameter(typeof(INotification), "notification");
			var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

			// Extract the concrete notification type from INotificationHandler<TNotification>
			var notificationType = handlerType.GetGenericArguments()[0];

			var castHandler = Expression.Convert(handlerParam, handlerType);
			var castNotification = Expression.Convert(notificationParam, notificationType);
			var call = Expression.Call(castHandler, methodInfo, castNotification, cancellationTokenParam);
			var lambda = Expression.Lambda<Func<object, INotification, CancellationToken, Task>>(call, handlerParam, notificationParam, cancellationTokenParam);

			return lambda.Compile();
		}

		/// <summary>
		/// Invokes the Handle method on a notification handler.
		/// </summary>
		/// <typeparam name="TNotification">The notification type.</typeparam>
		/// <param name="handlerType">The runtime handler interface type.</param>
		/// <param name="handler">The handler instance to invoke.</param>
		/// <param name="notification">The notification message.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task representing the handler invocation.</returns>
		private static Task InvokeNotificationHandler<TNotification>(Type handlerType, object handler, TNotification notification, CancellationToken cancellationToken)
			where TNotification : INotification
		{
			var delegateFunc = GetNotificationHandlerDelegate(handlerType);
			return delegateFunc(handler, notification, cancellationToken);
		}

		/// <summary>
		/// Invokes the Handle method on a request handler without return value.
		/// </summary>
		/// <param name="handlerType">The runtime handler interface type.</param>
		/// <param name="handler">The handler instance to invoke.</param>
		/// <param name="request">The request message.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task representing the handler invocation.</returns>
		private static Task InvokeRequestHandler(Type handlerType, object handler, IRequest request, CancellationToken cancellationToken)
		{
			var delegateFunc = GetRequestHandlerDelegate(handlerType);
			return delegateFunc(handler, request, cancellationToken);
		}

		/// <summary>
		/// Invokes the Handle method on a request handler and returns its response.
		/// </summary>
		/// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
		/// <param name="handlerType">The runtime handler interface type.</param>
		/// <param name="handler">The handler instance to invoke.</param>
		/// <param name="request">The request message.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task representing the handler invocation, containing the handler's response.</returns>
		private static Task<TResponse> InvokeRequestHandlerWithResponse<TResponse>(Type handlerType, object handler, IRequest<TResponse> request, CancellationToken cancellationToken)
		{
			var delegateFunc = GetRequestHandlerWithResponseDelegate<TResponse>(handlerType);
			return delegateFunc(handler, request, cancellationToken);
		}
	}
}

