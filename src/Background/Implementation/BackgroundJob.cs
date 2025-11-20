using System;
using Coordix.Interfaces;

namespace Coordix.Background.Implementation
{
	/// <summary>
	/// Represents a background job to be processed asynchronously.
	/// </summary>
	public class BackgroundJob
	{
		/// <summary>
		/// The request or notification message to process.
		/// </summary>
		public object Message { get; set; } = null!;

		/// <summary>
		/// The type of the message.
		/// </summary>
		public Type MessageType { get; set; } = null!;

		/// <summary>
		/// Indicates whether this is a request with response.
		/// </summary>
		public bool HasResponse { get; set; }

		/// <summary>
		/// The response type if this is a request with response.
		/// </summary>
		public Type? ResponseType { get; set; }
	}
}

