using FluentAssertions;
using Xunit;

namespace NetgearCli.Tests;

public class FactoryMetadataTests
{
    private const string ExpectedMinimumEssentialsFrameworkVersion = "3.0.0";

    [Fact]
    public void Factory_Source_Sets_MinimumEssentialsFrameworkVersion()
    {
        var source = AssemblyFixture.FindSourceForClass("NetgearCliFactory");
        source.Should().NotBeNull();
        source!.Should().Contain($"MinimumEssentialsFrameworkVersion = \"{ExpectedMinimumEssentialsFrameworkVersion}\"");
    }

    [Fact]
    public void Factory_Source_Sets_TypeNames()
    {
        var source = AssemblyFixture.FindSourceForClass("NetgearCliFactory");
        source.Should().NotBeNull();
        source!.Should().Contain("TypeNames = new List<string>()");
    }

    [Theory]
    [InlineData("NetgearCliFactory", "NetgearCli")]
    public void Factory_Source_Contains_TypeName(string factoryClassName, string typeName)
    {
        var source = AssemblyFixture.FindSourceForClass(factoryClassName);
        source.Should().NotBeNull();
        source!.Should().Contain($"\"{typeName}\"");
    }
}
