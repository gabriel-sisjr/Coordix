namespace Coordix
{
	/// <summary>
	/// Configuration options for Coordix mediator services.
	/// </summary>
	public class CoordixOptions
	{
		/// <summary>
		/// Gets or sets the handler resolution mode.
		/// Defaults to <see cref="HandlerResolutionMode.Reflection"/>.
		/// </summary>
		public HandlerResolutionMode HandlerResolutionMode { get; set; } = HandlerResolutionMode.Reflection;
	}
}

