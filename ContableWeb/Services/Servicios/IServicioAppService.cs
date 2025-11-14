using ContableWeb.Services.Dtos.Servicios;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace ContableWeb.Services.Servicios;

public interface IServicioAppService: 
    ICrudAppService<ServicioDto,int,PagedAndSortedResultRequestDto,CreateUpdateServicioDto>
{
    Task<ListResultDto<RubroLookupDto>> GetRubroLookupAsync();
    Task<PagedResultDto<ServicioDto>> GetServiciosByRubroIdAsync(int rubroId);
    Task<PagedResultDto<ServicioDto>> GetServiciosByRubroIdAsync(int rubroId, PagedAndSortedResultRequestDto input);
}