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
    /// <summary>
    /// Sincroniza los tipos de comprobantes desde AFIP a la base de datos
    /// </summary>
    /// <returns>Resultado de la sincronización</returns>
    Task<SincronizacionTiposComprobanteResult> SincronizarDesdeAfipAsync();
}