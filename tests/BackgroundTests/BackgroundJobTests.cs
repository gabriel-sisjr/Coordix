using System;
using Coordix.Background.Implementation;
using Xunit;

namespace Coordix.Background.Tests;

public class BackgroundJobTests
{
	[Fact]
	public void BackgroundJob_Properties_Should_Be_Settable()
	{
		// Arrange
		var job = new BackgroundJob();
		var message = new TestRequest();
		var messageType = typeof(TestRequest);
		var responseType = typeof(string);

		// Act
		job.Message = message;
		job.MessageType = messageType;
		job.HasResponse = true;
		job.ResponseType = responseType;

		// Assert
		Assert.Same(message, job.Message);
		Assert.Same(messageType, job.MessageType);
		Assert.True(job.HasResponse);
		Assert.Same(responseType, job.ResponseType);
	}

	[Fact]
	public void BackgroundJob_ResponseType_Can_Be_Null()
	{
		// Arrange
		var job = new BackgroundJob();

		// Act
		job.HasResponse = false;
		job.ResponseType = null;

		// Assert
		Assert.False(job.HasResponse);
		Assert.Null(job.ResponseType);
	}
}

