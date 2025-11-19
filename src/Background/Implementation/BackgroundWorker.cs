using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Coordix.Interfaces;
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
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<BackgroundWorker> _logger;

		public BackgroundWorker(
			ChannelReader<BackgroundJob> channelReader,
			IServiceProvider serviceProvider,
			ILogger<BackgroundWorker> logger)
		{
			_channelReader = channelReader ?? throw new ArgumentNullException(nameof(channelReader));
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
			var mediator = _serviceProvider.GetService(typeof(IMediator)) as IMediator;
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
					// Request with response
					// Get the generic Send<TResponse> method
					var sendMethods = typeof(IMediator).GetMethods();
					var sendMethod = sendMethods.FirstOrDefault(m =>
						m.Name == nameof(IMediator.Send) &&
						m.IsGenericMethod &&
						m.GetParameters().Length == 2 &&
						m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IRequest<>) &&
						m.GetParameters()[1].ParameterType == typeof(CancellationToken));

					if (sendMethod != null)
					{
						var genericMethod = sendMethod.MakeGenericMethod(job.ResponseType);
						var task = genericMethod.Invoke(mediator, new object[] { job.Message, cancellationToken });
						if (task is Task taskResult)
						{
							await taskResult;
						}
					}
					else
					{
						_logger.LogError("IMediator.Send method for IRequest<{ResponseType}> not found.", job.ResponseType.Name);
					}
				}
				else if (job.Message is IRequest request)
				{
					// Request without response
					await mediator.Send(request, cancellationToken);
				}
				else if (job.Message is INotification notification)
				{
					// Notification
					var publishMethod = typeof(IMediator).GetMethod(nameof(IMediator.Publish));
					if (publishMethod != null)
					{
						var genericMethod = publishMethod.MakeGenericMethod(job.MessageType);
						var task = (Task)genericMethod.Invoke(mediator, new object[] { notification, cancellationToken });
						await task;
					}
				}

				_logger.LogDebug("Background job processed successfully: {MessageType}", job.MessageType.Name);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing background job: {MessageType}", job.MessageType.Name);
				throw; // Re-throw to be caught by the outer try-catch
			}
		}
	}
}

