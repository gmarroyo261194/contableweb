using Volo.Abp.Application.Dtos;

namespace ContableWeb.Services.Dtos.TiposComprobantes;

public class TipoComprobanteDto: AuditedEntityDto<int>
{
    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? Abreviatura { get; set; }

    public bool EsFiscal { get; set; }

    public bool Enabled { get; set; }
}