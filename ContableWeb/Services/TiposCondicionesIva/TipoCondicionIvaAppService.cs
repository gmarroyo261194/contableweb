using ContableWeb.Entities.TiposCondicionesIva;
using ContableWeb.Services.Afip;
using ContableWeb.Services.Dtos.TiposCondicionesIva;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Repositories;

namespace ContableWeb.Services.TiposCondicionesIva;

[Audited]
public class TipoCondicionIvaAppService : CrudAppService<
        TipoCondicionIva,
        TipoCondicionIvaDto,
        int,
        PagedAndSortedResultRequestDto,
        CreateUpdateTipoCondicionIvaDto>,
    ITipoCondicionIvaAppService
{
    private readonly IFacturacionElectronicaService _facturacionElectronicaService;
    private readonly ILogger<TipoCondicionIvaAppService> _logger;

    public TipoCondicionIvaAppService(
        IRepository<TipoCondicionIva, int> repository,
        IFacturacionElectronicaService facturacionElectronicaService,
        ILogger<TipoCondicionIvaAppService> logger) : base(repository)
    {
        _facturacionElectronicaService = facturacionElectronicaService;
        _logger = logger;
    }

    /// <summary>
    /// Sincroniza los tipos de condiciones IVA desde AFIP a la base de datos
    /// </summary>
    public async Task<SincronizacionTiposCondicionIvaResult> SincronizarDesdeAfipAsync()
    {
        var result = new SincronizacionTiposCondicionIvaResult();

        try
        {
            _logger.LogInformation("=== INICIANDO SINCRONIZACIÓN DE CONDICIONES IVA DESDE AFIP ===");

            // 1. Obtener condiciones IVA desde AFIP
            var condicionesAfip = await _facturacionElectronicaService.ObtenerCondicionesIvaAsync();
            result.TotalObtenidos = condicionesAfip.Count;

            _logger.LogInformation($"Se obtuvieron {condicionesAfip.Count} condiciones IVA desde AFIP");

            if (!condicionesAfip.Any())
            {
                result.Exitoso = true;
                result.Mensaje = "No se encontraron condiciones IVA en AFIP";
                return result;
            }

            // 2. Obtener todos los registros existentes en BD
            var condicionesExistentes = await Repository.GetListAsync();

            // 3. Procesar cada condición IVA de AFIP
            foreach (var condicionAfip in condicionesAfip)
            {
                try
                {
                    // Buscar si ya existe en BD por CodigoAfip
                    var condicionExistente = condicionesExistentes.FirstOrDefault(c => c.CodigoAfip == condicionAfip.Id);

                    if (condicionExistente != null)
                    {
                        // ACTUALIZAR registro existente
                        condicionExistente.Descripcion = condicionAfip.Descripcion;
                        condicionExistente.Enabled = true;

                        await Repository.UpdateAsync(condicionExistente);
                        result.Actualizados++;

                        _logger.LogInformation($"Actualizado: {condicionAfip.Id} - {condicionAfip.Descripcion}");
                    }
                    else
                    {
                        // INSERTAR nuevo registro
                        var nuevaCondicion = new TipoCondicionIva
                        {
                            CodigoAfip = condicionAfip.Id,
                            Descripcion = condicionAfip.Descripcion,
                            Enabled = true
                        };

                        await Repository.InsertAsync(nuevaCondicion);
                        result.Insertados++;

                        _logger.LogInformation($"Insertado: {condicionAfip.Id} - {condicionAfip.Descripcion}");
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Error procesando condición {condicionAfip.Id} - {condicionAfip.Descripcion}: {ex.Message}";
                    result.Errores.Add(error);
                    _logger.LogError(ex, error);
                }
            }

            result.Exitoso = true;
            result.Mensaje = $"Sincronización completada: {result.Insertados} insertados, {result.Actualizados} actualizados";

            _logger.LogInformation($"=== SINCRONIZACIÓN COMPLETADA ===");
            _logger.LogInformation($"Total obtenidos: {result.TotalObtenidos}");
            _logger.LogInformation($"Insertados: {result.Insertados}");
            _logger.LogInformation($"Actualizados: {result.Actualizados}");
            _logger.LogInformation($"Errores: {result.Errores.Count}");
        }
        catch (Exception ex)
        {
            result.Exitoso = false;
            result.Mensaje = $"Error en sincronización: {ex.Message}";
            result.Errores.Add(ex.Message);
            _logger.LogError(ex, "Error en sincronización de condiciones IVA");
        }

        return result;
    }
}

