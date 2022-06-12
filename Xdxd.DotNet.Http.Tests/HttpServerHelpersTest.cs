// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global

namespace Xdxd.DotNet.Http.Tests;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Shared;
using Testing;
using Xunit;

public class WriteUtf8Works
{
    [Theory]
    [InlineData("4000b_lipsum.txt")]
    [InlineData("unicode_test_page.html")]
    public async Task WriteUtf8_works(string resourceName)
    {
        string resourceText = await TestHelper.GetResourceText(resourceName);

        async Task Handler(HttpContext context)
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteUtf8(resourceText);
        }

        await using (var tester = await HttpServerTester.Create(Handler))
        {
            var response = await tester.Client.GetAsync("/");

            tester.EnsureHandlerSuccess();
            response.EnsureSuccessStatusCode();

            var responseBodyBytes = await response.Content.ReadAsByteArrayAsync();

            //  Check the sting for equality.
            string responseBodyString = EncodingHelper.UTF8.GetString(responseBodyBytes);
            Assert.Equal(resourceText, responseBodyString);

            // Check the bytes for equality.
            var expectedBytes = EncodingHelper.UTF8.GetBytes(resourceText);
            AssertExt.SequentialEqual(expectedBytes, responseBodyBytes);
        }
    }
}

public class ReadUtf8WorksForValidUtf8
{
    [Theory]
    [InlineData("4000b_lipsum.txt")]
    [InlineData("unicode_test_page.html")]
    public async Task ReadUtf8_works_for_valid_utf8(string resourceName)
    {
        for (int i = 0; i < 1000; i++)
        {
            var resourceBytes = await TestHelper.GetResourceBytes(resourceName);
            string resourceString = EncodingHelper.UTF8.GetString(resourceBytes);

            string str = null;

            async Task Handler(HttpContext context)
            {
                str = await context.Request.ReadUtf8();
                context.Response.StatusCode = 200;
            }

            await using (var tester = await HttpServerTester.Create(Handler))
            {
                var response = await tester.Client.PostAsync("/", new ReadOnlyMemoryContent(resourceBytes));
                tester.EnsureHandlerSuccess();
                response.EnsureSuccessStatusCode();

                Assert.Equal(resourceString, str);
            }
        }
    }
}

public class ReadToEndWorks
{
    [Theory]
    [InlineData("4000b_lipsum.txt")]
    [InlineData("unicode_test_page.html")]
    public async Task ReadToEnd_works(string resourceName)
    {
        var resourceBytes = await TestHelper.GetResourceBytes(resourceName);

        async Task Handler(HttpContext context)
        {
            using var dataHandle = await context.Request.ReadToEnd();
            context.Response.StatusCode = 200;
            await context.Response.Body.WriteAsync(dataHandle.Memory);
        }

        await using (var tester = await HttpServerTester.Create(Handler))
        {
            var response = await tester.Client.PostAsync("/", new ReadOnlyMemoryContent(resourceBytes));
            tester.EnsureHandlerSuccess();
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsByteArrayAsync();

            AssertExt.SequentialEqual(resourceBytes, responseBody);
        }
    }
}

public class ReadToEndThrowsOnNetworkError
{
    [Fact]
    public async Task ReadToEnd_throws_on_network_error()
    {
        static async Task Handler(HttpContext context)
        {
            using var dataHandle = await context.Request.ReadToEnd();
            context.Response.StatusCode = 200;
        }

        await using (var tester = await HttpServerTester.Create(Handler))
        {
            string text = string.Join("", Enumerable.Range(0, 10000).Select(_ => "X"));
            string requestJson = "{\"type\": \"PingRequest\", \"payload\":{ \"data\": \"" + text + "\" } }";
            var requestBody = EncodingHelper.UTF8.GetBytes(requestJson);

            var uri = new Uri(tester.Server.BoundAddresses.First());

            var requestHeaderBuilder = new StringBuilder();
            requestHeaderBuilder.Append("POST / HTTP/1.1\r\n");
            requestHeaderBuilder.Append($"Host: {uri.Host}\r\n");
            requestHeaderBuilder.Append("Content-Length: " + requestBody.Length + "\r\n");
            requestHeaderBuilder.Append("Connection: close\r\n");
            requestHeaderBuilder.Append("\r\n");

            var ipEndPoint = new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port);
            var reqHeader = Encoding.ASCII.GetBytes(requestHeaderBuilder.ToString());

            for (int i = 0; i < 1000; i++)
            {
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
                {
                    LingerState = new LingerOption(true, 0),
                    NoDelay = true,
                };

                socket.Connect(ipEndPoint);
                socket.Send(reqHeader, SocketFlags.None);
                socket.Send(requestBody, 0, requestBody.Length / 10, SocketFlags.None);
                socket.Dispose();
                // ReSharper disable once RedundantAssignment
                socket = null;
                GC.Collect(0, GCCollectionMode.Default, false);
            }

            Snapshot.MatchTopLevelError(tester.Exception);
        }
    }
}

public class ReadUtf8ThrowsOnInvalidUtf8
{
    [Fact]
    public async Task ReadUtf8_throws_on_invalid_utf8()
    {
        var invalidUtf8 = await TestHelper.GetResourceBytes("app-vnd.flatpak-icon.png");

        Exception exception = null;

        async Task Handler(HttpContext context)
        {
            try
            {
                await context.Request.ReadUtf8();
            }
            catch (Exception err)
            {
                exception = err;
            }

            context.Response.StatusCode = 200;
        }

        await using (var tester = await HttpServerTester.Create(Handler))
        {
            var response = await tester.Client.PostAsync("/", new ReadOnlyMemoryContent(invalidUtf8));
            tester.EnsureHandlerSuccess();
            response.EnsureSuccessStatusCode();

            Snapshot.MatchError(exception);
        }
    }
}

public class WriteUtf8ThrowsOnInvalidInput
{
    [Fact]
    public async Task WriteUtf8_throws_on_invalid_input()
    {
        Exception exception = null;

        async Task Handler(HttpContext context)
        {
            context.Response.StatusCode = 200;

            try
            {
                await context.Response.WriteUtf8("X\uD800Y");
            }
            catch (Exception err)
            {
                exception = err;
            }
        }

        await using (var tester = await HttpServerTester.Create(Handler))
        {
            _ = await tester.Client.GetAsync("/");

            Snapshot.MatchError(exception);
        }
    }
}

public class ReadUnknownSizeStreamWorksWithDifferentSizes
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1023)]
    [InlineData(1024)]
    [InlineData(1025)]
    [InlineData(2047)]
    [InlineData(2048)]
    [InlineData(2049)]
    [InlineData(1024 * 100)]
    public async Task ReadUnknownSizeStream_works_with_different_sizes(int numberOfBytes)
    {
        static byte[] GetRandomBytes(int length)
        {
            var random = new Random(123);
            var array = new byte[length];

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = (byte)random.Next(0, byte.MaxValue + 1);
            }

            return array;
        }

        var data = GetRandomBytes(numberOfBytes);

        Stream memoryStream = new MemoryStream(data);

        var read = await memoryStream.ReadUnknownSizeStream();

        var actualSequence = read.Memory.ToArray();

        AssertExt.SequentialEqual(data, actualSequence);
    }
}
