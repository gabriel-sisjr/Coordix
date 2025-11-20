using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Coordix.Background.Implementation
{
	/// <summary>
	/// Background service that processes enqueued jobs from the channel.
	/// </summary>
	public class BackgroundWorker : BackgroundService
	{
		private readonly ChannelReader<BackgroundJob> _channelReader;
		private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly ILogger<BackgroundWorker> _logger;

		/// <summary>
		/// Cache for compiled delegates for IMediator.Send&lt;TResponse&gt; method invocations.
		/// Key: Response type, Value: Compiled delegate for invoking Send.
		/// </summary>
		private static readonly ConcurrentDictionary<Type, Delegate> _sendWithResponseDelegates = new ConcurrentDictionary<Type, Delegate>();

		/// <summary>
		/// Cache for compiled delegates for IMediator.Publish&lt;TNotification&gt; method invocations.
		/// Key: Notification type, Value: Compiled delegate for invoking Publish.
		/// </summary>
		private static readonly ConcurrentDictionary<Type, Delegate> _publishDelegates = new ConcurrentDictionary<Type, Delegate>();

		public BackgroundWorker(
			ChannelReader<BackgroundJob> channelReader,
			IServiceScopeFactory serviceScopeFactory,
			ILogger<BackgroundWorker> logger)
		{
			_channelReader = channelReader ?? throw new ArgumentNullException(nameof(channelReader));
			_serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Background worker started");

			try
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					try
					{
						if (await _channelReader.WaitToReadAsync(stoppingToken))
						{
							while (_channelReader.TryRead(out var job))
							{
								try
								{
									await ProcessJobAsync(job, stoppingToken);
								}
								catch (Exception ex)
								{
									_logger.LogError(ex, "Error processing background job: {MessageType}", job.MessageType.Name);
									// Continue processing other jobs even if one fails
								}
							}
						}
					}
					catch (OperationCanceledException)
					{
						break;
					}
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Background worker is stopping");
			}
			catch (Exception ex)
			{
				_logger.LogCritical(ex, "Background worker encountered a fatal error");
				throw;
			}
		}

		private async Task ProcessJobAsync(BackgroundJob job, CancellationToken cancellationToken)
		{
			// Create a scope for this job to ensure proper lifetime management
			// This allows handlers registered as scoped to work correctly
			using var scope = _serviceScopeFactory.CreateScope();
			var mediator = scope.ServiceProvider.GetService<IMediator>();
			if (mediator == null)
			{
				_logger.LogError("IMediator not found in service provider");
				return;
			}

			_logger.LogDebug("Processing background job: {MessageType}", job.MessageType.Name);

			try
			{
				if (job.HasResponse && job.ResponseType != null)
				{
					// Request with response - use cached delegate to avoid reflection overhead
					var sendDelegate = GetSendWithResponseDelegate(job.ResponseType);
					var task = sendDelegate(mediator, job.Message, cancellationToken);
					await task;
				}
				else if (job.Message is IRequest request)
				{
					// Request without response
					await mediator.Send(request, cancellationToken);
				}
				else if (job.Message is INotification notification)
				{
					// Notification - use cached delegate to avoid reflection overhead
					var publishDelegate = GetPublishDelegate(job.MessageType);
					var task = publishDelegate(mediator, notification, cancellationToken);
					await task;
				}

				_logger.LogDebug("Background job processed successfully: {MessageType}", job.MessageType.Name);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing background job: {MessageType}", job.MessageType.Name);
				throw; // Re-throw to be caught by the outer try-catch
			}
		}

		/// <summary>
		/// Gets or creates a cached delegate for invoking IMediator.Send&lt;TResponse&gt;.
		/// This avoids reflection overhead on every job processing.
		/// </summary>
		/// <param name="responseType">The response type.</param>
		/// <returns>A compiled delegate for invoking Send.</returns>
		private static Func<IMediator, object, CancellationToken, Task> GetSendWithResponseDelegate(Type responseType)
		{
			return (Func<IMediator, object, CancellationToken, Task>)_sendWithResponseDelegates.GetOrAdd(
				responseType,
				CreateSendWithResponseDelegate);
		}

		/// <summary>
		/// Creates a strongly-typed delegate for invoking IMediator.Send&lt;TResponse&gt;.
		/// Uses Expression Trees to compile a delegate that directly invokes the Send method,
		/// avoiding the overhead of MethodInfo.Invoke on every call.
		/// </summary>
		/// <param name="responseType">The response type.</param>
		/// <returns>A compiled delegate that can invoke Send.</returns>
		private static Func<IMediator, object, CancellationToken, Task> CreateSendWithResponseDelegate(Type responseType)
		{
			var sendMethod = typeof(IMediator).GetMethods()
				.FirstOrDefault(m =>
					m.Name == nameof(IMediator.Send) &&
					m.IsGenericMethod &&
					m.GetParameters().Length == 2 &&
					m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IRequest<>) &&
					m.GetParameters()[1].ParameterType == typeof(CancellationToken));

			if (sendMethod == null)
			{
				throw new InvalidOperationException($"IMediator.Send method for IRequest<{responseType.Name}> not found.");
			}

			var genericMethod = sendMethod.MakeGenericMethod(responseType);
			var mediatorParam = Expression.Parameter(typeof(IMediator), "mediator");
			var messageParam = Expression.Parameter(typeof(object), "message");
			var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

			// Cast message to IRequest<TResponse>
			var requestType = typeof(IRequest<>).MakeGenericType(responseType);
			var castMessage = Expression.Convert(messageParam, requestType);
			var call = Expression.Call(mediatorParam, genericMethod, castMessage, cancellationTokenParam);
			var lambda = Expression.Lambda<Func<IMediator, object, CancellationToken, Task>>(
				call, mediatorParam, messageParam, cancellationTokenParam);

			return lambda.Compile();
		}

		/// <summary>
		/// Gets or creates a cached delegate for invoking IMediator.Publish&lt;TNotification&gt;.
		/// This avoids reflection overhead on every job processing.
		/// </summary>
		/// <param name="notificationType">The notification type.</param>
		/// <returns>A compiled delegate for invoking Publish.</returns>
		private static Func<IMediator, object, CancellationToken, Task> GetPublishDelegate(Type notificationType)
		{
			return (Func<IMediator, object, CancellationToken, Task>)_publishDelegates.GetOrAdd(
				notificationType,
				CreatePublishDelegate);
		}

		/// <summary>
		/// Creates a strongly-typed delegate for invoking IMediator.Publish&lt;TNotification&gt;.
		/// Uses Expression Trees to compile a delegate that directly invokes the Publish method,
		/// avoiding the overhead of MethodInfo.Invoke on every call.
		/// </summary>
		/// <param name="notificationType">The notification type.</param>
		/// <returns>A compiled delegate that can invoke Publish.</returns>
		private static Func<IMediator, object, CancellationToken, Task> CreatePublishDelegate(Type notificationType)
		{
			var publishMethod = typeof(IMediator).GetMethods()
				.FirstOrDefault(m =>
					m.Name == nameof(IMediator.Publish) &&
					m.IsGenericMethod &&
					m.GetParameters().Length == 2 &&
					m.GetParameters()[1].ParameterType == typeof(CancellationToken));

			if (publishMethod == null)
			{
				throw new InvalidOperationException($"IMediator.Publish method for {notificationType.Name} not found.");
			}

			var genericMethod = publishMethod.MakeGenericMethod(notificationType);
			var mediatorParam = Expression.Parameter(typeof(IMediator), "mediator");
			var notificationParam = Expression.Parameter(typeof(object), "notification");
			var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

			// Cast notification to INotification
			var castNotification = Expression.Convert(notificationParam, notificationType);
			var call = Expression.Call(mediatorParam, genericMethod, castNotification, cancellationTokenParam);
			var lambda = Expression.Lambda<Func<IMediator, object, CancellationToken, Task>>(
				call, mediatorParam, notificationParam, cancellationTokenParam);

			return lambda.Compile();
		}
	}
}

