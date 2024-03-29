﻿namespace Xdxd.DotNet.Postgres.Tests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NpgsqlTypes;

public class GeneratedData<T> : IEnumerable<object[]>
    where T : class, IReadOnlyPoco<T>, new()
{
    public IEnumerator<object[]> GetEnumerator()
    {
        return PocoDataGenerator.GenerateData<T>()
            .Select(x => new object[] { x })
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}

public class GeneratedBulkData<T> : IEnumerable<object[]>
    where T : class, IReadOnlyPoco<T>, new()
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[]
        {
            PocoDataGenerator.GenerateData<T>(),
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}

public class PocoDataGenerator
{
    private const int RANDOM_SEED = 938274923;

    public static List<T> GenerateData<T>()
        where T : class, IReadOnlyPoco<T>, new()
    {
        var list = new List<T>();

        var metadata = DbMetadataHelpers.GetMetadata<T>();

        var setters = NpgsqlConnectionExtensions.GetSetters<T>();

        var valuesArray = metadata.Columns.Select(x => GetValuesByType(x.PropertyType.NpgsqlDbType)).ToArray();

        for (var i = 0; i < metadata.Columns.Count; i++)
        {
            var column = metadata.Columns[i];

            if (!column.IsPrimaryKey)
            {
                var values = valuesArray[i];

                if (column.IsNullable)
                {
                    values = values.Concat(new object[] { null }).ToArray();
                }

                foreach (object value in values)
                {
                    var instance = new T();

                    setters[column.ColumnName](instance, value);

                    foreach (var otherColumn in metadata.Columns.Where(x => x != column && !x.IsPrimaryKey))
                    {
                        var newValue = GetValuesByType(otherColumn.PropertyType.NpgsqlDbType).First();
                        setters[otherColumn.ColumnName](instance, newValue);
                    }

                    list.Add(instance);
                }
            }
        }

        return list;
    }

    private static object[] GetValuesByType(NpgsqlDbType dbType)
    {
        return dbType switch
        {
            NpgsqlDbType.Bigint => GenerateLongArray().Cast<object>().ToArray(),
            NpgsqlDbType.Double => GenerateDoubleArray().Cast<object>().ToArray(),
            NpgsqlDbType.Integer => GenerateIntArray().Cast<object>().ToArray(),
            NpgsqlDbType.Numeric => GenerateDecimalArray().Cast<object>().ToArray(),
            NpgsqlDbType.Real => GenerateFloatArray().Cast<object>().ToArray(),
            NpgsqlDbType.Smallint => GenerateShortArray().Cast<object>().ToArray(),
            NpgsqlDbType.Boolean => GenerateBooleanArray().Cast<object>().ToArray(),
            NpgsqlDbType.Char => GenerateCharArray().Cast<object>().ToArray(),
            NpgsqlDbType.Text => GenerateStringArray().Cast<object>().ToArray(),
            NpgsqlDbType.Varchar => GenerateStringArray().Cast<object>().ToArray(),
            NpgsqlDbType.Bytea => GenerateByteArray().Cast<object>().ToArray(),
            NpgsqlDbType.Date => GenerateDateArray().Cast<object>().ToArray(),
            NpgsqlDbType.Timestamp => GenerateDateTimeArray().Cast<object>().ToArray(),
            NpgsqlDbType.Uuid => GenerateGuidArray().Cast<object>().ToArray(),
            NpgsqlDbType.Xml => GenerateXmlArray().Cast<object>().ToArray(),
            NpgsqlDbType.Json => GenerateJsonArray().Cast<object>().ToArray(),
            NpgsqlDbType.Jsonb => GenerateJsonArray().Cast<object>().ToArray(),
            NpgsqlDbType.TimestampTz => GenerateDateTimeArray().Cast<object>().ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(dbType), dbType, null),
        };
    }

    private static bool[] GenerateBooleanArray()
    {
        return new[] { true, false };
    }

    private static byte[][] GenerateByteArray()
    {
        var random = new Random(RANDOM_SEED);

        byte[][] all = new byte[1][];

        for (int i = 0; i < all.Length; i++)
        {
            byte[] buffer = all[i];

            all[i] = new byte[10];

            random.NextBytes(buffer);
        }

        return all;
    }

    private static string[] GenerateCharArray()
    {
        return new[] { "a", "b", "c" };
    }

    private static DateTime[] GenerateDateArray()
    {
        var random = new Random(RANDOM_SEED);

        return Enumerable.Range(0, 2)
            .Select(_ => new DateTime(random.Next(2000, 2100), random.Next(1, 13), random.Next(1, 27), 0, 0, 0, DateTimeKind.Utc))
            .ToArray();
    }

    private static DateTime[] GenerateDateTimeArray()
    {
        var random = new Random(RANDOM_SEED);

        return Enumerable.Range(0, 2)
            .Select(_ => new DateTime(random.Next(2000, 2100), random.Next(1, 13), random.Next(1, 27), random.Next(1, 24),
                random.Next(1, 60), random.Next(1, 60), DateTimeKind.Utc))
            .ToArray();
    }

    private static decimal[] GenerateDecimalArray()
    {
        return new[] { 0m, 1.234m, -1.234m };
    }

    private static double[] GenerateDoubleArray()
    {
        return new[] { 0d, 1.234d, -1.234d };
    }

    private static float[] GenerateFloatArray()
    {
        return new[] { 0f, 1.234f, -1.234f };
    }

    private static string[] GenerateGuidArray()
    {
        return new[] { "173e5661-e425-431a-a3b0-03e7d65b95aa" };
    }

    private static int[] GenerateIntArray()
    {
        return new[] { -1, 0, 1 };
    }

    private static string[] GenerateJsonArray()
    {
        var random = new Random(RANDOM_SEED);

        return new[]
        {
            "{\"a\": \"|\", \"b\": |}".Replace("|", random.Next(0, 100).ToString()),
            "{}",
        };
    }

    private static long[] GenerateLongArray()
    {
        return new[] { -1L, 0L, 1L };
    }

    private static short[] GenerateShortArray()
    {
        return new short[] { -1, 0, 1 };
    }

    private static string[] GenerateStringArray()
    {
        var random = new Random(RANDOM_SEED);

        string template = "abcdefghijklmnopqrstuvwxyz";
        template += template.ToUpper();

        return Enumerable.Range(0, 2)
            .Select(_ => new string(Enumerable.Range(0, 50)
                .Select(_ => template[random.Next(0, template.Length)])
                .ToArray()))
            .ToArray();
    }

    private static string[] GenerateXmlArray()
    {
        var random = new Random(RANDOM_SEED);

        return new[]
        {
            "<tag>|</tag>".Replace("|", random.Next(0, 100).ToString()),
        };
    }
}
