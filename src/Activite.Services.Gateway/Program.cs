using Activite.Services.Gateway.Builders;
using Activite.Services.Gateway.Middlewares;
using Activite.Services.Gateway.Options;
using Activite.Services.Gateway.Services;
using Convey;
using Convey.Discovery.Consul;
using Convey.HTTP;
using Convey.Logging;
using Convey.WebApi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    .ConfigureServices((context, services) =>
    {
        services.Configure<GoogleTokenOptions>(context.Configuration.GetSection(GoogleTokenOptions.SectionName));

        services
            .AddOcelot()
            .AddConsul<ConsulServiceBuilder>();

        services
            .AddTransient<IntegrationService>()
            .AddTransient<UserService>()
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
            .UseMiddleware<TokenMiddleware>()
            .UseMiddleware<AuthorityMiddleware>()
            .UseOcelot().Wait();
    })
    .UseLogging()
    .Build();

await host.RunAsync();