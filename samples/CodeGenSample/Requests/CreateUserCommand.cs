using Coordix.Interfaces;

namespace CodeGenSample.Requests
{
	/// <summary>
	/// Command to create a new user (no response).
	/// </summary>
	public class CreateUserCommand : IRequest
	{
		public string Name { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
	}
}

