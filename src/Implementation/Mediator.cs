using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Coordix.Implementation
{
    /// <summary>
    /// A mediator that resolves handlers from an IServiceProvider and invokes them for requests and notifications.
    /// </summary>
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="Mediator"/> class using the given service provider.
        /// </summary>
        /// <param name="provider">The service provider used to resolve handler instances.</param>
        public Mediator(IServiceProvider provider) => _provider = provider;

        /// <summary>
        /// Sends a request to a single handler and returns its response.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
        /// <param name="request">The request message to send.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing the handler's response.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no handler is found for the request type.</exception>
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var requestType = request.GetType();
            var responseType = typeof(TResponse);
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);

            var handler = _provider.GetService(handlerType);

            if (handler is null)
                throw new InvalidOperationException($"Handler not found for {requestType.Name}");

            var response = await ToHandleInvokeRequest(handlerType, handler, request, cancellationToken);
            return response;
        }

        /// <summary>
        /// Sends a request to a single handler without expecting a response.
        /// </summary>
        /// <param name="request">The request message to send.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no handler is found for the request type.</exception>
        public async Task Send(IRequest request, CancellationToken cancellationToken = default)
        {
            var requestType = request.GetType();
            var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);

            var handler = _provider.GetService(handlerType);

            if (handler is null)
                throw new InvalidOperationException($"Handler not found for {requestType.Name}");

            await ToHandleInvokeRequest(handlerType, handler, request, cancellationToken);
        }

        /// <summary>
        /// Publishes a notification to all registered handlers.
        /// </summary>
        /// <typeparam name="TNotification">The type of notification to publish.</typeparam>
        /// <param name="notification">The notification message to publish.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous publish operation.</returns>
        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            var notificationType = notification.GetType();
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handlers = _provider.GetServices(handlerType).ToArray();
            var tasksHandlers = handlers.Where(h => h != null).Select(handler => ToHandleInvokeNotification(handlerType, handler!, notification, cancellationToken));

            await Task.WhenAll(tasksHandlers);
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
        private static Task ToHandleInvokeNotification<TNotification>(Type handlerType, object handler, TNotification notification, CancellationToken cancellationToken)
            => (Task)handlerType.GetMethod("Handle")!.Invoke(handler, new object[] { notification!, cancellationToken });

        /// <summary>
        /// Invokes the Handle method on a request handler without return value.
        /// </summary>
        /// <param name="handlerType">The runtime handler interface type.</param>
        /// <param name="handler">The handler instance to invoke.</param>
        /// <param name="request">The request message.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the handler invocation.</returns>
        private static Task ToHandleInvokeRequest(Type handlerType, object handler, IRequest request, CancellationToken cancellationToken)
            => (Task)handlerType.GetMethod("Handle")!.Invoke(handler, new object[] { request, cancellationToken })!;

        /// <summary>
        /// Invokes the Handle method on a request handler and returns its response.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
        /// <param name="handlerType">The runtime handler interface type.</param>
        /// <param name="handler">The handler instance to invoke.</param>
        /// <param name="request">The request message.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the handler invocation, containing the handler's response.</returns>
        private static Task<TResponse> ToHandleInvokeRequest<TResponse>(Type handlerType, object handler, IRequest<TResponse> request, CancellationToken cancellationToken)
            => (Task<TResponse>)handlerType.GetMethod("Handle")!.Invoke(handler, new object[] { request, cancellationToken })!;
    }
}
