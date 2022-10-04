namespace ChannelCharla.Model
{
    public class Evento
    {
        public int Indice { get; set; }
        public string? Nombre { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Finalizacion { get; set; }
        public string? Provincia { get; set; }
        public string? Municipio { get; set; }
        public string? Direccion { get; set; }
    }
}
