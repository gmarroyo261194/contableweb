using ContableWeb.Entities.Rubros;
using ContableWeb.Services.Dtos.Rubros;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace ContableWeb.Services.Rubros;

public class RubroAppService:CrudAppService<Rubro,RubroDto, int, PagedAndSortedResultRequestDto, CreateUpdateRubroDto>, IRubroAppService
{
    public RubroAppService(IRepository<Rubro, int> repository) : base(repository)
    {
    }
}