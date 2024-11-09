using Activite.Services.Gateway.Builders;
using Convey;
using Convey.Discovery.Consul;
using Convey.HTTP;
using Convey.Logging;
using Convey.WebApi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;

#if DEBUG

DotNetEnv.Env.Load();

#endif

var host = WebHost.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config
            .AddJsonFile("ocelot.json", true, true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices(services =>
    {
        services
            .AddOcelot()
            .AddConsul<ConsulServiceBuilder>();

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
                .Get("/ping", ctx => ctx.Response.WriteAsync("pong")))
            .UseOcelot().Wait();
    })
    .UseLogging()
    .Build();

await host.RunAsync();