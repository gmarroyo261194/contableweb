using Volo.Abp.Domain.Entities.Auditing;

namespace ContableWeb.Entities.TiposComprobantes;

public class TipoComprobante: AuditedEntity<int>
{
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? Abreviatura { get; set; }
    public bool EsFiscal { get; set; }
    public bool Enabled { get; set; } = true;
    
}