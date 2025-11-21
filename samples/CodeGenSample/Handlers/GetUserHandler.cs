using System.Threading;
using System.Threading.Tasks;
using CodeGenSample.Requests;
using Coordix.Interfaces;

namespace CodeGenSample.Handlers
{
	/// <summary>
	/// Handler for GetUserRequest that returns user information.
	/// </summary>
	public class GetUserHandler : IRequestHandler<GetUserRequest, GetUserResponse>
	{
		public Task<GetUserResponse> Handle(GetUserRequest request, CancellationToken cancellationToken)
		{
			// Simulate fetching user from database
			var response = new GetUserResponse
			{
				UserId = request.UserId,
				Name = "John Doe",
				Email = "john.doe@example.com"
			};

			Console.WriteLine($"[GetUserHandler] Retrieved user {response.UserId}: {response.Name}");

			return Task.FromResult(response);
		}
	}
}

