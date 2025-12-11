
using SistemaOpinionesETL.Core.Common;
namespace SistemaOpinionesETL.Core.Entities;

public class Comentario
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProductoId { get; set; } = string.Empty;
    public string? ClienteId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string Texto { get; set; } = string.Empty;
    public decimal? Calificacion { get; set; }
    public string Fuente { get; set; } = string.Empty;
    public TipoFuente TipoFuente { get; set; }
    public Sentimiento Sentimiento { get; set; } = Sentimiento.Neutral;
    public double PuntajePolaridad { get; set; }
    public DateTime FechaImportacion { get; set; } = DateTime.UtcNow;
}