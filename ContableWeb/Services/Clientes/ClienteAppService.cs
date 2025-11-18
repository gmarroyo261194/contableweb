using ContableWeb.Entities.Clientes;
using ContableWeb.Services.Dtos.Clientes;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Repositories;

namespace ContableWeb.Services.Clientes;

[Audited]
public class ClienteAppService: CrudAppService<Cliente, ClienteDto, int, PagedAndSortedResultRequestDto, CreateUpdateClienteDto>,
    IClienteAppService
{
    public ClienteAppService(IRepository<Cliente, int> repository) : base(repository)
    {
    }

    public override async Task<ClienteDto> CreateAsync(CreateUpdateClienteDto input)
    {
        if(await GetDocumentoDuplicadoAsync(input.TipoDocumento, input.NumeroDocumento))
        {
            throw new UserFriendlyException("Datos Duplicados", "DocumentoDuplicado",$"Existe un cliente en con el mismo tipo {input.TipoDocumento} y número {input.NumeroDocumento}.");
        }
        return await base.CreateAsync(input);
    }

    public override async Task<ClienteDto> UpdateAsync(int id, CreateUpdateClienteDto input)
    {
        if(await GetDocumentoDuplicadoAsync(input.TipoDocumento, input.NumeroDocumento, id))
        {
            throw new UserFriendlyException("Datos Duplicados", "DocumentoDuplicado",$"Existe un cliente en con el mismo tipo {input.TipoDocumento} y número {input.NumeroDocumento}.");
        }
        return await base.UpdateAsync(id, input);
    }

    public async Task<bool> GetDocumentoDuplicadoAsync(TipoDoc tipoDoc, string numeroDoc, int id = 0)
    {
        if (id > 0)
        {
            var cliente = await Repository.GetAsync(id);
            return cliente.TipoDocumento != tipoDoc || cliente.NumeroDocumento != numeroDoc;
        }

        var count = await Repository.CountAsync(c => c.TipoDocumento == tipoDoc && c.NumeroDocumento == numeroDoc);
        return count > 0;
    }
}