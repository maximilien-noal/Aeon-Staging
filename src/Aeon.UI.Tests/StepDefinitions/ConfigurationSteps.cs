using System.Text.Json;
using Aeon.Emulator.Configuration;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

namespace Aeon.UI.Tests.StepDefinitions;

[Binding]
public sealed class ConfigurationSteps : IDisposable
{
    private string? tempFilePath;
    private string? tempDir;
    private AeonConfiguration? config;
    private AeonConfiguration? baseConfig;
    private AeonConfiguration? overlayConfig;
    private AeonConfiguration? mergedConfig;

    [Given("a configuration file with the following JSON")]
    public void GivenAConfigurationFileWithTheFollowingJSON(string multilineText)
    {
        tempDir = $"{Path.GetTempPath()}aeon-cfg-test-{Guid.NewGuid():N}";
        Directory.CreateDirectory(tempDir);
        tempFilePath = $"{tempDir}/test.AeonConfig";
        File.WriteAllText(tempFilePath, multilineText);
    }

    [When("the configuration is loaded")]
    public void WhenTheConfigurationIsLoaded()
    {
        config = AeonConfiguration.Load(tempFilePath!);
    }

    [Then("the configuration EmulationSpeed should be {int}")]
    public void ThenTheConfigurationEmulationSpeedShouldBe(int expected)
    {
        Assert.IsNotNull(config);
        Assert.AreEqual(expected, config.EmulationSpeed);
    }

    [Then("the configuration IsMouseAbsolute should be true")]
    public void ThenTheConfigurationIsMouseAbsoluteShouldBeTrue()
    {
        Assert.IsNotNull(config);
        Assert.IsTrue(config.IsMouseAbsolute);
    }

    [Then("the configuration IsMouseAbsolute should be false")]
    public void ThenTheConfigurationIsMouseAbsoluteShouldBeFalse()
    {
        Assert.IsNotNull(config);
        Assert.IsFalse(config.IsMouseAbsolute);
    }

    [Then("the configuration Title should be {string}")]
    public void ThenTheConfigurationTitleShouldBe(string expected)
    {
        Assert.IsNotNull(config);
        Assert.AreEqual(expected, config.Title);
    }

    [Then("the configuration should have {int} drive mappings")]
    public void ThenTheConfigurationShouldHaveDriveMappings(int count)
    {
        Assert.IsNotNull(config);
        Assert.IsNotNull(config.Drives);
        Assert.AreEqual(count, config.Drives.Count);
    }

    [Then("the configuration should have a {string} drive")]
    public void ThenTheConfigurationShouldHaveADrive(string driveLetter)
    {
        Assert.IsNotNull(config);
        Assert.IsNotNull(config.Drives);
        Assert.IsTrue(config.Drives.ContainsKey(driveLetter), $"Drive '{driveLetter}' not found.");
    }

    [Given("a quick launch configuration for {string} launching {string}")]
    public void GivenAQuickLaunchConfigurationForLaunching(string hostPath, string launchTarget)
    {
        config = AeonConfiguration.GetQuickLaunchConfiguration(hostPath, launchTarget);
    }

    [Then("the configuration Launch should be {string}")]
    public void ThenTheConfigurationLaunchShouldBe(string expected)
    {
        Assert.IsNotNull(config);
        Assert.AreEqual(expected, config.Launch);
    }

    [Then("the configuration PhysicalMemorySize should be {int}")]
    public void ThenTheConfigurationPhysicalMemorySizeShouldBe(int expected)
    {
        Assert.IsNotNull(config);
        Assert.AreEqual(expected, config.PhysicalMemorySize);
    }

    [Given("a base configuration with speed {int}")]
    public void GivenABaseConfigurationWithSpeed(int speed)
    {
        baseConfig = new AeonConfiguration { EmulationSpeed = speed };
    }

    [Given("an overlay configuration with title {string}")]
    public void GivenAnOverlayConfigurationWithTitle(string title)
    {
        overlayConfig = new AeonConfiguration { Title = title };
    }

    [When("the overlay is merged into the base configuration")]
    public void WhenTheOverlayIsMergedIntoTheBaseConfiguration()
    {
        Assert.IsNotNull(baseConfig);
        baseConfig.MergeFrom(overlayConfig);
        mergedConfig = baseConfig;
    }

    [Then("the merged configuration speed should be {int}")]
    public void ThenTheMergedConfigurationSpeedShouldBe(int expected)
    {
        Assert.IsNotNull(mergedConfig);
        Assert.AreEqual(expected, mergedConfig.EmulationSpeed);
    }

    [Then("the merged configuration title should be {string}")]
    public void ThenTheMergedConfigurationTitleShouldBe(string expected)
    {
        Assert.IsNotNull(mergedConfig);
        Assert.AreEqual(expected, mergedConfig.Title);
    }

    [Then("the configuration StartupPath should be {string}")]
    public void ThenTheConfigurationStartupPathShouldBe(string expected)
    {
        Assert.IsNotNull(config);
        Assert.AreEqual(expected, config.StartupPath);
    }

    [Then("the configuration MidiEngine should be {string}")]
    public void ThenTheConfigurationMidiEngineShouldBe(string expected)
    {
        Assert.IsNotNull(config);
        Assert.IsNotNull(config.MidiEngine);
        Assert.AreEqual(expected, config.MidiEngine.Value.ToString());
    }

    [AfterScenario]
    public void Cleanup()
    {
        TestHelpers.CleanupTempDir(tempDir);
        tempFilePath = null;
        tempDir = null;
    }

    public void Dispose()
    {
        Cleanup();
    }
}
