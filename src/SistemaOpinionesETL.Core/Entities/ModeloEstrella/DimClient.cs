
namespace SistemaOpinionesETL.Core.Entities.ModeloEstrella;

/// <summary>
/// Dimensión de Cliente
/// Describe a los clientes o usuarios que generan las opiniones
/// </summary>
public class DimClient
{
    // Campos clave
    public int client_key { get; set; }
    public string client_id { get; set; } = string.Empty;
    public string client_name { get; set; } = string.Empty;

    // Atributos demográficos
    public string country { get; set; } = string.Empty;
    public int? age { get; set; }
    public string gender { get; set; } = string.Empty;
    public string client_type { get; set; } = string.Empty;

    // Otros datos
    public DateTime registration_date { get; set; }
    public string client_segment { get; set; } = string.Empty; // Corregido: client_exponent -> client_segment
    public DateTime created_date { get; set; } = DateTime.UtcNow;

    // Control
    public bool is_active { get; set; } = true;
}