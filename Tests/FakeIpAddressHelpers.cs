using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Tests;

public class CustomStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.UseMiddleware<FakeRemoteIpAddressMiddleware>();
            next(app);
        };
}

public class FakeRemoteIpAddressMiddleware
{
    private readonly RequestDelegate next;
    private static IPAddress? fakeIpAddress = IPAddress.Parse("127.0.0.1");

    public static void SetIpAddress(string? ipAddress) =>
        fakeIpAddress = ipAddress == null ?
        null :
        IPAddress.Parse(ipAddress);

    public FakeRemoteIpAddressMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        httpContext.Connection.RemoteIpAddress = fakeIpAddress;

        await this.next(httpContext);
    }
}
