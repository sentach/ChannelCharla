using ChannelCharla.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Net;
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
                using var conn = new SqlConnection(_configuration.GetConnectionString("channel"));
                conn.Open();
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
                        await ProcessInfoAsync(row[..fin], conn);
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

        private async Task ProcessInfoAsync(string item, SqlConnection conn)
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
            var url = xml.Root.Element("dataxml")?.Value;
            if(!string.IsNullOrEmpty(url))
            {
                var httpClient = new HttpClient();
                var result = await httpClient.GetAsync(url);
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    var xmlDesc = XDocument.Load(await result.Content.ReadAsStreamAsync());
                    evento.Descripcion = xmlDesc.Root?.Element("eventDescription")?.Value.Replace("'", "''") ?? "";
                }
                else { evento.Descripcion = ""; }
            }

            var sql = "INSERT INTO [dbo].[Eventos]([Indice],[Nombre],[Inicio],[Finalizacion],[Provincia],[Municipio],[Direccion],[Descripcion]) " +
                    $"VALUES({evento.Indice},'{evento.Nombre}','{evento.Inicio:yyyy-MM-dd}','{evento.Finalizacion:yyyy-MM-dd}','{evento.Provincia}','{evento.Municipio}','{evento.Direccion}', '{evento.Descripcion}')";
            using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
            _log.LogInformation("Insertado elemento {indice}", evento.Indice);
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
