
namespace SistemaOpinionesETL.Core.Entities.ModeloEstrella;

public class DimClient
{
   
    public int client_key { get; set; }
    public string client_id { get; set; } = string.Empty;
    public string client_name { get; set; } = string.Empty;

    
    public string country { get; set; } = string.Empty;
    public int? age { get; set; }
    public string gender { get; set; } = string.Empty;
    public string client_type { get; set; } = string.Empty;

    
    public DateTime registration_date { get; set; }
    public string client_segment { get; set; } = string.Empty; // Corregido: client_exponent -> client_segment
    public DateTime created_date { get; set; } = DateTime.UtcNow;

    
    public bool is_active { get; set; } = true;
}