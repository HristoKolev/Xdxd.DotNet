namespace Xdxd.DotNet.Postgres;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Shared;

/// <summary>
/// Provides extension methods for `NpgsqlConnection` objects that perform high level database operations that don't require information about the specific
/// database schema.
/// </summary>
public static class NpgsqlConnectionExtensions
{
    private static readonly ConcurrentDictionary<Type, object> SettersCache = new();

    private static readonly ConcurrentDictionary<Type, object> GettersCache = new();

    /// <summary>
    /// The default parameter type map that is used when creating parameters without specifying the NpgsqlDbType explicitly.
    /// </summary>
    private static readonly Dictionary<Type, NpgsqlDbType> DefaultNpgsqlDbTypeMap = new()
    {
        { typeof(int), NpgsqlDbType.Integer },
        { typeof(long), NpgsqlDbType.Bigint },
        { typeof(bool), NpgsqlDbType.Boolean },
        { typeof(float), NpgsqlDbType.Real },
        { typeof(double), NpgsqlDbType.Double },
        { typeof(short), NpgsqlDbType.Smallint },
        { typeof(decimal), NpgsqlDbType.Numeric },
        { typeof(string), NpgsqlDbType.Text },
        { typeof(DateTime), NpgsqlDbType.TimestampTz },
        { typeof(byte[]), NpgsqlDbType.Bytea },
        { typeof(int?), NpgsqlDbType.Integer },
        { typeof(long?), NpgsqlDbType.Bigint },
        { typeof(bool?), NpgsqlDbType.Boolean },
        { typeof(float?), NpgsqlDbType.Real },
        { typeof(double?), NpgsqlDbType.Double },
        { typeof(short?), NpgsqlDbType.Smallint },
        { typeof(decimal?), NpgsqlDbType.Numeric },
        { typeof(DateTime?), NpgsqlDbType.TimestampTz },
        { typeof(string[]), NpgsqlDbType.Array | NpgsqlDbType.Text },
        { typeof(int[]), NpgsqlDbType.Array | NpgsqlDbType.Integer },
        { typeof(DateTime[]), NpgsqlDbType.Array | NpgsqlDbType.TimestampTz },
    };

    /// <summary>
    /// Executes a query and returns the rows affected.
    /// </summary>
    public static async Task<int> ExecuteNonQuery(this NpgsqlConnection connection,
        string sql,
        IEnumerable<NpgsqlParameter> parameters,
        CancellationToken cancellationToken = default)
    {
        if (connection == null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        if (sql == null)
        {
            throw new ArgumentNullException(nameof(sql));
        }

        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        await EnsureOpenState(connection, cancellationToken);

        await using (var command = CreateCommand(connection, sql, parameters))
        {
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Executes a query and returns the rows affected.
    /// </summary>
    public static Task<int> ExecuteNonQuery(this NpgsqlConnection connection, string sql, CancellationToken cancellationToken = default)
    {
        return connection.ExecuteNonQuery(sql, Array.Empty<NpgsqlParameter>(), cancellationToken);
    }

    /// <summary>
    /// Executes a query and returns the rows affected.
    /// </summary>
    public static Task<int> ExecuteNonQuery(this NpgsqlConnection connection, string sql, params NpgsqlParameter[] parameters)
    {
        return connection.ExecuteNonQuery(sql, parameters, CancellationToken.None);
    }

    /// <summary>
    /// Executes a query and returns a scalar value of type T.
    /// It throws if the result set does not have exactly one column and one row.
    /// It throws if the return value is 'null' and the type T is a value type.
    /// </summary>
    public static async Task<T> ExecuteScalar<T>(this NpgsqlConnection connection,
        string sql,
        IEnumerable<NpgsqlParameter> parameters,
        CancellationToken cancellationToken = default)
    {
        if (connection == null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        if (sql == null)
        {
            throw new ArgumentNullException(nameof(sql));
        }

        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        await EnsureOpenState(connection, cancellationToken);

        await using (var command = CreateCommand(connection, sql, parameters))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (reader.FieldCount == 0)
            {
                throw new ApplicationException("No columns returned for query that expected exactly one column.");
            }

            if (reader.FieldCount > 1)
            {
                throw new ApplicationException("More than one column returned for query that expected exactly one column.");
            }

            bool hasRow = await reader.ReadAsync(cancellationToken);

            if (!hasRow)
            {
                throw new ApplicationException("No rows returned for query that expected exactly one row.");
            }

            var value = reader.GetValue(0);

            bool hasMoreRows = await reader.ReadAsync(cancellationToken);

            if (hasMoreRows)
            {
                throw new ApplicationException("More than one row returned for query that expected exactly one row.");
            }

            if (value is DBNull)
            {
                if (default(T) == null)
                {
                    value = null;
                }
                else
                {
                    throw new ApplicationException("Cannot cast DBNull value to a value type parameter.");
                }
            }

            return (T)value;
        }
    }

    /// <summary>
    /// Executes a query and returns a scalar value of type T.
    /// It throws if the result set does not have exactly one column and one row.
    /// It throws if the return value is 'null' and the type T is a value type.
    /// </summary>
    public static Task<T> ExecuteScalar<T>(this NpgsqlConnection connection, string sql, CancellationToken cancellationToken = default)
    {
        return connection.ExecuteScalar<T>(sql, Array.Empty<NpgsqlParameter>(), cancellationToken);
    }

    /// <summary>
    /// Executes a query and returns a scalar value of type T.
    /// It throws if the result set does not have exactly one column and one row.
    /// It throws if the return value is 'null' and the type T is a value type.
    /// </summary>
    public static Task<T> ExecuteScalar<T>(this NpgsqlConnection connection, string sql, params NpgsqlParameter[] parameters)
    {
        return connection.ExecuteScalar<T>(sql, parameters, CancellationToken.None);
    }

    /// <summary>
    /// Executes a query and returns a list of all rows read into objects of type `T`.
    /// </summary>
    public static async Task<List<T>> Query<T>(this NpgsqlConnection connection,
        string sql,
        IEnumerable<NpgsqlParameter> parameters,
        CancellationToken cancellationToken = default) where T : new()
    {
        if (connection == null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        if (sql == null)
        {
            throw new ArgumentNullException(nameof(sql));
        }

        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        await EnsureOpenState(connection, cancellationToken);

        var result = new List<T>();

        await using (var command = CreateCommand(connection, sql, parameters))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            var setters = GetSetters<T>();

            var settersByColumnOrder = new Action<T, object>[reader.FieldCount];

            for (int i = 0; i < reader.FieldCount; i++)
            {
                settersByColumnOrder[i] = setters[reader.GetName(i)];
            }

            while (await reader.ReadAsync(cancellationToken))
            {
                var instance = new T();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var setter = settersByColumnOrder[i];

                    object value = reader.GetValue(i);

                    if (value is DBNull)
                    {
                        setter(instance, null);
                    }
                    else
                    {
                        setter(instance, value);
                    }
                }

                result.Add(instance);
            }
        }

        return result;
    }

    /// <summary>
    /// Executes a query and returns a list of all rows read into objects of type `T`.
    /// </summary>
    public static Task<List<T>> Query<T>(this NpgsqlConnection connection, string sql, CancellationToken cancellationToken = default) where T : new()
    {
        return connection.Query<T>(sql, Array.Empty<NpgsqlParameter>(), cancellationToken);
    }

    /// <summary>
    /// Executes a query and returns a list of all rows read into objects of type `T`.
    /// </summary>
    public static Task<List<T>> Query<T>(this NpgsqlConnection connection, string sql, params NpgsqlParameter[] parameters) where T : new()
    {
        return connection.Query<T>(sql, parameters, CancellationToken.None);
    }

    /// <summary>
    /// Executes a query and returns a single row read into an object of type `T`.
    /// </summary>
    public static async Task<T> QueryOne<T>(this NpgsqlConnection connection,
        string sql,
        IEnumerable<NpgsqlParameter> parameters,
        CancellationToken cancellationToken = default) where T : class, new()
    {
        if (connection == null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        if (sql == null)
        {
            throw new ArgumentNullException(nameof(sql));
        }

        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        await EnsureOpenState(connection, cancellationToken);

        await using (var command = CreateCommand(connection, sql, parameters))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            var instance = new T();

            var setters = GetSetters<T>();

            bool hasRow = await reader.ReadAsync(cancellationToken);

            if (!hasRow)
            {
                return null;
            }

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var setter = setters[reader.GetName(i)];

                object value = reader.GetValue(i);

                if (value is DBNull)
                {
                    setter(instance, null);
                }
                else
                {
                    setter(instance, value);
                }
            }

            bool hasMoreRows = await reader.ReadAsync(cancellationToken);

            if (hasMoreRows)
            {
                throw new ApplicationException("More than one row returned for query that expected only one row.");
            }

            return instance;
        }
    }

    /// <summary>
    /// Executes a query and returns a single row read into an object of type `T`.
    /// </summary>
    public static Task<T> QueryOne<T>(this NpgsqlConnection connection, string sql, CancellationToken cancellationToken = default) where T : class, new()
    {
        return connection.QueryOne<T>(sql, Array.Empty<NpgsqlParameter>(), cancellationToken);
    }

    /// <summary>
    /// Executes a query and returns a single row read into an object of type `T`.
    /// </summary>
    public static Task<T> QueryOne<T>(this NpgsqlConnection connection, string sql, params NpgsqlParameter[] parameters) where T : class, new()
    {
        return connection.QueryOne<T>(sql, parameters, CancellationToken.None);
    }

    /// <summary>
    /// Creates a parameter of type T with NpgsqlDbType from the default type map 'defaultNpgsqlDbTypeMap'.
    /// </summary>
    public static NpgsqlParameter CreateParameter<T>(this NpgsqlConnection connection, string parameterName, T value)
    {
        NpgsqlDbType dbType;

        var type = typeof(T);

        if (DefaultNpgsqlDbTypeMap.ContainsKey(type))
        {
            dbType = DefaultNpgsqlDbTypeMap[type];
        }
        else
        {
            throw new ApplicationException(
                "Parameter type is not mapped to any \'NpgsqlDbType\'. Please specify a \'NpgsqlDbType\' explicitly.");
        }

        return connection.CreateParameter(parameterName, value, dbType);
    }

    /// <summary>
    /// Creates a parameter of type T by explicitly specifying NpgsqlDbType.
    /// </summary>
    public static NpgsqlParameter CreateParameter<T>(this NpgsqlConnection connection, string parameterName, T value, NpgsqlDbType dbType)
    {
        if (value == null)
        {
            return new NpgsqlParameter(parameterName, DBNull.Value);
        }

        return new NpgsqlParameter<T>(parameterName, dbType)
        {
            TypedValue = value,
        };
    }

    /// <summary>
    /// Creates a generic parameter.
    /// </summary>
    public static NpgsqlParameter CreateParameter(this NpgsqlConnection connection, string parameterName, object value)
    {
        return new NpgsqlParameter(parameterName, value);
    }

    /// <summary>
    /// Opens the connection if it's not in an opened state.
    /// </summary>
    public static Task EnsureOpenState(this NpgsqlConnection connection, CancellationToken cancellationToken = default)
    {
        switch (connection.State)
        {
            case ConnectionState.Open:
            {
                break;
            }
            case ConnectionState.Closed:
            case ConnectionState.Broken:
            {
                return connection.OpenAsync(cancellationToken);
            }
            default:
            {
                throw new DetailedException($"Unexpected connection state: {connection.State.ToString()}. Possibly a race condition.");
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a command.
    /// </summary>
    private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, string sql, IEnumerable<NpgsqlParameter> parameters)
    {
        var command = connection.CreateCommand();

        command.CommandText = sql;

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        return command;
    }

    /// <summary>
    /// Returns a dictionary where the key is a name of a database column and the
    /// value is a 'setter' - a delegate instance that takes one object instance and a value
    /// and assigns the value to a mapped property of the object instance.
    /// </summary>
    public static Dictionary<string, Action<T, object>> GetSetters<T>()
    {
        static Dictionary<string, Action<T, object>> ValueFactory(Type type)
        {
            var result = new Dictionary<string, Action<T, object>>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.SetMethod != null))
            {
                var builder = new DynamicMethod($"{property.Name}_setter", typeof(void), new[] { typeof(T), typeof(object) });
                var il = builder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                if (property.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, property.PropertyType);
                }

                il.Emit(OpCodes.Call, property!.SetMethod!);
                il.Emit(OpCodes.Ret);

                var setter = builder.CreateDelegate<Action<T, object>>();

                result.TryAdd(property.Name, setter);
                result.TryAdd(property.Name.Replace("_", ""), setter);
                result.TryAdd(ConvertToSnakeCase(property.Name), setter);
            }

            return result;
        }

        return (Dictionary<string, Action<T, object>>)SettersCache.GetOrAdd(typeof(T), ValueFactory);
    }

    /// <summary>
    /// Returns a dictionary where the key is a name of a database column and the
    /// value is a 'getter' - a delegate instance that takes one object instance and
    /// returns the value of a mapped property.
    /// </summary>
    public static Dictionary<string, Func<T, object>> GetGetters<T>()
    {
        static Dictionary<string, Func<T, object>> ValueFactory(Type type)
        {
            var result = new Dictionary<string, Func<T, object>>();

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.GetMethod != null))
            {
                var builder = new DynamicMethod($"{property.Name}_getter", typeof(object), new[] { typeof(T) });
                var il = builder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, property!.GetMethod!);

                if (property.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Box, property.PropertyType);
                }

                il.Emit(OpCodes.Ret);

                var getter = builder.CreateDelegate<Func<T, object>>();

                result.TryAdd(property.Name, getter);
                result.TryAdd(property.Name.Replace("_", ""), getter);
                result.TryAdd(ConvertToSnakeCase(property.Name), getter);
            }

            return result;
        }

        return (Dictionary<string, Func<T, object>>)GettersCache.GetOrAdd(typeof(T), ValueFactory);
    }

    /// <summary>
    /// Converts `PascalCase` property names into `snake_case` column names.
    /// The conversion happens on Uppercase letter or the string `ID`.
    /// Examples:
    /// SystemSettingName => system_setting_name
    /// SystemSettingID => system_setting_id
    /// System_Setting_ID => system_setting_id
    /// system_setting_id => system_setting_id
    /// FK_Reference_Schema_Name => fk_reference_schema_name
    /// </summary>
    private static string ConvertToSnakeCase(string propertyName)
    {
        var sb = new StringBuilder();

        sb.Append(char.ToLower(propertyName[0]));

        for (int i = 1; i < propertyName.Length; i++)
        {
            bool NextIs(Func<char, bool> func)
            {
                if (i + 1 >= propertyName.Length)
                {
                    return false;
                }

                char nextChar = propertyName[i + 1];

                return func(nextChar);
            }

            bool PrevIs(Func<char, bool> func)
            {
                if (i - 1 < 0)
                {
                    return false;
                }

                char prevChar = propertyName[i - 1];

                return func(prevChar);
            }

            char c = propertyName[i];

            if (c == 'I' && NextIs(x => x == 'D'))
            {
                sb.Append(PrevIs(x => x == '_') ? "id" : "_id");
                i++;
            }
            else if (c == '_')
            {
                sb.Append('_');
            }
            else if (char.IsUpper(c))
            {
                if (!PrevIs(char.IsUpper) && !PrevIs(x => x == '_'))
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLower(c));
            }

            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
