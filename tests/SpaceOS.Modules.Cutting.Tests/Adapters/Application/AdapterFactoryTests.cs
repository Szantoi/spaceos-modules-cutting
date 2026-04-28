using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Application.Adapters;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Application;

public class AdapterFactoryTests
{
    private static ICuttingProvider MockProvider() => Mock.Of<ICuttingProvider>();

    [Fact]
    public void GetByName_RegisteredAdapter_ReturnsIt()
    {
        var provider = MockProvider();
        var factory = new AdapterFactory(new[]
        {
            new KeyedAdapterRegistration("builtin", provider)
        });

        var result = factory.GetByName("builtin");

        result.Should().BeSameAs(provider);
    }

    [Fact]
    public void GetByName_UnknownName_ThrowsInvalidOperationException()
    {
        var factory = new AdapterFactory(new[]
        {
            new KeyedAdapterRegistration("builtin", MockProvider())
        });

        var act = () => factory.GetByName("nonexistent");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*nonexistent*");
    }

    [Fact]
    public void RegisteredAdapterNames_ReturnsAllKeys()
    {
        var factory = new AdapterFactory(new[]
        {
            new KeyedAdapterRegistration("builtin", MockProvider()),
            new KeyedAdapterRegistration("opticut", MockProvider())
        });

        factory.RegisteredAdapterNames.Should().BeEquivalentTo("builtin", "opticut");
    }

    [Fact]
    public void GetByName_CaseSensitive_DoesNotMatchDifferentCase()
    {
        var factory = new AdapterFactory(new[]
        {
            new KeyedAdapterRegistration("builtin", MockProvider())
        });

        var act = () => factory.GetByName("Builtin");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_EmptyRegistrations_CreatesEmptyFactory()
    {
        var factory = new AdapterFactory(Array.Empty<KeyedAdapterRegistration>());
        factory.RegisteredAdapterNames.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_NullRegistrations_ThrowsArgumentNullException()
    {
        var act = () => new AdapterFactory(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetByName_MultipleAdapters_ReturnsCorrectOne()
    {
        var builtin = MockProvider();
        var opticut = MockProvider();
        var factory = new AdapterFactory(new[]
        {
            new KeyedAdapterRegistration("builtin", builtin),
            new KeyedAdapterRegistration("opticut", opticut)
        });

        factory.GetByName("opticut").Should().BeSameAs(opticut);
        factory.GetByName("builtin").Should().BeSameAs(builtin);
    }
}
