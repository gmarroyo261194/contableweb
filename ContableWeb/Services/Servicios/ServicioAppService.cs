using ContableWeb.Entities.Rubros;
using ContableWeb.Entities.Servicios;
using ContableWeb.Services.Dtos.Servicios;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace ContableWeb.Services.Servicios;

public class ServicioAppService :
    CrudAppService<Servicio, ServicioDto, int, PagedAndSortedResultRequestDto, CreateUpdateServicioDto>,
    IServicioAppService
{
    private readonly IRepository<Rubro, int> _rubroRepository;
    
    public ServicioAppService(IRepository<Servicio, int> repository, IRepository<Rubro, int> rubroRepository) : base(repository)
    {
        _rubroRepository = rubroRepository;
    }

    public override async Task<PagedResultDto<ServicioDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var queryable = await Repository.GetQueryableAsync();
        var query = from servicio in queryable
            join rubro in await _rubroRepository.GetQueryableAsync() on servicio.RubroId equals rubro.Id
            select new { servicio, rubro };
        
        var pagedQuery = query
            .OrderBy(x => x.servicio.Nombre)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var queryResult = await AsyncExecuter.ToListAsync(pagedQuery);
        var servicioDtos = queryResult.Select(x =>
            {
                var servicioDto = ObjectMapper.Map<Servicio, ServicioDto>(x.servicio);
                servicioDto.RubroNombre = x.rubro.Nombre;
                return servicioDto;
            })
            .ToList();  
        var totalCount = await AsyncExecuter.CountAsync(query);

        return new PagedResultDto<ServicioDto>(totalCount, servicioDtos);
    }

    public async Task<ListResultDto<RubroLookupDto>> GetRubroLookupAsync()
    {
        var rubros = await _rubroRepository.GetListAsync();
        var rubroLookupDtos = ObjectMapper.Map<List<Rubro>, List<RubroLookupDto>>(rubros);
        return new ListResultDto<RubroLookupDto>(rubroLookupDtos);
    }

    public async Task<PagedResultDto<ServicioDto>> GetServiciosByRubroIdAsync(int rubroId)
    {
        var queryable = await Repository.GetQueryableAsync();
        var filtered = queryable.Where(s => s.RubroId == rubroId);
        var totalCount = await AsyncExecuter.CountAsync(filtered);
        var servicios = await AsyncExecuter.ToListAsync(filtered);
        var servicioDtos = ObjectMapper.Map<List<Servicio>, List<ServicioDto>>(servicios);
        return new PagedResultDto<ServicioDto>(totalCount, servicioDtos);
    }
}