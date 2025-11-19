using ContableWeb.Entities.TiposComprobantes;
using ContableWeb.Services.Dtos.TiposComprobantes;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Repositories;

namespace ContableWeb.Services.TiposComprobantes;

[Audited]
public class TipoComprobanteAppService: CrudAppService<
        TipoComprobante,
        TipoComprobanteDto,
        int,
        PagedAndSortedResultRequestDto,
        CreateUpdateTipoComprobanteDto>,
    ITipoComprobanteAppService
{
    public TipoComprobanteAppService(IRepository<TipoComprobante, int> repository) : base(repository)
    {
    }
}