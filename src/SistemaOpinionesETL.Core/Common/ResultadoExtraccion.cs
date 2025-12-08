
using SistemaOpinionesETL.Core.Entities;
namespace SistemaOpinionesETL.Core.Common;

public class ResultadoExtraccion
{
    public string NombreFuente { get; set; } = string.Empty;
    public bool Exitoso { get; set; }
    public IEnumerable<Comentario> Comentarios { get; set; } = new List<Comentario>();
    public int RegistrosExtraidos => Comentarios?.Count() ?? 0;
    public TimeSpan TiempoExtraccion { get; set; }
    public string MensajeError { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}

public class ResumenExtraccion
{
    public int TotalFuentes { get; set; }
    public int FuentesExitosas { get; set; }
    public int TotalRegistrosExtraidos { get; set; }
    public TimeSpan TiempoTotal { get; set; }
    public List<ResultadoExtraccion> Resultados { get; set; } = new();
}