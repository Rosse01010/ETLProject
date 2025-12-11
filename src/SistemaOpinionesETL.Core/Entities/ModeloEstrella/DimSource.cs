
namespace SistemaOpinionesETL.Core.Entities.ModeloEstrella;


public class DimSource
{
    
    public int source_key { get; set; }
    public string source_id { get; set; } = string.Empty;
    public string source_name { get; set; } = string.Empty;

    
    public string source_type { get; set; } = string.Empty;
    public string source_category { get; set; } = string.Empty;
    public decimal reliability_score { get; set; } = 1.0m;

   
    public bool is_active { get; set; } = true; // Corregido: it_sache -> is_active

    
    public DateTime created_date { get; set; } = DateTime.UtcNow;
    public DateTime? updated_date { get; set; }
}