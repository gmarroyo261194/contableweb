using ContableWeb.Services.Dtos.TiposComprobantes;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace ContableWeb.Services.TiposComprobantes;

public interface ITipoComprobanteAppService: ICrudAppService<
    TipoComprobanteDto,
    int,
    PagedAndSortedResultRequestDto,
    CreateUpdateTipoComprobanteDto>
{
    
}