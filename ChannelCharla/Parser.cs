using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ChannelCharla
{
    public class Parser : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Parser> _log;
        public Parser(IConfiguration configuration, ILogger<Parser> logger)
        {
            _configuration = configuration;
            _log = logger;            
        }

        protected Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _log.LogInformation("Start the process");
                var sw = new Stopwatch();
                sw.Start();
                
                sw.Stop();
                _log.LogInformation("Tiempo transcurrido {tiempo}", sw.Elapsed);
                _log.LogInformation("End the process");
            }
            catch (Exception ex)
            {
                _log.LogError("Error exception {ex}", ex);
            }

            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return ExecuteAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
