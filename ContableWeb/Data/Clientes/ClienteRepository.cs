using ContableWeb.Entities.Clientes;
using ContableWeb.Services.Clientes;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;

namespace ContableWeb.Data.Clientes;

public class ClienteRepository: EfCoreRepository<ContableWebDbContext,Cliente,int>, IClienteRepository
{
    public ClienteRepository(IDbContextProvider<ContableWebDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }

    public async Task<List<Cliente>> GetListAsync(int skipCount, int maxResultCount, string sorting = "Nombre", ClientesFilter? filter = null)
    {
        try
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet.AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.Nombre))
                {
                    query = query.Where(x => x.Nombre.Contains(filter.Nombre));
                }

                if (!string.IsNullOrWhiteSpace(filter.NumeroDocumento))
                {
                    query = query.Where(x => x.NumeroDocumento.Contains(filter.NumeroDocumento));
                }

                if (!string.IsNullOrWhiteSpace(filter.Email))
                {
                    query = query.Where(x => x.Email.Contains(filter.Email));
                }

                if (!string.IsNullOrWhiteSpace(filter.Nombre))
                {
                    query = query.Where(x => x.Nombre.ToLower().Contains(filter.Nombre.ToLower()));
                }
            }

            var clientes = await query
                .OrderBy(sorting)
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync();

            return clientes;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new BusinessException("1","Error al obtener la lista de clientes.", e.Message);
        }
    }

    public async Task<int> GetTotalCountAsync(ClientesFilter filter)
    {
        try
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Nombre))
            {
                query = query.Where(x => x.Nombre.Contains(filter.Nombre));
            }

            if (!string.IsNullOrWhiteSpace(filter.NumeroDocumento))
            {
                query = query.Where(x => x.NumeroDocumento.Contains(filter.NumeroDocumento));
            }

            if (!string.IsNullOrWhiteSpace(filter.Email))
            {
                query = query.Where(x => x.Email!.Contains(filter.Email));
            }

            if (!string.IsNullOrWhiteSpace(filter.Nombre))
            {
                query = query.Where(x => x.Nombre.ToLower().Contains(filter.Nombre.ToLower()));
            }

            var count = await query.CountAsync();
            return count;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new BusinessException("1","Error al obtener el conteo total de clientes.", e.Message);
        }
    }
}