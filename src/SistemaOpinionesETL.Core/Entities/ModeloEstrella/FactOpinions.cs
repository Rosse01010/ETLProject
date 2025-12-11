
namespace SistemaOpinionesETL.Core.Entities.ModeloEstrella;


public class FactOpinions
{
   
    public long opinion_key { get; set; }

   
    public int client_key { get; set; }
    public int product_key { get; set; }
    public int time_key { get; set; }
    public int source_key { get; set; }
    public int sentiment_key { get; set; }

    
    public decimal? rating { get; set; }
    public decimal sentiment_score { get; set; }
    public string comment_text { get; set; } = string.Empty;
    public int comment_length { get; set; }
    public int word_count { get; set; }

    
    public bool contains_keywords { get; set; }
    public int? response_time_hours { get; set; } 
    public string original_comment_id { get; set; } = string.Empty; 
    public string comment_type { get; set; } = string.Empty;
    public string channel_code { get; set; } = string.Empty;

    
    public DateTime created_date { get; set; } = DateTime.UtcNow;
    public DateTime? updated_date { get; set; }
    public string etl_batch_id { get; set; } = string.Empty;

    
    public DimClient? Client { get; set; }
    public DimProduct? Product { get; set; }
    public DimTime? Time { get; set; }
    public DimSource? Source { get; set; }
    public DimSentiment? Sentiment { get; set; }
}