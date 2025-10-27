using ContableWeb.Services.Dtos.Rubros;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace ContableWeb.Services.Rubros;

public interface IRubroAppService :
    ICrudAppService<
        RubroDto,
        int,
        PagedAndSortedResultRequestDto,
        CreateUpdateRubroDto>
{
}