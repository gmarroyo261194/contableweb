using Volo.Abp.Application.Dtos;

namespace ContableWeb.Services.Dtos.Rubros;

public class RubroDto: AuditedEntityDto<int>
{
    public string Nombre { get; set; } = null!;

    public bool Enabled { get; set; }
    
}