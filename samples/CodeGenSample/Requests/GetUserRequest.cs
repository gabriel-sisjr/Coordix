using Coordix.Interfaces;

namespace CodeGenSample.Requests
{
	/// <summary>
	/// Request to get user information by ID.
	/// </summary>
	public class GetUserRequest : IRequest<GetUserResponse>
	{
		public int UserId { get; set; }
	}

	/// <summary>
	/// Response containing user information.
	/// </summary>
	public class GetUserResponse
	{
		public int UserId { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
	}
}

