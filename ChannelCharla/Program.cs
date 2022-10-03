using ChannelCharla;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Parser>();
    })
    .Build();

await host.StartAsync();

Console.ReadKey();
