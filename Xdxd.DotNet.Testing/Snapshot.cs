using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Xdxd.DotNet.Testing;

[assembly: UseReporter(typeof(CustomReporter))]
[assembly: UseApprovalSubdirectory("./snapshots")]

namespace Xdxd.DotNet.Testing;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ApprovalTests;
using ApprovalTests.Core;
using ApprovalUtilities.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;
using Xunit.Sdk;

public static class Snapshot
{
    private static readonly string[] IgnoredExceptionFields =
    {
        "InnerException",
        "StackTrace",
        "StackTraceString",
    };

    public static void Match<T>(T obj, string[] parameters = null)
    {
        string json = Serialize(obj);

        if (parameters != null)
        {
            NamerFactory.AdditionalInformation = string.Join("_", parameters);
        }

        Approvals.VerifyWithExtension(json, ".json");
    }

    public static void MatchJson(string json, string[] parameters = null)
    {
        string formatJson = JsonPrettyPrint.FormatJson(json);

        if (parameters != null)
        {
            NamerFactory.AdditionalInformation = string.Join("_", parameters);
        }

        Approvals.VerifyWithExtension(formatJson, ".json");
    }

    public static void MatchError(Exception exception, string[] parameters = null)
    {
        string json = SerializeException(exception, false);

        // Add additional information.
        if (parameters != null)
        {
            NamerFactory.AdditionalInformation = string.Join("_", parameters);
        }

        Approvals.VerifyWithExtension(json, ".json");
    }

    public static void MatchTopLevelError(Exception exception, string[] parameters = null)
    {
        string json = SerializeException(exception, true);

        // Add additional information.
        if (parameters != null)
        {
            NamerFactory.AdditionalInformation = string.Join("_", parameters);
        }

        Approvals.VerifyWithExtension(json, ".json");
    }

    public static void MatchError(Action func, string[] parameters = null)
    {
        Exception exception = null;

        try
        {
            func();
        }
        catch (Exception err)
        {
            exception = err;
        }

        MatchError(exception, parameters);
    }

    public static async Task MatchError(Func<Task> func, string[] parameters = null)
    {
        Exception exception = null;

        try
        {
            await func();
        }
        catch (Exception err)
        {
            exception = err;
        }

        MatchError(exception, parameters);
    }

    public static void MatchTopLevelError(Action func, string[] parameters = null)
    {
        Exception exception = null;

        try
        {
            func();
        }
        catch (Exception err)
        {
            exception = err;
        }

        MatchTopLevelError(exception, parameters);
    }

    public static async Task MatchTopLevelError(Func<Task> func, string[] parameters = null)
    {
        Exception exception = null;

        try
        {
            await func();
        }
        catch (Exception err)
        {
            exception = err;
        }

        MatchTopLevelError(exception, parameters);
    }

    private static string SerializeException(Exception exception, bool topLevelOnly)
    {
        // Spread the exception chain into a list.
        var exceptions = new List<Exception>();

        if (topLevelOnly)
        {
            exceptions.Add(exception);
        }
        else
        {
            while (exception != null)
            {
                exceptions.Add(exception);
                exception = exception.InnerException;
            }
        }

        // Clear all properties that appear in the `IgnoredExceptionFields`.
        string json = JsonConvert.SerializeObject(exceptions);
        var jsonExceptions = JArray.Parse(json);
        foreach (var obj in jsonExceptions.Cast<JObject>())
        {
            foreach (string ignoredPropertyName in IgnoredExceptionFields)
            {
                obj.Property(ignoredPropertyName)?.Remove();
            }
        }

        json = Serialize(jsonExceptions);

        return json;
    }

    private static string Serialize(object obj)
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = SortedPropertiesContractResolver.Instance,
        };

        string json = JsonConvert.SerializeObject(obj, settings);

        string formatJson = JsonPrettyPrint.FormatJson(json);

        return formatJson;
    }
}

public class CustomReporter : IApprovalFailureReporter
{
    public void Report(string approvedFilePath, string receivedFilePath)
    {
        // Create the approved file if it doesn't exist.
        if (!File.Exists(approvedFilePath))
        {
            File.WriteAllText(approvedFilePath, "");
        }

        string approvedContent = File.ReadAllText(approvedFilePath);
        string receivedContent = File.ReadAllText(receivedFilePath);

        try
        {
            Assert.Equal(approvedContent, receivedContent);
        }
        catch (EqualException ex)
        {
            string message = ex.Message;
            message += "\n";
            message += new string('=', 30);
            message += "\n\n\n";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                message += $"winmergeu '{receivedFilePath}' '{approvedFilePath}'";
            }
            else
            {
                message += $"kdiff3 '{receivedFilePath}' '{approvedFilePath}'";
            }

            message += "\n\n\n";
            message += new string('=', 30);
            message += "\n\n\n";

            message += $"mv '{receivedFilePath}' '{approvedFilePath}'";

            message += "\n\n\n";
            message += new string('=', 30);

            var field = typeof(EqualException).GetField("message", BindingFlags.Instance | BindingFlags.NonPublic);

            field!.SetValue(ex, message);

            throw;
        }
    }
}

public static class JsonPrettyPrint
{
    private const string INDENT_STRING = "  ";

    public static string FormatJson(string str)
    {
        int indent = 0;

        bool quoted = false;

        var sb = new StringBuilder();

        for (int i = 0; i < str.Length; i++)
        {
            char ch = str[i];

            switch (ch)
            {
                case '{':
                {
                    sb.Append(ch);
                    if (!quoted)
                    {
                        if (str[i + 1] != '}')
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(_ => sb.Append(INDENT_STRING));
                        }
                    }

                    break;
                }
                case '[':
                {
                    sb.Append(ch);
                    if (!quoted)
                    {
                        if (str[i + 1] != ']')
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(_ => sb.Append(INDENT_STRING));
                        }
                    }

                    break;
                }
                case '}':
                {
                    if (!quoted)
                    {
                        if (str[i - 1] != '{')
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(_ => sb.Append(INDENT_STRING));
                        }
                    }

                    sb.Append(ch);
                    break;
                }
                case ']':
                {
                    if (!quoted)
                    {
                        if (str[i - 1] != '[')
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(_ => sb.Append(INDENT_STRING));
                        }
                    }

                    sb.Append(ch);
                    break;
                }
                case '"':
                {
                    sb.Append(ch);
                    bool escaped = false;
                    int index = i;
                    while (index > 0 && str[--index] == '\\')
                    {
                        escaped = !escaped;
                    }

                    if (!escaped)
                    {
                        quoted = !quoted;
                    }

                    break;
                }
                case ',':
                {
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.AppendLine();
                        Enumerable.Range(0, indent).ForEach(_ => sb.Append(INDENT_STRING));
                    }

                    break;
                }
                case ':':
                {
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.Append(" ");
                    }

                    break;
                }
                default:
                {
                    sb.Append(ch);
                    break;
                }
            }
        }

        return sb.ToString();
    }
}

public class SortedPropertiesContractResolver : DefaultContractResolver
{
    // use a static instance for optimal performance

    static SortedPropertiesContractResolver()
    {
        Instance = new SortedPropertiesContractResolver();
    }

    public static SortedPropertiesContractResolver Instance { get; }

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var properties = base.CreateProperties(type, memberSerialization);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (properties != null)
        {
            return properties.OrderBy(p => p.UnderlyingName).ToList();
        }

        return properties;
    }
}
