using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Coordix.Background.Interfaces;
using Coordix.Interfaces;
using Microsoft.Extensions.Logging;

namespace Coordix.Background.Implementation
{
	/// <summary>
	/// Implementation of IBackgroundMediator that enqueues jobs to an internal channel.
	/// </summary>
	public class BackgroundMediator : IBackgroundMediator
	{
		private readonly ChannelWriter<BackgroundJob> _channelWriter;
		private readonly ILogger<BackgroundMediator> _logger;

		public BackgroundMediator(ChannelWriter<BackgroundJob> channelWriter, ILogger<BackgroundMediator> logger)
		{
			_channelWriter = channelWriter ?? throw new ArgumentNullException(nameof(channelWriter));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public Task Enqueue<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			var job = new BackgroundJob
			{
				Message = request,
				MessageType = request.GetType(),
				HasResponse = true,
				ResponseType = typeof(TResponse)
			};

			return EnqueueJob(job, cancellationToken);
		}

		public Task Enqueue(IRequest request, CancellationToken cancellationToken = default)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			var job = new BackgroundJob
			{
				Message = request,
				MessageType = request.GetType(),
				HasResponse = false
			};

			return EnqueueJob(job, cancellationToken);
		}

		public Task Enqueue<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
			where TNotification : INotification
		{
			if (notification == null)
			{
				throw new ArgumentNullException(nameof(notification));
			}

			var job = new BackgroundJob
			{
				Message = notification,
				MessageType = typeof(TNotification),
				HasResponse = false
			};

			return EnqueueJob(job, cancellationToken);
		}

		private async Task EnqueueJob(BackgroundJob job, CancellationToken cancellationToken)
		{
			try
			{
				await _channelWriter.WriteAsync(job, cancellationToken);
				_logger.LogDebug("Background job enqueued: {MessageType}", job.MessageType.Name);
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogError(ex, "Failed to enqueue background job: {MessageType}", job.MessageType.Name);
				throw;
			}
		}
	}
}

