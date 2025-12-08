
namespace SistemaOpinionesETL.Core.Entities.ModeloEstrella;

/// <summary>
/// Dimensión de Fuente
/// Describe el origen de los datos o comentarios
/// </summary>
public class DimSource
{
    // Campos clave
    public int source_key { get; set; }
    public string source_id { get; set; } = string.Empty;
    public string source_name { get; set; } = string.Empty;

    // Características
    public string source_type { get; set; } = string.Empty;
    public string source_category { get; set; } = string.Empty;
    public decimal reliability_score { get; set; } = 1.0m;

    // Campo de verificación
    public bool is_active { get; set; } = true; // Corregido: it_sache -> is_active

    // Metadata
    public DateTime created_date { get; set; } = DateTime.UtcNow;
    public DateTime? updated_date { get; set; }
}