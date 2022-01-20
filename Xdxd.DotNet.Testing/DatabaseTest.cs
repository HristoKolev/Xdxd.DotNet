namespace Xdxd.DotNet.Testing;

using System;
using System.Threading.Tasks;
using Npgsql;
using Postgres;
using Xunit;

public abstract class DatabaseTest<TDb, TPocos> : IAsyncLifetime
    where TDb : IDbService<TPocos> where TPocos : IDbPocos<TPocos>, new()
{
    private readonly string stageSqlFileName;
    private readonly Func<NpgsqlConnection, TDb> createDbService;

    private readonly string testMasterConnectionString;
    private NpgsqlConnection testMasterConnection;

    private string testDatabaseName;
    private TDb testDb;

    protected NpgsqlConnection Connection { get; private set; }

    protected TDb Db => this.testDb;

    protected string ConnectionString { get; private set; }

    protected DatabaseTest(string stageSqlFileName, string testMasterConnectionString, Func<NpgsqlConnection, TDb> createDbService)
    {
        this.stageSqlFileName = stageSqlFileName;
        this.testMasterConnectionString = testMasterConnectionString;
        this.createDbService = createDbService;
    }

    public virtual async Task InitializeAsync()
    {
        var testMasterConnectionStringBuilder = new NpgsqlConnectionStringBuilder(this.testMasterConnectionString)
        {
            Enlist = false,
            IncludeErrorDetail = true,
        };

        this.testMasterConnection = new NpgsqlConnection(testMasterConnectionStringBuilder.ToString());
        this.testDatabaseName = Guid.NewGuid().ToString().Replace("-", string.Empty);
        await this.testMasterConnection.ExecuteNonQuery($"create database \"{this.testDatabaseName}\";");

        var testConnectionStringBuilder = new NpgsqlConnectionStringBuilder(this.testMasterConnectionString)
        {
            Database = this.testDatabaseName,
            Pooling = false,
            Enlist = false,
            IncludeErrorDetail = true,
        };

        this.ConnectionString = testConnectionStringBuilder.ToString();
        this.Connection = new NpgsqlConnection(this.ConnectionString);
        this.testDb = this.createDbService(this.Connection);

        await this.ExecuteStageSql();
    }

    private async Task ExecuteStageSql()
    {
        string content = TestHelper.GetProjectFileContent(this.stageSqlFileName);

        // This splitting is done to fix a problem (possibly in postgres or npgsql) where if you use a table
        // immediately after declaring it, it would appear to not be there.
        var parts = content.Split("--SPLIT_HERE", StringSplitOptions.RemoveEmptyEntries);

        foreach (string sql in parts)
        {
            await this.Connection.ExecuteNonQuery(sql);
        }
    }

    public virtual async Task DisposeAsync()
    {
        await this.Connection.DisposeAsync();
        this.testDb.Dispose();
        await this.testMasterConnection.ExecuteNonQuery($"drop database \"{this.testDatabaseName}\";");
        await this.testMasterConnection.DisposeAsync();
    }
}
