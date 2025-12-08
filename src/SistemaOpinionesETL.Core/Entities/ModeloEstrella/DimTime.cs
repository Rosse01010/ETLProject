
namespace SistemaOpinionesETL.Core.Entities.ModeloEstrella;

/// <summary>
/// Dimensión de Tiempo
/// Tabla de calendario para el análisis temporal
/// </summary>
public class DimTime
{
    // Campo clave
    public int time_key { get; set; } // Corregido: t_id_date -> time_key
    public DateTime full_date { get; set; }

    // Jerarquías temporales
    public int day { get; set; }
    public int week { get; set; }
    public int month { get; set; }
    public int quarter { get; set; }
    public int year { get; set; }

    // Nombres descriptivos
    public string day_name { get; set; } = string.Empty;
    public string month_name { get; set; } = string.Empty;
    public string quarter_name { get; set; } = string.Empty;

    // Indicadores especiales
    public bool is_weekend { get; set; }
    public bool is_holiday { get; set; }
    public string holiday_name { get; set; } = string.Empty;

    // Campo corregido
    public bool is_business_day { get; set; } = true; // Corregido: is_officer -> is_business_day

    // Metadata
    public DateTime created_date { get; set; } = DateTime.UtcNow;
}