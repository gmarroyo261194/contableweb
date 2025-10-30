using Volo.Abp.Application.Dtos;

namespace ContableWeb.Services.Dtos.Servicios;

public class RubroLookupDto: EntityDto<int>
{
    public string Nombre { get; set; } = null!;
}