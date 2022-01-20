using Xunit;

[assembly: CollectionBehavior(MaxParallelThreads = 32)]

namespace Xdxd.DotNet.Testing;

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Shared;

public static class TestHelper
{
    // ReSharper disable once InconsistentNaming
    private static TestConfig _testConfig;
    private static readonly object TestConfigSync = new();

    public static async Task<string> GetResourceText(string name)
    {
        var bytes = await GetResourceBytes(name);
        return EncodingHelper.UTF8.GetString(bytes);
    }

    public static string GetProjectFilePath(string name)
    {
        return $"../../../{name}";
    }

    public static string GetProjectFileContent(string name)
    {
        return File.ReadAllText(GetProjectFilePath(name));
    }

    public static string GetResourceFilePath(string name)
    {
        return GetProjectFilePath($"resources/{name}");
    }

    public static async Task<byte[]> GetResourceBytes(string name)
    {
        var content = await File.ReadAllBytesAsync(GetResourceFilePath(name));
        return content;
    }


    public static TestConfig TestConfig
    {
        get
        {
            if (_testConfig == null)
            {
                lock (TestConfigSync)
                {
                    if (_testConfig == null)
                    {
                        var assembly = Assembly.GetCallingAssembly();
                        var testConfigAttribute = assembly.GetCustomAttribute<TestConfigAttribute>();
                        string json = GetProjectFileContent(testConfigAttribute!.TestConfigFile);
                        _testConfig = JsonHelper.Deserialize<TestConfig>(json);
                    }
                }
            }

            return _testConfig;
        }
    }
}

public class TestConfig
{
    public string TestMasterConnectionString { get; set; }
}

[AttributeUsage(AttributeTargets.Assembly)]
public class TestConfigAttribute : Attribute
{
    public string TestConfigFile { get; }

    public TestConfigAttribute(string testConfigFile)
    {
        this.TestConfigFile = testConfigFile;
    }
}
