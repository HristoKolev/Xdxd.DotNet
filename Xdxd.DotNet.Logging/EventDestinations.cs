namespace Xdxd.DotNet.Logging;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Shared;

/// <summary>
/// Prints events to `stdout`.
/// </summary>
public class ConsoleEventDestination : EventDestination<LogData>
{
    public ConsoleEventDestination(ErrorReporter errorReporter) : base(errorReporter) { }

    protected override async ValueTask Flush(ArraySegment<LogData> data)
    {
        for (int i = 0; i < data.Count; i++)
        {
            string json = JsonHelper.Serialize(data[i].Fields);
            await Console.Out.WriteLineAsync(json);
        }
    }
}

public class CustomStreamContent : HttpContent
{
    private readonly Func<Stream, Task> func;

    public CustomStreamContent(Func<Stream, Task> func)
    {
        this.func = func;
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
    {
        return this.func(stream);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }
}

/// <summary>
/// Using this extension method allows us to not specify the stream name and the event data structure.
/// </summary>
public static class GeneralLoggingExtensions
{
    public const string GENERAL_EVENT_STREAM = "GENERAL_LOG";

    public static void General(this Log log, Func<LogData> func)
    {
        log.Log(GENERAL_EVENT_STREAM, func);
    }
}

public class AppInfoEventData
{
    public virtual string InstanceName { get; set; }

    public virtual string Environment { get; set; }

    public virtual string AppVersion { get; set; }
}

/// <summary>
/// This is used as a most general log data structure.
/// </summary>
public class LogData : AppInfoEventData, IEnumerable
{
    private const string INSTANCE_NAME_KEY = "instanceName";
    private const string ENVIRONMENT_KEY = "environment";
    private const string APP_VERSION_KEY = "appVersion";

    public Dictionary<string, object> Fields { get; } = new Dictionary<string, object>();

    public LogData(string message)
    {
        this.Fields.Add("message", message);
    }

    /// <summary>
    /// This is not meant to be used explicitly, but with he collection initialization syntax.
    /// </summary>
    public void Add(string key, object val)
    {
        this.Fields.Add(key, val);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public static implicit operator LogData(string x)
    {
        return new LogData(x);
    }

    public override string InstanceName
    {
        get => (string)this.Fields[INSTANCE_NAME_KEY];
        set => this.Fields[INSTANCE_NAME_KEY] = value;
    }

    public override string Environment
    {
        get => (string)this.Fields[ENVIRONMENT_KEY];
        set => this.Fields[ENVIRONMENT_KEY] = value;
    }

    public override string AppVersion
    {
        get => (string)this.Fields[APP_VERSION_KEY];
        set => this.Fields[APP_VERSION_KEY] = value;
    }
}

public class LogPreprocessor : EventPreprocessor
{
    private readonly DateTimeService dateTimeService;
    private readonly LogPreprocessorConfig logPreprocessorConfig;

    public LogPreprocessor(DateTimeService dateTimeService, LogPreprocessorConfig logPreprocessorConfig)
    {
        this.dateTimeService = dateTimeService;
        this.logPreprocessorConfig = logPreprocessorConfig;
    }

    public void ProcessItem<TData>(ref TData item)
    {
        if (item is LogData logData)
        {
            logData.Fields.Add("log_date", this.dateTimeService.EventTime().ToString("O"));
        }

        if (item is AppInfoEventData appInfoEventData)
        {
            appInfoEventData.AppVersion = this.logPreprocessorConfig.AppVersion;
            appInfoEventData.Environment = this.logPreprocessorConfig.Environment;
            appInfoEventData.InstanceName = this.logPreprocessorConfig.InstanceName;
        }
    }
}

public class LogPreprocessorConfig
{
    public string InstanceName { get; set; }

    public string Environment { get; set; }

    public string AppVersion { get; set; }
}
