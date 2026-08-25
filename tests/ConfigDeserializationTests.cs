using FluentAssertions;
using Xunit;

namespace NetgearCli.Tests;

public class ConfigDeserializationTests
{
    [Fact]
    public void Config_Class_Exists()
    {
        AssemblyFixture.PluginAssembly.GetType("PepperDash.Essentials.Plugins.NetgearCliConfigObject")
            .Should().NotBeNull();
    }

    [Fact]
    public void Config_Has_Parameterless_Constructor()
    {
        var type = AssemblyFixture.PluginAssembly.GetType("PepperDash.Essentials.Plugins.NetgearCliConfigObject");
        type!.GetConstructor(Type.EmptyTypes).Should().NotBeNull();
    }

    [Theory]
    [InlineData("Control", "control")]
    [InlineData("Password", "password")]
    [InlineData("MaxEnableAttempts", "maxEnableAttempts")]
    public void Config_Property_Has_JsonPropertyAttribute(string propertyName, string jsonName)
    {
        var type = AssemblyFixture.PluginAssembly.GetType("PepperDash.Essentials.Plugins.NetgearCliConfigObject");
        var property = type!.GetProperty(propertyName);
        property.Should().NotBeNull();

        var hasAttribute = property!.CustomAttributes.Any(a =>
            a.AttributeType.Name == "JsonPropertyAttribute"
            && a.ConstructorArguments.Any(arg =>
                string.Equals(arg.Value?.ToString(), jsonName, StringComparison.Ordinal)));

        hasAttribute.Should().BeTrue($"{propertyName} should be decorated with [JsonProperty(\"{jsonName}\")]");
    }
}
