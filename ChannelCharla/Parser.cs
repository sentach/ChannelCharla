using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ChannelCharla
{
    public class Parser : IHostedService
    {
        private const int bufferSize = 1024;
        private const string rowend = "</row>";
        private const string rowstart = "<row ";
        private readonly IConfiguration _configuration;
        private readonly ILogger<Parser> _log;
        int numero;

        public Parser(IConfiguration configuration, ILogger<Parser> logger)
        {
            _configuration = configuration;
            _log = logger;            
        }

        protected async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _log.LogInformation("Start the process");
                var sw = new Stopwatch();
                sw.Start();
                var file = _configuration.GetValue<string>("file");

                _log.LogInformation("El fichero es {file}", file);
                numero = 1;

                await Process(file);

                sw.Stop();
                _log.LogInformation("Tiempo transcurrido {tiempo}", sw.Elapsed);
                _log.LogInformation("End the process");
            }
            catch (Exception ex)
            {
                _log.LogError("Error exception {ex}", ex);
            }                        
        }

        private async Task Process(string file)
        {
            try
            {
                _log.LogInformation("El fichero es {file}", file);

                using var reader = new StreamReader(file);
                string row = string.Empty;
                while (!reader.EndOfStream)
                {
                    var buffer = new char[bufferSize];
                    int readed = await reader.ReadAsync(buffer, 0, bufferSize);

                    row += new string(buffer, 0, readed);

                    int inicio = row.IndexOf(rowstart);
                    if (inicio > 0)
                    {
                        row = row[inicio..];
                    }
                    int fin = row.IndexOf(rowend);
                    while (fin >= 0)
                    {
                        fin += rowend.Length;
                        ProcessInfo(row[..fin]);
                        row = row[fin..];
                        fin = row.IndexOf(rowend);
                    }

                }

            }
            catch (Exception ex)
            {
                _log.LogError("Excepcion {ex}", ex);
            }
        }

        private void ProcessInfo(string item)
        {
            _log.LogInformation("Comienzo del reader");

            var inicio = item.IndexOf('"');
            var fin = item.IndexOf('"', inicio + 1);
            var num = item.Substring(inicio + 1, fin - inicio - 1);
            _ = int.TryParse(num, out int result);
            _log.LogInformation("Numero encontrado {result} vamos por {numero}", result, numero++);

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
