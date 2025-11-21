using System.Threading;
using System.Threading.Tasks;
using CodeGenSample.Requests;
using Coordix.Interfaces;

namespace CodeGenSample.Handlers
{
	/// <summary>
	/// Handler for CreateUserCommand that creates a new user.
	/// </summary>
	public class CreateUserHandler : IRequestHandler<CreateUserCommand>
	{
		public Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
		{
			// Simulate creating user in database
			Console.WriteLine($"[CreateUserHandler] Creating user: {request.Name} ({request.Email})");

			return Task.CompletedTask;
		}
	}
}

