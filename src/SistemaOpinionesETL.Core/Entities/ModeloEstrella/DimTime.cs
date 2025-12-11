
namespace SistemaOpinionesETL.Core.Entities.ModeloEstrella;

public class DimTime
{
    
    public int time_key { get; set; } // Corregido: t_id_date -> time_key
    public DateTime full_date { get; set; }

    
    public int day { get; set; }
    public int week { get; set; }
    public int month { get; set; }
    public int quarter { get; set; }
    public int year { get; set; }

    
    public string day_name { get; set; } = string.Empty;
    public string month_name { get; set; } = string.Empty;
    public string quarter_name { get; set; } = string.Empty;

    
    public bool is_weekend { get; set; }
    public bool is_holiday { get; set; }
    public string holiday_name { get; set; } = string.Empty;

    
    public bool is_business_day { get; set; } = true; // Corregido: is_officer -> is_business_day

    
    public DateTime created_date { get; set; } = DateTime.UtcNow;
}