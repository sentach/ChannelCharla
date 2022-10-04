using ChannelCharla.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Channels;
using System.Xml.Linq;

namespace ChannelCharla
{
    public class Parser : IHostedService
    {
        private const int bufferSize = 1024;
        private const string rowend = "</row>";
        private const string rowstart = "<row ";
        private readonly IConfiguration _configuration;
        private readonly ILogger<Parser> _log;
        private readonly Channel<string> _channelString;
        private readonly Channel<Evento> _eventChannel;


        public Parser(IConfiguration configuration, ILogger<Parser> logger)
        {
            _configuration = configuration;
            _log = logger;
            _channelString = Channel.CreateUnbounded<string>();
            _eventChannel = Channel.CreateUnbounded<Evento>();
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
                var numero = _configuration.GetValue<int>("numLectoresBBDD");                               
                var readerStringTask = Task.Run(ProccesChannelString);
                var readEventTask = new Task[numero];
                for(int i = 0; i < numero; i++)
                {
                    readEventTask[i] = Task.Run(ProcessChannelEvents);
                }

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
                        await _channelString.Writer.WriteAsync(row[..fin]);
                        row = row[fin..];
                        fin = row.IndexOf(rowend);
                    }

                }
                _channelString.Writer.Complete();
                readerStringTask.Wait();
                _eventChannel.Writer.Complete();
                Task.WaitAll(readEventTask);
            }
            catch (Exception ex)
            {
                _log.LogError("Excepcion {ex}", ex);
            }
        }
                
        private async Task ProccesChannelString()
        {
            _log.LogInformation("Comienzo del reader elementos");            
            await foreach (var item in _channelString.Reader.ReadAllAsync())
            {
                var xml = XDocument.Parse(item);

                var evento = new Evento
                {
                    Indice = Convert.ToInt32(xml.Root.Attribute("num")?.Value ?? "0"),
                    Nombre = xml.Root.Element("documentname")?.Value ?? string.Empty,
                    Inicio = Convert.ToDateTime(xml.Root.Element("eventstartdate")?.Value),
                    Finalizacion = Convert.ToDateTime(xml.Root.Element("eventenddate").Value),
                    Provincia = xml.Root.Element("territory")?.Value ?? "",
                    Municipio = xml.Root.Element("municipality")?.Value ?? "",
                    Direccion = xml.Root.Element("address")?.Value ?? ""
                };
                await _eventChannel.Writer.WriteAsync(evento);
            }
            _log.LogInformation("Fin del reader elementos");
        }

        private async Task ProcessChannelEvents()
        {
            _log.LogInformation("Comienzo reader channel BBDD");

            using var conn = new SqlConnection(_configuration.GetConnectionString("channel"));
            conn.Open();
            await foreach(var evento in _eventChannel.Reader.ReadAllAsync())
            {
                var sql = "INSERT INTO [dbo].[Eventos]([Indice],[Nombre],[Inicio],[Finalizacion],[Provincia],[Municipio],[Direccion]) " +
                    $"VALUES({evento.Indice},'{evento.Nombre}','{evento.Inicio:yyyy-MM-dd}','{evento.Finalizacion:yyyy-MM-dd}','{evento.Provincia}','{evento.Municipio}','{evento.Direccion}')";
                using var cmd = new SqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }

            _log.LogInformation("Fin reader channel BBDD");
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
