
using MaxMind.GeoIP2;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace WebApi;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var databasePath = builder.Configuration.GetSection("DatabasePath").Value ??
            throw new Exception("No database set!");
        builder.Services.AddSingleton<IGeoIP2DatabaseReader>(new DatabaseReader(databasePath));

        builder.Services.AddControllers();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "Geolocation WebApi v1");
        });

        app.UseHttpsRedirection();

        app.MapControllers();

        app.Run();
    }
}
