using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities.Auditing;

namespace ContableWeb.Entities.TiposComprobantes;

[Audited]
public class TipoComprobante: AuditedEntity<int>
{
    public int CodigoAfip { get; set; } // Código del tipo de comprobante en AFIP
    public required string Nombre { get; set; }
    public string? Abreviatura { get; set; }
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public bool EsFiscal { get; set; }
    public bool Enabled { get; set; } = true;
    
}