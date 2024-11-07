using Convey;
using Convey.Discovery.Consul;
using Convey.HTTP;
using Convey.Logging;
using Convey.WebApi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

#if DEBUG

DotNetEnv.Env.Load();

#endif

var host = WebHost.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config
            // .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile("ocelot.json", true, true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices(services =>
    {
        services
            .AddConvey()
            .AddWebApi()
            .AddHttpClient()
            .AddConsul()
            .Build();
    })
    .Configure(app =>
    {
        app
            .UseConvey()
            .UseEndpoints(endpoints => endpoints
                .Get("/ping", ctx => ctx.Response.WriteAsync("pong")));
    })
    .UseLogging()
    .Build();

await host.RunAsync();