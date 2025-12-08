
namespace SistemaOpinionesETL.Core.Entities.ModeloEstrella;

/// <summary>
/// Dimensión de Producto
/// Contiene información sobre los productos analizados
/// </summary>
public class DimProduct
{
    // Campos clave
    public int product_key { get; set; }
    public string product_id { get; set; } = string.Empty;
    public string product_name { get; set; } = string.Empty;

    // Detalles
    public string category { get; set; } = string.Empty;
    public string subcategory { get; set; } = string.Empty;
    public decimal price { get; set; }
    public DateTime launch_date { get; set; } // Corregido: bunch_date -> launch_date

    // Campos de control (SCD Type 2)
    public bool is_active { get; set; } = true; // Corregido: is_write -> is_active
    public DateTime effective_date { get; set; } = DateTime.UtcNow;
    public DateTime? end_date { get; set; }
    public bool is_current { get; set; } = true;
    public DateTime created_date { get; set; } = DateTime.UtcNow;
}