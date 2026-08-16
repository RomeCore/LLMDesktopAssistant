using LLMDesktopAssistant.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LLMDesktopAssistant.Tests.Utils;

public class ServiceCollectionExtensionsTests
{
	public interface IFirst { }
	public interface ISecond { }
	public sealed class SharedService : IFirst, ISecond { }
	public sealed class FirstImpl : IFirst { }
	public sealed class SecondImpl : IFirst { }

	[Fact]
	public void SingletonSameImplementationSharesInstance()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IFirst, SharedService>();
		services.AddSingleton<ISecond, SharedService>();
		services.DeduplicateServices();

		var provider = services.BuildServiceProvider();

		Assert.Same(provider.GetRequiredService<IFirst>(), provider.GetRequiredService<ISecond>());
		Assert.Same(provider.GetRequiredService<IFirst>(), provider.GetRequiredService<SharedService>());
	}

	[Fact]
	public void ScopedSameImplementationSharesInstanceWithinScope()
	{
		var services = new ServiceCollection();
		services.AddScoped<IFirst, SharedService>();
		services.AddScoped<ISecond, SharedService>();
		services.DeduplicateServices();

		var provider = services.BuildServiceProvider();

		using var scope1 = provider.CreateScope();
		using var scope2 = provider.CreateScope();

		Assert.Same(
			scope1.ServiceProvider.GetRequiredService<IFirst>(),
			scope1.ServiceProvider.GetRequiredService<ISecond>());
		Assert.NotSame(
			scope1.ServiceProvider.GetRequiredService<IFirst>(),
			scope2.ServiceProvider.GetRequiredService<IFirst>());
	}

	[Fact]
	public void MultipleImplementationsOfSameInterfaceArePreserved()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IFirst, FirstImpl>();
		services.AddSingleton<IFirst, SecondImpl>();
		services.DeduplicateServices();

		var provider = services.BuildServiceProvider();

		var all = provider.GetServices<IFirst>().ToArray();
		Assert.Equal(2, all.Length);
		Assert.Contains(all, s => s is FirstImpl);
		Assert.Contains(all, s => s is SecondImpl);
	}

	[Fact]
	public void ConcreteRegistrationStaysResolvable()
	{
		var services = new ServiceCollection();
		services.AddSingleton<SharedService>();
		services.AddSingleton<IFirst, SharedService>();
		services.DeduplicateServices();

		var provider = services.BuildServiceProvider();

		Assert.Same(provider.GetRequiredService<SharedService>(), provider.GetRequiredService<IFirst>());
	}

	[Fact]
	public void InstanceRegistrationsRemainShared()
	{
		var instance = new SharedService();
		var services = new ServiceCollection();
		services.AddSingleton<IFirst>(instance);
		services.AddSingleton<ISecond>(instance);
		services.DeduplicateServices();

		var provider = services.BuildServiceProvider();

		Assert.Same(instance, provider.GetRequiredService<IFirst>());
		Assert.Same(instance, provider.GetRequiredService<ISecond>());
	}

	[Fact]
	public void SingleRegistrationIsNotRewritten()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IFirst, FirstImpl>();
		services.DeduplicateServices();

		var provider = services.BuildServiceProvider();

		Assert.IsType<FirstImpl>(provider.GetRequiredService<IFirst>());
	}

	[Fact]
	public void FactoryRegistrationsAreNotDeduplicated()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IFirst>(_ => new SharedService());
		services.AddSingleton<ISecond>(_ => new SharedService());
		services.DeduplicateServices();

		var provider = services.BuildServiceProvider();

		// Factories are passed through untouched: each service type keeps its own instance.
		Assert.NotSame(provider.GetRequiredService<IFirst>(), provider.GetRequiredService<ISecond>());
	}

	[Fact]
	public void KeyedTypeRegistrationsArePreserved()
	{
		var services = new ServiceCollection();
		services.AddKeyedSingleton<IFirst, FirstImpl>("key");
		services.AddSingleton<IFirst, SecondImpl>();
		services.DeduplicateServices();

		var provider = services.BuildServiceProvider();

		// Keyed registration keeps its own instance and remains resolvable via the key.
		Assert.IsType<FirstImpl>(provider.GetRequiredKeyedService<IFirst>("key"));
		// Non-keyed registration is not affected by the keyed one.
		Assert.IsType<SecondImpl>(provider.GetRequiredService<IFirst>());
	}
}
