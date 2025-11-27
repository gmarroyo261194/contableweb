using Volo.Abp.Application.Dtos;

namespace ContableWeb.Services.Dtos.TiposCondicionesIva;

public class TipoCondicionIvaDto : AuditedEntityDto<int>
{
    public int CodigoAfip { get; set; }
    public string Descripcion { get; set; } = null!;
    public bool Enabled { get; set; }
}

