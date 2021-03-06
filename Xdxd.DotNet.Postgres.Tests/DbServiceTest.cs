namespace Xdxd.DotNet.Postgres.Tests;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Testing;
using Xunit;

public class ConnectionExtensionsTest : TestPocosDatabaseTest
{
    [Fact]
    public async Task ExecuteNonQuery1()
    {
        int number = 123;

        var parameters = new[]
        {
            this.Connection.CreateParameter("n", number),
        };

        await this.Connection.ExecuteNonQuery(@"
                INSERT into public.test2 
                (test_name, test_date, test_number) values
                ('enq_test', now(), :n);
            ", parameters, CancellationToken.None);

        int result = await this.Connection.ExecuteScalar<int>(
            "select test_number from public.test2 where test_name = 'enq_test';"
        );

        Assert.Equal(number, result);
    }

    [Fact]
    public async Task ExecuteNonQuery2()
    {
        int number = 123;

        await this.Connection.ExecuteNonQuery($@"
                INSERT into public.test2 
                (test_name, test_date, test_number) values
                ('enq_test', now(), {number});
            ");

        int result = await this.Connection.ExecuteScalar<int>(
            "select test_number from public.test2 where test_name = 'enq_test';"
        );

        Assert.Equal(number, result);
    }

    [Fact]
    public async Task ExecuteNonQuery3()
    {
        int number = 123;

        await this.Connection.ExecuteNonQuery(@"
                INSERT into public.test2 
                (test_name, test_date, test_number) values
                ('enq_test', now(), :n);
            ", this.Connection.CreateParameter("n", number));

        int result = await this.Connection.ExecuteScalar<int>(
            "select test_number from public.test2 where test_name = 'enq_test';"
        );

        Assert.Equal(number, result);
    }

    [Fact]
    public async Task ExecuteScalar1()
    {
        int number = 123;
        var parameters = new[] { this.Connection.CreateParameter("n", number) };
        int result = await this.Connection.ExecuteScalar<int>("select :n;", parameters, CancellationToken.None);
        Assert.Equal(number, result);
    }

    [Fact]
    public async Task ExecuteScalar2()
    {
        int number = 123;
        int result = await this.Connection.ExecuteScalar<int>($"select {number};");
        Assert.Equal(number, result);
    }

    [Fact]
    public async Task ExecuteScalar3()
    {
        int number = 123;
        int result = await this.Connection.ExecuteScalar<int>("select :n;", this.Connection.CreateParameter("n", number));
        Assert.Equal(number, result);
    }

    [Fact]
    public async Task Query1()
    {
        int number = 3;

        var parameters = new[]
        {
            this.Connection.CreateParameter("n", number),
        };

        var result = await this.Connection.Query<Test2Model>(
            "select test_id, test_name, test_number from test2 where test_number = :n;",
            parameters,
            CancellationToken.None
        );

        Snapshot.Match(result);
    }

    [Fact]
    public async Task Query2()
    {
        int number = 3;

        var result = await this.Connection.Query<Test2Model>(
            $"select test_id, test_name, test_number from test2 where test_number = {number};"
        );

        Snapshot.Match(result);
    }

    [Fact]
    public async Task Query3()
    {
        int number = 3;

        var result = await this.Connection.Query<Test2Model>(
            "select test_id, test_name, test_number from test2 where test_number = :n;",
            this.Connection.CreateParameter("n", number)
        );

        Snapshot.Match(result);
    }

    [Fact]
    public async Task QueryOne1()
    {
        int number = 3;

        var parameters = new[]
        {
            this.Connection.CreateParameter("n", number),
        };

        var result = await this.Connection.QueryOne<Test2Model>(
            "select test_id, test_name, test_number from test2 where test_number = :n;",
            parameters,
            CancellationToken.None
        );

        Snapshot.Match(result);
    }

    [Fact]
    public async Task QueryOne2()
    {
        int number = 3;

        var result = await this.Connection.QueryOne<Test2Model>(
            $"select test_id, test_name, test_number from test2 where test_number = {number};"
        );

        Snapshot.Match(result);
    }

    [Fact]
    public async Task QueryOne3()
    {
        int number = 3;

        var result = await this.Connection.QueryOne<Test2Model>(
            "select test_id, test_name, test_number from test2 where test_number = :n;",
            this.Connection.CreateParameter("n", number)
        );

        Snapshot.Match(result);
    }

    [Fact]
    public async Task ExecuteNonQuery_throws_on_null_connection()
    {
        await Snapshot.MatchError(async () =>
        {
            await NpgsqlConnectionExtensions.ExecuteNonQuery(null, "select 1;", Array.Empty<NpgsqlParameter>(), CancellationToken.None);
        });
    }

    [Fact]
    public async Task ExecuteNonQuery_throws_on_null_sql()
    {
        await Snapshot.MatchError(async () =>
        {
            await this.Connection.ExecuteNonQuery(null, Array.Empty<NpgsqlParameter>(), CancellationToken.None);
        });
    }

    [Fact]
    public async Task ExecuteNonQuery_throws_on_null_parameters()
    {
        await Snapshot.MatchError(async () =>
        {
            await this.Connection.ExecuteNonQuery("select 1;", null, CancellationToken.None);
        });
    }

    [Fact]
    public async Task ExecuteScalar_throws_on_null_connection()
    {
        await Snapshot.MatchError(async () =>
        {
            await NpgsqlConnectionExtensions.ExecuteScalar<int>(null, "select 1;", Array.Empty<NpgsqlParameter>(), CancellationToken.None);
        });
    }

    [Fact]
    public async Task ExecuteScalar_throws_on_null_sql()
    {
        await Snapshot.MatchError(async () =>
        {
            await this.Connection.ExecuteScalar<int>(null, Array.Empty<NpgsqlParameter>(), CancellationToken.None);
        });
    }

    [Fact]
    public async Task ExecuteScalar_throws_on_null_parameters()
    {
        await Snapshot.MatchError(async () =>
        {
            await this.Connection.ExecuteScalar<int>("select 1;", null, CancellationToken.None);
        });
    }

    [Fact]
    public async Task ExecuteScalar_throws_on_empty_result_set()
    {
        Exception exception = null;

        try
        {
            await this.Connection.ExecuteScalar<int>("select 1 where false;");
        }
        catch (Exception err)
        {
            exception = err;
        }

        Snapshot.MatchError(exception);
    }

    [Fact]
    public async Task ExecuteScalar_throws_on_no_columns()
    {
        Exception exception = null;

        try
        {
            await this.Connection.ExecuteScalar<int>("create table test22 (); select * from test22;");
        }
        catch (Exception err)
        {
            exception = err;
        }

        Snapshot.MatchError(exception);
    }

    [Fact]
    public async Task ExecuteScalar_throws_on_more_that_one_column()
    {
        Exception exception = null;

        try
        {
            await this.Connection.ExecuteScalar<int>("select 1,1;");
        }
        catch (Exception err)
        {
            exception = err;
        }

        Snapshot.MatchError(exception);
    }

    [Fact]
    public async Task ExecuteScalar_throws_on_more_that_one_row()
    {
        Exception exception = null;

        try
        {
            await this.Connection.ExecuteScalar<int>("select * from (values (1), (2)) as x;");
        }
        catch (Exception err)
        {
            exception = err;
        }

        Snapshot.MatchError(exception);
    }

    [Fact]
    public async Task ExecuteScalar_throws_when_null_is_selected_for_value_type()
    {
        Exception exception = null;

        try
        {
            await this.Connection.ExecuteScalar<int>("select null;");
        }
        catch (Exception err)
        {
            exception = err;
        }

        Snapshot.MatchError(exception);
    }

    [Fact]
    public async Task ExecuteScalar_can_return_null()
    {
        string x = await this.Connection.ExecuteScalar<string>("select null;");

        Assert.Null(x);
    }

    [Fact]
    public async Task Query_throws_on_null_connection()
    {
        await Snapshot.MatchError(async () =>
        {
            await NpgsqlConnectionExtensions.Query<TestRow>(null, "select 1;", Array.Empty<NpgsqlParameter>(), CancellationToken.None);
        });
    }

    [Fact]
    public async Task Query_throws_on_null_sql()
    {
        await Snapshot.MatchError(async () =>
        {
            await this.Connection.Query<TestRow>(null, Array.Empty<NpgsqlParameter>(), CancellationToken.None);
        });
    }

    [Fact]
    public async Task Query_throws_on_null_parameters()
    {
        await Snapshot.MatchError(async () =>
        {
            await this.Connection.Query<TestRow>("select 1;", null, CancellationToken.None);
        });
    }

    [Fact]
    public async Task Query_handles_null_values()
    {
        var rows = await this.Connection.Query<TestRow>("select null as col1, null as col2;");
        var row = rows.First();

        Assert.Null(row.Col1);
        Assert.Null(row.Col2);
    }

    [Fact]
    public async Task QueryOne_throws_on_null_connection()
    {
        await Snapshot.MatchError(async () =>
        {
            await NpgsqlConnectionExtensions.QueryOne<TestRow>(null, "select 1;", Array.Empty<NpgsqlParameter>(), CancellationToken.None);
        });
    }

    [Fact]
    public async Task QueryOne_throws_on_null_sql()
    {
        await Snapshot.MatchError(async () =>
        {
            await this.Connection.QueryOne<TestRow>(null, Array.Empty<NpgsqlParameter>(), CancellationToken.None);
        });
    }

    [Fact]
    public async Task QueryOne_throws_on_null_parameters()
    {
        await Snapshot.MatchError(async () =>
        {
            await this.Connection.QueryOne<TestRow>("select 1;", null, CancellationToken.None);
        });
    }

    [Fact]
    public async Task QueryOne_returns_null_on_no_rows()
    {
        var row = await this.Connection.QueryOne<TestRow>("select 1 where false;");

        Assert.Null(row);
    }

    [Fact]
    public async Task QueryOne_throws_on_more_than_one_row()
    {
        Exception exception = null;

        try
        {
            await this.Connection.QueryOne<TestRow>("select * from (values (1, 2), (3, 4)) as x(col1, col2);");
        }
        catch (Exception err)
        {
            exception = err;
        }

        Snapshot.MatchError(exception);
    }

    [Fact]
    public void CreateParameter_with_npgsql_type()
    {
        var parameter1 = this.Connection.CreateParameter("p1", 1, NpgsqlDbType.Integer);
        Assert.Equal(1, (int)parameter1.Value);
        var parameter2 = this.Connection.CreateParameter<string>("p1", null, NpgsqlDbType.Text);
        Assert.Equal(DBNull.Value, parameter2.Value);
    }

    [Fact]
    public void CreateParameter_boxed()
    {
        var parameter1 = this.Connection.CreateParameter("p1", (object)1);
        Assert.Equal(1, (int)parameter1.Value!);
    }

    [Fact]
    public void CreateParameter_with_inferred_npgsql_type()
    {
        var parameter = this.Connection.CreateParameter("p1", 1);
        Assert.Equal(1, (int)parameter.Value!);
    }

    [Fact]
    public void CreateParameter_with_inferred_npgsql_type_throws_the_type_cannot_be_inferred()
    {
        Snapshot.MatchError(() =>
        {
            this.Connection.CreateParameter("p1", new TestRow());
        });
    }
}

public class DbServiceTest : TestPocosDatabaseTest
{
    [Fact]
    public async Task ExecuteNonQuery1()
    {
        int number = 123;

        var parameters = new[]
        {
            this.Db.CreateParameter("n", number),
        };

        await this.Db.ExecuteNonQuery(@"
                INSERT into public.test2 
                (test_name, test_date, test_number) values
                ('enq_test', now(), :n);
            ", parameters, CancellationToken.None);

        int result = await this.Db.ExecuteScalar<int>(
            "select test_number from public.test2 where test_name = 'enq_test';"
        );

        Assert.Equal(number, result);
    }

    [Fact]
    public async Task ExecuteNonQuery2()
    {
        int number = 123;

        await this.Db.ExecuteNonQuery($@"
                INSERT into public.test2 
                (test_name, test_date, test_number) values
                ('enq_test', now(), {number});
            ");

        int result = await this.Db.ExecuteScalar<int>(
            "select test_number from public.test2 where test_name = 'enq_test';"
        );

        Assert.Equal(number, result);
    }

    [Fact]
    public async Task ExecuteNonQuery3()
    {
        int number = 123;

        await this.Db.ExecuteNonQuery(@"
                INSERT into public.test2 
                (test_name, test_date, test_number) values
                ('enq_test', now(), :n);
            ", this.Db.CreateParameter("n", number));

        int result = await this.Db.ExecuteScalar<int>(
            "select test_number from public.test2 where test_name = 'enq_test';"
        );

        Assert.Equal(number, result);
    }

    [Fact]
    public async Task ExecuteScalar1()
    {
        int number = 123;
        var parameters = new[] { this.Db.CreateParameter("n", number) };
        int result = await this.Db.ExecuteScalar<int>("select :n;", parameters, CancellationToken.None);
        Assert.Equal(number, result);
    }

    [Fact]
    public async Task ExecuteScalar2()
    {
        int number = 123;
        int result = await this.Db.ExecuteScalar<int>($"select {number};");
        Assert.Equal(number, result);
    }

    [Fact]
    public async Task ExecuteScalar3()
    {
        int number = 123;
        int result = await this.Db.ExecuteScalar<int>("select :n;", this.Db.CreateParameter("n", number));
        Assert.Equal(number, result);
    }

    [Fact]
    public async Task Query1()
    {
        int number = 3;

        var parameters = new[]
        {
            this.Db.CreateParameter("n", number),
        };

        var result = await this.Db.Query<Test2Model>(
            "select test_id, test_name, test_number from test2 where test_number = :n;",
            parameters,
            CancellationToken.None
        );

        Snapshot.Match(result);
    }

    [Fact]
    public async Task Query2()
    {
        int number = 3;

        var result = await this.Db.Query<Test2Model>(
            $"select test_id, test_name, test_number from test2 where test_number = {number};"
        );

        Snapshot.Match(result);
    }

    [Fact]
    public async Task Query3()
    {
        int number = 3;

        var result = await this.Db.Query<Test2Model>(
            "select test_id, test_name, test_number from test2 where test_number = :n;",
            this.Db.CreateParameter("n", number)
        );

        Snapshot.Match(result);
    }

    [Fact]
    public async Task QueryOne1()
    {
        int number = 3;

        var parameters = new[]
        {
            this.Db.CreateParameter("n", number),
        };

        var result = await this.Db.QueryOne<Test2Model>(
            "select test_id, test_name, test_number from test2 where test_number = :n;",
            parameters,
            CancellationToken.None
        );

        Snapshot.Match(result);
    }

    [Fact]
    public async Task QueryOne2()
    {
        int number = 3;

        var result = await this.Db.QueryOne<Test2Model>(
            $"select test_id, test_name, test_number from test2 where test_number = {number};"
        );

        Snapshot.Match(result);
    }

    [Fact]
    public async Task QueryOne3()
    {
        int number = 3;

        var result = await this.Db.QueryOne<Test2Model>(
            "select test_id, test_name, test_number from test2 where test_number = :n;",
            this.Db.CreateParameter("n", number)
        );

        Snapshot.Match(result);
    }

    [Fact]
    public void CreateParameter_with_npgsql_type()
    {
        var parameter1 = this.Db.CreateParameter("p1", 1, NpgsqlDbType.Integer);
        Assert.Equal(1, (int)parameter1.Value!);
        var parameter2 = this.Db.CreateParameter<string>("p1", null, NpgsqlDbType.Text);
        Assert.Equal(DBNull.Value, parameter2.Value);
    }

    [Fact]
    public void CreateParameter_boxed()
    {
        var parameter1 = this.Db.CreateParameter("p1", (object)1);
        Assert.Equal(1, (int)parameter1.Value!);
    }

    [Fact]
    public void CreateParameter_with_inferred_npgsql_type()
    {
        var parameter = this.Db.CreateParameter("p1", 1);
        Assert.Equal(1, (int)parameter.Value!);
    }

    [Fact]
    public void CreateParameter_with_inferred_npgsql_type_throws_the_type_cannot_be_inferred()
    {
        Snapshot.MatchError(() =>
        {
            this.Db.CreateParameter("p1", new TestRow());
        });
    }

    [Fact]
    public async Task Delete_by_ids_does_nothing_if_the_ids_array_is_empty()
    {
        int deletedCount = await this.Db.Delete<Test1Poco>(Array.Empty<int>());
        Assert.Equal(0, deletedCount);
    }

    [Fact]
    public async Task Update_throws_when_the_poco_is_new()
    {
        var poco = new Test1Poco();

        await Snapshot.MatchError(async () =>
        {
            await this.Db.Update(poco);
        });
    }

    [Fact]
    public async Task Save_works_when_the_poco_is_new()
    {
        var poco = new Test1Poco
        {
            TestName1 = "test",
            TestText2 = "test",
            TestChar2 = "c",
        };

        await this.Db.Save(poco);
    }

    [Fact]
    public async Task Save_works_when_the_poco_is_old()
    {
        var poco = new Test1Poco
        {
            TestName1 = "test",
            TestText2 = "test",
            TestChar2 = "c",
        };

        await this.Db.Insert(poco);
        await this.Db.Save(poco);
    }
}

public class Test2Model
{
    public DateTime TestDate { get; set; }

    public int TestID { get; set; }

    public string TestName { get; set; }

    public int TestNumber { get; set; }
}

public class TestRow
{
    public string Col1 { get; set; }

    public string Col2 { get; set; }
}
