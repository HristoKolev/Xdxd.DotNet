namespace Xdxd.DotNet.Testing;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Logging;
using Npgsql;
using NpgsqlTypes;
using Postgres;
using Shared;

public class ErrorReporterMock : ErrorReporter, IAsyncDisposable
{
    private readonly ErrorReporterMockConfig config;
    private const string ZERO_GUID = "61289445-04b7-4f59-bbdd-499c36861bc0";

    private ErrorReporter innerReporter;

    public List<(Exception, string, Dictionary<string, object>)> Errors { get; } = new();

    public ErrorReporterMock() : this(new ErrorReporterMockConfig()) { }

    public Exception SingleException
    {
        get
        {
            if (this.Errors.Count > 1)
            {
                throw new ApplicationException("SingleException is called with more that 1 error in the list.");
            }

            if (this.Errors.Count == 0)
            {
                throw new ApplicationException("SingleException is called with zero errors in the list.");
            }

            var exception = this.Errors.Single().Item1;

            if (exception is DetailedException dex)
            {
                foreach ((string key, object value) in this.Errors.SelectMany(tuple => tuple.Item3))
                {
                    dex.Details.Add(key, value);
                }
            }

            return exception;
        }
    }

    public ErrorReporterMock(ErrorReporterMockConfig config)
    {
        this.config = config;
    }

    public Task<string> Error(Exception exception, string explicitFingerprint, Dictionary<string, object> additionalInfo)
    {
        this.Errors.Add((exception, explicitFingerprint, additionalInfo));

        try
        {
            this.innerReporter?.Error(exception, explicitFingerprint, additionalInfo);
        }
        catch
        {
            // ignore
        }

        return Task.FromResult(ZERO_GUID);
    }

    public Task<string> Error(Exception exception, Dictionary<string, object> additionalInfo)
    {
        return this.Error(exception, null, additionalInfo);
    }

    public Task<string> Error(Exception exception, string explicitFingerprint)
    {
        return this.Error(exception, explicitFingerprint, null);
    }

    public Task<string> Error(Exception exception)
    {
        return this.Error(exception, null, null);
    }

    public void SetInnerReporter(ErrorReporter errorReporter)
    {
        this.innerReporter = errorReporter;
    }

    public void AddDataHook(Func<Dictionary<string, object>> hook)
    {
        this.innerReporter?.AddDataHook(hook);
    }

    public ValueTask DisposeAsync()
    {
        if (this.config.ThrowFirstErrorOnDispose && this.Errors.Count > 0)
        {
            var firstException = this.Errors.First().Item1;

            ExceptionDispatchInfo.Capture(firstException).Throw();
        }

        return new ValueTask();
    }
}

public class ErrorReporterMockConfig
{
    public bool ThrowFirstErrorOnDispose { get; set; } = true;
}

public class RngServiceMock : RngService
{
    public string GenerateSecureString(int length)
    {
        return new('X', length);
    }
}

public class StructuredLogMock : Log
{
    public Dictionary<string, List<object>> Logs { get; } = new();

    public void Log<T>(string eventStreamName, Func<T> func)
    {
        var item = func();

        if (!this.Logs.ContainsKey(eventStreamName))
        {
            this.Logs.Add(eventStreamName, new List<object> { item });
        }
        else
        {
            this.Logs[eventStreamName].Add(item);
        }
    }
}

public class DbServiceStub<TPocos> : IDbService<TPocos> where TPocos : IDbPocos<TPocos>, new()
{
    public Task<DbTransaction> BeginTransaction()
    {
        return Task.FromResult((DbTransaction)new DbTransactionStub());
    }

    public void Dispose() { }

    public TPocos Poco => throw new NotImplementedException();

    public Task<int> ExecuteNonQuery(string sql, IEnumerable<NpgsqlParameter> parameters, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> ExecuteNonQuery(string sql, params NpgsqlParameter[] parameters)
    {
        throw new NotImplementedException();
    }

    public Task<int> ExecuteNonQuery(string sql, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> ExecuteScalar<T>(string sql, IEnumerable<NpgsqlParameter> parameters, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> ExecuteScalar<T>(string sql, params NpgsqlParameter[] parameters)
    {
        throw new NotImplementedException();
    }

    public Task<T> ExecuteScalar<T>(string sql, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<T>> Query<T>(string sql, IEnumerable<NpgsqlParameter> parameters, CancellationToken cancellationToken = default) where T : new()
    {
        throw new NotImplementedException();
    }

    public Task<List<T>> Query<T>(string sql, params NpgsqlParameter[] parameters) where T : new()
    {
        throw new NotImplementedException();
    }

    public Task<List<T>> Query<T>(string sql, CancellationToken cancellationToken = default) where T : new()
    {
        throw new NotImplementedException();
    }

    public Task<T> QueryOne<T>(string sql, IEnumerable<NpgsqlParameter> parameters, CancellationToken cancellationToken = default) where T : class, new()
    {
        throw new NotImplementedException();
    }

    public Task<T> QueryOne<T>(string sql, CancellationToken cancellationToken = default) where T : class, new()
    {
        throw new NotImplementedException();
    }

    public Task<T> QueryOne<T>(string sql, params NpgsqlParameter[] parameters) where T : class, new()
    {
        throw new NotImplementedException();
    }

    public Task<T> FindByID<T>(int id, CancellationToken cancellationToken = default) where T : class, IPoco<T>, new()
    {
        throw new NotImplementedException();
    }

    public NpgsqlParameter CreateParameter<T>(string parameterName, T value)
    {
        throw new NotImplementedException();
    }

    public NpgsqlParameter CreateParameter<T>(string parameterName, T value, NpgsqlDbType dbType)
    {
        throw new NotImplementedException();
    }

    public NpgsqlParameter CreateParameter(string parameterName, object value)
    {
        throw new NotImplementedException();
    }

    public Task<int> Insert<T>(T poco, CancellationToken cancellationToken = default) where T : IPoco<T>
    {
        throw new NotImplementedException();
    }

    public Task<int> InsertWithoutMutating<T>(T poco, CancellationToken cancellationToken = default) where T : IPoco<T>
    {
        throw new NotImplementedException();
    }

    public Task Update<T>(T poco, CancellationToken cancellationToken = default) where T : class, IPoco<T>
    {
        throw new NotImplementedException();
    }

    public Task<int> Save<T>(T poco, CancellationToken cancellationToken = default) where T : class, IPoco<T>
    {
        throw new NotImplementedException();
    }

    public Task Delete<T>(T poco, CancellationToken cancellationToken = default) where T : IPoco<T>
    {
        throw new NotImplementedException();
    }

    public Task<int> Delete<T>(int[] ids, CancellationToken cancellationToken = default) where T : IPoco<T>
    {
        throw new NotImplementedException();
    }

    public Task Delete<T>(int id, CancellationToken cancellationToken = default) where T : IPoco<T>
    {
        throw new NotImplementedException();
    }

    public Task BulkInsert<T>(IEnumerable<T> pocos, CancellationToken cancellationToken = default) where T : IPoco<T>
    {
        throw new NotImplementedException();
    }

    public Task Copy<T>(IEnumerable<T> pocos) where T : IPoco<T>
    {
        throw new NotImplementedException();
    }

    public string GetCopyHeader<T>() where T : IReadOnlyPoco<T>
    {
        throw new NotImplementedException();
    }
}

public class DbTransactionStub : DbTransaction
{
    public override Task CommitAsync(CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public override Task RollbackAsync(CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing) { }

    public override ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected override DbConnection DbConnection => throw new NotImplementedException();

    public override IsolationLevel IsolationLevel => throw new NotImplementedException();

    public override void Commit()
    {
        throw new NotImplementedException();
    }

    public override void Rollback()
    {
        throw new NotImplementedException();
    }

    public override Task SaveAsync(string savepointName, CancellationToken cancellationToken = new())
    {
        throw new NotImplementedException();
    }

    public override Task RollbackAsync(string savepointName, CancellationToken cancellationToken = new())
    {
        throw new NotImplementedException();
    }

    public override Task ReleaseAsync(string savepointName, CancellationToken cancellationToken = new())
    {
        throw new NotImplementedException();
    }

    public override void Save(string savepointName)
    {
        throw new NotImplementedException();
    }

    public override void Rollback(string savepointName)
    {
        throw new NotImplementedException();
    }

    public override void Release(string savepointName)
    {
        throw new NotImplementedException();
    }

    public override bool SupportsSavepoints => throw new NotImplementedException();
}
