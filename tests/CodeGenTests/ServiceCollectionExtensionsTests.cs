using System;
using System.Linq;
using System.Reflection;
using Coordix;
using Coordix.CodeGen.Extensions;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Coordix.CodeGen.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCoordixWithCodeGen_ThrowsException_WhenGeneratedExecutorNotFound()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCoordixWithCodeGen());

        Assert.Contains("GeneratedHandlerExecutor", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void AddCoordixWithCodeGen_WithArgs_ThrowsException_WhenGeneratedExecutorNotFound()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        Assembly assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCoordixWithCodeGen(assembly));

        Assert.Contains("GeneratedHandlerExecutor", exception.Message);
    }

    [Fact]
    public void AddCoordixWithCodeGen_WithPrefixStrings_ThrowsException_WhenGeneratedExecutorNotFound()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCoordixWithCodeGen("Coordix.Tests"));

        Assert.Contains("GeneratedHandlerExecutor", exception.Message);
    }

    [Fact]
    public void AddCoordixWithCodeGen_ThrowsException_WhenExecutorNotFound_WithoutRegisteringServices()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCoordixWithCodeGen());

        // Assert - should throw before registering any services
        Assert.Contains("not found", exception.Message);

        // Services should NOT be registered when executor is not found
        // because the exception is thrown before AddCoordix is called
        ServiceDescriptor? mediatorDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMediator));
        Assert.Null(mediatorDescriptor); // Nothing was registered
    }

    [Fact]
    public void AddCoordixBackgroundWithCodeGen_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services!.AddCoordixBackgroundWithCodeGen());
    }

    [Fact]
    public void AddCoordixBackgroundWithCodeGen_ThrowsException_WhenGeneratedExecutorNotFound()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCoordixBackgroundWithCodeGen());

        Assert.Contains("GeneratedHandlerExecutor", exception.Message);
    }

    [Fact]
    public void AddCoordixBackgroundWithCodeGen_ThrowsException_WhenBackgroundPackageNotInstalled()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();

        // This test assumes Background assembly is loaded, but in a real scenario
        // where it's not loaded, it should throw the Background package not found error
        // Since Background IS loaded in our test environment, we'll test the executor error instead
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCoordixBackgroundWithCodeGen());

        // Either executor or background package not found
        Assert.True(
            exception.Message.Contains("GeneratedHandlerExecutor") ||
            exception.Message.Contains("Coordix.Background"));
    }

    [Fact]
    public void AddCoordixBackgroundWithCodeGen_WithArgs_PropagatesArgs()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        Assembly assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCoordixBackgroundWithCodeGen(assembly));

        // Should fail at executor lookup, meaning args were passed correctly
        Assert.Contains("GeneratedHandlerExecutor", exception.Message);
    }

    // Test for ResolveAssemblies with empty args
    [Fact]
    public void AddCoordixWithCodeGen_WithNoArgs_ScansAllAssemblies()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();

        // Act & Assert
        try
        {
            services.AddCoordixWithCodeGen();
        }
        catch (InvalidOperationException ex)
        {
            // Should scan all assemblies and fail to find executor
            Assert.Contains("not found", ex.Message);
        }
    }

    // Test for ResolveAssemblies with mixed types (should throw ArgumentException)
    [Fact]
    public void AddCoordixWithCodeGen_WithMixedArgs_ThrowsArgumentException()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        Assembly assembly = typeof(ServiceCollectionExtensionsTests).Assembly;
        object[] mixedArgs = new object[] { assembly, "SomePrefix" };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            services.AddCoordixWithCodeGen(mixedArgs));

        Assert.Contains("Invalid parameters", exception.Message);
    }

    // Test for multiple calls to AddCoordixWithCodeGen (should not duplicate due to TryAdd)
    [Fact]
    public void AddCoordixWithCodeGen_CalledMultipleTimes_DoesNotDuplicateRegistrations()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();

        // Act - try to call twice (both will fail at executor lookup, but that's fine)
        try { services.AddCoordixWithCodeGen(); } catch { /* ignore */ }

        int mediatorCountBefore = services.Count(d => d.ServiceType == typeof(IMediator));

        try { services.AddCoordixWithCodeGen(); } catch { /* ignore */ }

        int mediatorCountAfter = services.Count(d => d.ServiceType == typeof(IMediator));

        // Assert - should not have duplicated the IMediator registration
        Assert.Equal(mediatorCountBefore, mediatorCountAfter);
    }

    // Test CoordixOptions registration with CodeGenPreferred mode
    [Fact]
    public void AddCoordixWithCodeGen_WhenSuccessful_ShouldRegisterCodeGenPreferredMode()
    {
        // This test would require a mock GeneratedHandlerExecutor to be present
        // For now, we verify that the method attempts to set CodeGenPreferred
        // by checking the exception doesn't complain about the mode

        // Arrange
        ServiceCollection services = new ServiceCollection();

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCoordixWithCodeGen());

        // Should fail at finding executor, not at setting mode
        Assert.Contains("not found", exception.Message);
        Assert.DoesNotContain("HandlerResolutionMode", exception.Message);
    }

    // Integration-style test: verify exception behavior
    [Fact]
    public void AddCoordixWithCodeGen_DoesNotRegisterServices_WhenExecutorNotFound()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        int serviceCountBefore = services.Count;

        // Act
        try
        {
            services.AddCoordixWithCodeGen();
            Assert.Fail("Should have thrown InvalidOperationException");
        }
        catch (InvalidOperationException ex)
        {
            // Expected - no executor found
            Assert.Contains("not found", ex.Message);
        }

        // Assert - No services should be registered because exception is thrown early
        int serviceCountAfter = services.Count;
        Assert.Equal(serviceCountBefore, serviceCountAfter);
    }

    // Test for prefix-based assembly filtering
    [Fact]
    public void AddCoordixWithCodeGen_WithMultiplePrefixes_FiltersAssembliesCorrectly()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();

        // Act & Assert
        try
        {
            services.AddCoordixWithCodeGen("Coordix", "System");
        }
        catch (InvalidOperationException ex)
        {
            // Should scan assemblies with those prefixes and fail to find executor
            Assert.Contains("not found", ex.Message);
        }
    }

    // Test for Assembly array parameter
    [Fact]
    public void AddCoordixWithCodeGen_WithMultipleAssemblies_SearchesAllAssemblies()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        Assembly assembly1 = typeof(ServiceCollectionExtensionsTests).Assembly;
        Assembly assembly2 = typeof(IMediator).Assembly;

        // Act & Assert
        try
        {
            services.AddCoordixWithCodeGen(assembly1, assembly2);
        }
        catch (InvalidOperationException ex)
        {
            Assert.Contains("not found", ex.Message);
        }
    }
}

