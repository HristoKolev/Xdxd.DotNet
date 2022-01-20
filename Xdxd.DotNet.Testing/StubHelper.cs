namespace Xdxd.DotNet.Testing;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Logging;
using Shared;

public static class StubHelper
{
    public static DateTime Date3000 => Testing.DateTimeServiceStub.Date3000;

    public static DateTimeService DateTimeServiceStub => new DateTimeServiceStub();

    public static Log LogStub => new LogStub();

    public static ErrorReporter ErrorReporterStub => new ErrorReporterStub();
}

public class LogStub : Log
{
    public void Log<TEventData>(string eventStreamName, Func<TEventData> item) { }
}

public class ErrorReporterStub : ErrorReporter
{
    public Task<string> Error(Exception exception, string explicitFingerprint, Dictionary<string, object> additionalInfo)
    {
        return Task.FromResult<string>(null);
    }

    public Task<string> Error(Exception exception, Dictionary<string, object> additionalInfo)
    {
        return Task.FromResult<string>(null);
    }

    public Task<string> Error(Exception exception, string explicitFingerprint)
    {
        return Task.FromResult<string>(null);
    }

    public Task<string> Error(Exception exception)
    {
        return Task.FromResult<string>(null);
    }

    public void SetInnerReporter(ErrorReporter errorReporter) { }

    public void AddDataHook(Func<Dictionary<string, object>> hook) { }
}

public class DateTimeServiceStub : DateTimeService
{
    public static DateTime Date3000 = new(3000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public DateTime EventTime()
    {
        return Date3000;
    }

    public DateTime CurrentTime()
    {
        return Date3000;
    }
}
