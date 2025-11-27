using ContableWeb.Services.Dtos.TiposCondicionesIva;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace ContableWeb.Services.TiposCondicionesIva;

public interface ITipoCondicionIvaAppService : ICrudAppService<
    TipoCondicionIvaDto,
    int,
    PagedAndSortedResultRequestDto,
    CreateUpdateTipoCondicionIvaDto>
{
    /// <summary>
    /// Sincroniza los tipos de condiciones IVA desde AFIP a la base de datos
    /// </summary>
    /// <returns>Resultado de la sincronización</returns>
    Task<SincronizacionTiposCondicionIvaResult> SincronizarDesdeAfipAsync();
}

