using System;
using System.Linq;
using System.Threading.Channels;
using Coordix.Background.Extensions;
using Coordix.Background.Implementation;
using Coordix.Background.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Coordix.Background.Tests;

public class ServiceCollectionExtensionsTests
{
	[Fact]
	public void AddCoordixBackground_With_Null_Services_Should_Throw_ArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddCoordixBackground(null!));
	}

	[Fact]
	public void AddCoordixBackground_Should_Register_ChannelWriter()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddCoordixBackground();

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		var channelWriter = serviceProvider.GetService<ChannelWriter<BackgroundJob>>();
		Assert.NotNull(channelWriter);
	}

	[Fact]
	public void AddCoordixBackground_Should_Register_ChannelReader()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddCoordixBackground();

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		var channelReader = serviceProvider.GetService<ChannelReader<BackgroundJob>>();
		Assert.NotNull(channelReader);
	}

	[Fact]
	public void AddCoordixBackground_Should_Register_IBackgroundMediator()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();

		// Act
		services.AddCoordixBackground();

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		var backgroundMediator = serviceProvider.GetService<IBackgroundMediator>();
		Assert.NotNull(backgroundMediator);
		Assert.IsType<BackgroundMediator>(backgroundMediator);
	}

	[Fact]
	public void AddCoordixBackground_Should_Register_BackgroundWorker_As_HostedService()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();

		// Act
		services.AddCoordixBackground();

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		var hostedServices = serviceProvider.GetServices<IHostedService>();
		var backgroundWorker = hostedServices.OfType<BackgroundWorker>().FirstOrDefault();
		Assert.NotNull(backgroundWorker);
	}

	[Fact]
	public void AddCoordixBackground_Should_Return_Same_ServiceCollection()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		var result = services.AddCoordixBackground();

		// Assert
		Assert.Same(services, result);
	}

	[Fact]
	public void AddCoordixBackground_Should_Register_Channel_As_Singleton()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddCoordixBackground();

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		var channelWriter1 = serviceProvider.GetService<ChannelWriter<BackgroundJob>>();
		var channelWriter2 = serviceProvider.GetService<ChannelWriter<BackgroundJob>>();
		Assert.Same(channelWriter1, channelWriter2);
	}
}

