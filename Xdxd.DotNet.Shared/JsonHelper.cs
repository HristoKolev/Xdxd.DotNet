namespace Xdxd.DotNet.Shared;

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Toolkit.HighPerformance.Buffers;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions SerializationOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize<T>(T value) where T : class
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var inputType = value.GetType();

        return JsonSerializer.Serialize(value, inputType, SerializationOptions);
    }

    public static Task Serialize<T>(Stream outputStream, T value) where T : class
    {
        if (outputStream == null)
        {
            throw new ArgumentNullException(nameof(outputStream));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var inputType = value.GetType();

        return JsonSerializer.SerializeAsync(outputStream, value, inputType, SerializationOptions);
    }

    public static Task SerializeGenericType<T>(Stream outputStream, T value)
    {
        if (outputStream == null)
        {
            throw new ArgumentNullException(nameof(outputStream));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return JsonSerializer.SerializeAsync(outputStream, value, SerializationOptions);
    }

    public static object Deserialize(string json, Type returnType)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        if (returnType == null)
        {
            throw new ArgumentNullException(nameof(returnType));
        }

        try
        {
            return JsonSerializer.Deserialize(json, returnType, SerializationOptions);
        }
        catch (Exception err)
        {
            long? bytePositionInLine = null;
            long? lineNumber = null;
            string jsonPath = null;

            if (err is JsonException jsonException)
            {
                bytePositionInLine = jsonException.BytePositionInLine;
                lineNumber = jsonException.LineNumber;
                jsonPath = jsonException.Path;
            }

            throw new DetailedException("Failed deserialize json.")
            {
                Details =
                {
                    { "bytePositionInLine", bytePositionInLine },
                    { "lineNumber", lineNumber },
                    { "jsonPath", jsonPath },
                    { "inputJson", json },
                    { "outputType", returnType.FullName },
                },
            };
        }
    }

    public static T Deserialize<T>(string json) where T : class
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, SerializationOptions);
        }
        catch (Exception err)
        {
            long? bytePositionInLine = null;
            long? lineNumber = null;
            string jsonPath = null;

            if (err is JsonException jsonException)
            {
                bytePositionInLine = jsonException.BytePositionInLine;
                lineNumber = jsonException.LineNumber;
                jsonPath = jsonException.Path;
            }

            throw new DetailedException("Failed deserialize json.")
            {
                Details =
                {
                    { "bytePositionInLine", bytePositionInLine },
                    { "lineNumber", lineNumber },
                    { "jsonPath", jsonPath },
                    { "inputJson", json },
                    { "outputType", typeof(T).FullName },
                },
            };
        }
    }

    public static object Deserialize(ReadOnlySpan<byte> utf8Bytes, Type returnType)
    {
        if (utf8Bytes == default)
        {
            throw new ArgumentException($"The {nameof(utf8Bytes)} parameter must not be empty.", nameof(utf8Bytes));
        }

        if (returnType == null)
        {
            throw new ArgumentNullException(nameof(returnType));
        }

        try
        {
            return JsonSerializer.Deserialize(utf8Bytes, returnType, SerializationOptions);
        }
        catch (Exception err)
        {
            long? bytePositionInLine = null;
            long? lineNumber = null;
            string jsonPath = null;

            if (err is JsonException jsonException)
            {
                bytePositionInLine = jsonException.BytePositionInLine;
                lineNumber = jsonException.LineNumber;
                jsonPath = jsonException.Path;
            }

            throw new DetailedException("Failed deserialize json.")
            {
                Details =
                {
                    { "bytePositionInLine", bytePositionInLine },
                    { "lineNumber", lineNumber },
                    { "jsonPath", jsonPath },
                    { "base64Data", Convert.ToBase64String(utf8Bytes) },
                    { "outputType", returnType.FullName },
                },
            };
        }
    }

    public static T Deserialize<T>(ReadOnlySpan<byte> utf8Bytes)
    {
        if (utf8Bytes == default)
        {
            throw new ArgumentException($"The {nameof(utf8Bytes)} parameter must not be empty.", nameof(utf8Bytes));
        }

        try
        {
            return JsonSerializer.Deserialize<T>(utf8Bytes, SerializationOptions);
        }
        catch (Exception err)
        {
            long? bytePositionInLine = null;
            long? lineNumber = null;
            string jsonPath = null;

            if (err is JsonException jsonException)
            {
                bytePositionInLine = jsonException.BytePositionInLine;
                lineNumber = jsonException.LineNumber;
                jsonPath = jsonException.Path;
            }

            throw new DetailedException("Failed deserialize json.")
            {
                Details =
                {
                    { "bytePositionInLine", bytePositionInLine },
                    { "lineNumber", lineNumber },
                    { "jsonPath", jsonPath },
                    { "base64Data", Convert.ToBase64String(utf8Bytes) },
                    { "outputType", typeof(T).FullName },
                },
            };
        }
    }

    /// <summary>
    /// Returns the size (in bytes) of the JSON representation of an object, without allocating the memory to hold it.
    /// </summary>
    public static int GetJsonSize<T>(T value) where T : class
    {
        if (value == null)
        {
            return "null".Length;
        }

        var inputType = value.GetType();

        using (var bufferWriter = new ArrayPoolBufferWriter<byte>())
        using (var utf8JsonWriter = new Utf8JsonWriter(bufferWriter))
        {
            JsonSerializer.Serialize(utf8JsonWriter, value, inputType, SerializationOptions);
            return bufferWriter.WrittenCount;
        }
    }
}
