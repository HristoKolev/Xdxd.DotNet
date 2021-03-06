namespace Xdxd.DotNet.Http;

using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

/// <summary>
/// A testing service that facilitates testing of <see cref="RequestDelegate" />'s.
/// Creates <see cref="CustomHttpServerImpl" /> with given <see cref="RequestDelegate" /> and starts it on a random port.
/// Creates an <see cref="HttpClient" /> with <see cref="HttpClient.BaseAddress" /> that points to the
/// <see cref="CustomHttpServerImpl" />.
/// Disposes both <see cref="CustomHttpServerImpl" /> and <see cref="HttpClient" /> when <see cref="DisposeAsync" /> is
/// called.
/// </summary>
public class HttpServerTester : IAsyncDisposable
{
    private HttpServerTester() { }

    /// <summary>
    /// This gets populated with an <see cref="Exception" /> object
    /// if the given <see cref="RequestDelegate" /> throws.
    /// </summary>
    public Exception Exception { get; set; }

    public CustomHttpServerImpl Server { get; set; }

    public HttpClient Client { get; set; }

    public async ValueTask DisposeAsync()
    {
        await this.Server.DisposeAsync();
        this.Client.Dispose();
    }

    /// <summary>
    /// Creates and starts a new instance of <see cref="HttpServerTester" />
    /// </summary>
    public static async Task<HttpServerTester> Create(RequestDelegate requestDelegate)
    {
        var tester = new HttpServerTester();

        async Task Handler(HttpContext context)
        {
            try
            {
                await requestDelegate(context);
            }
            catch (Exception err)
            {
                tester.Exception = err;
                throw;
            }
        }

        var server = new CustomHttpServerImpl();

        await server.Start(Handler, new[] { "http://127.0.0.1:0" });

        var client = new HttpClient
        {
            BaseAddress = new Uri(server.BoundAddresses.First()),
        };

        tester.Server = server;
        tester.Client = client;

        return tester;
    }

    public void EnsureHandlerSuccess()
    {
        if (this.Exception != null)
        {
            ExceptionDispatchInfo.Capture(this.Exception).Throw();
        }
    }
}
