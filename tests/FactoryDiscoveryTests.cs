using FluentAssertions;
using Xunit;

namespace NetgearCli.Tests;

public class FactoryDiscoveryTests
{
    [Fact]
    public void Assembly_Loads_Successfully()
    {
        AssemblyFixture.PluginAssembly.Should().NotBeNull();
    }

    [Fact]
    public void Assembly_Name_Matches_Expected()
    {
        AssemblyFixture.PluginAssembly.GetName().Name
            .Should().Be("PepperDash.Essentials.Plugins.Netgear.CLI");
    }

    [Fact]
    public void Factory_Count_Matches_Expected()
    {
        AssemblyFixture.FindFactoryTypes().Should().HaveCount(1);
    }

    [Theory]
    [InlineData("NetgearCliFactory")]
    public void Factory_Exists_ByName(string factoryClassName)
    {
        var factories = AssemblyFixture.FindFactoryTypes();
        factories.Should().Contain(t => t.Name == factoryClassName);
    }

    [Fact]
    public void All_Factories_Have_Parameterless_Constructor()
    {
        foreach (var factory in AssemblyFixture.FindFactoryTypes())
        {
            factory.GetConstructor(Type.EmptyTypes)
                .Should().NotBeNull($"factory {factory.Name} must have a parameterless constructor for plugin discovery");
        }
    }
}
