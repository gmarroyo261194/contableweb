using Volo.Abp.Application.Dtos;

namespace ContableWeb.Services.Dtos.TiposComprobantes;

public class TipoComprobanteDto: AuditedEntityDto<int>
{
    public int CodigoAfip { get; set; }
    public string Nombre { get; set; } = null!;
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public string? Abreviatura { get; set; }
    public bool EsFiscal { get; set; }
    public bool Enabled { get; set; }
}