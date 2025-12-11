
namespace SistemaOpinionesETL.Core.Entities.ModeloEstrella;


public class DimSentiment
{
   
    public int sentiment_key { get; set; }
    public string sentiment_id { get; set; } = string.Empty;
    public string sentiment_name { get; set; } = string.Empty;
    public int sentiment_score { get; set; }

   
    public decimal score_range_min { get; set; }
    public decimal score_range_max { get; set; }
    public string color_code { get; set; } = string.Empty;

   
    public DateTime created_date { get; set; } = DateTime.UtcNow;
    public bool is_active { get; set; } = true;
}