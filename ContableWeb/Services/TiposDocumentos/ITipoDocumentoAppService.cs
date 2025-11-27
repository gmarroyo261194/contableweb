using ContableWeb.Services.Dtos.TiposDocumentos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace ContableWeb.Services.TiposDocumentos;

public interface ITipoDocumentoAppService : ICrudAppService<
    TipoDocumentoDto,
    int,
    PagedAndSortedResultRequestDto,
    CreateUpdateTipoDocumentoDto>
{
    /// <summary>
    /// Sincroniza los tipos de documentos desde AFIP a la base de datos
    /// </summary>
    /// <returns>Resultado de la sincronización</returns>
    Task<SincronizacionTiposDocumentoResult> SincronizarDesdeAfipAsync();
}

