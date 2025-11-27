using Volo.Abp.Application.Dtos;

namespace ContableWeb.Services.Dtos.TiposDocumentos;

public class TipoDocumentoDto : AuditedEntityDto<int>
{
    public int CodigoAfip { get; set; }
    public string Descripcion { get; set; } = null!;
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public bool Enabled { get; set; }
}

