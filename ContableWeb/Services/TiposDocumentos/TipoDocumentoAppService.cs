using ContableWeb.Entities.TiposDocumentos;
using ContableWeb.Services.Afip;
using ContableWeb.Services.Dtos.TiposDocumentos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Repositories;

namespace ContableWeb.Services.TiposDocumentos;

[Audited]
public class TipoDocumentoAppService : CrudAppService<
        TipoDocumento,
        TipoDocumentoDto,
        int,
        PagedAndSortedResultRequestDto,
        CreateUpdateTipoDocumentoDto>,
    ITipoDocumentoAppService
{
    private readonly IFacturacionElectronicaService _facturacionElectronicaService;
    private readonly ILogger<TipoDocumentoAppService> _logger;

    public TipoDocumentoAppService(
        IRepository<TipoDocumento, int> repository,
        IFacturacionElectronicaService facturacionElectronicaService,
        ILogger<TipoDocumentoAppService> logger) : base(repository)
    {
        _facturacionElectronicaService = facturacionElectronicaService;
        _logger = logger;
    }

    /// <summary>
    /// Sincroniza los tipos de documentos desde AFIP a la base de datos
    /// </summary>
    public async Task<SincronizacionTiposDocumentoResult> SincronizarDesdeAfipAsync()
    {
        var result = new SincronizacionTiposDocumentoResult();

        try
        {
            _logger.LogInformation("=== INICIANDO SINCRONIZACIÓN DE TIPOS DE DOCUMENTOS DESDE AFIP ===");

            // 1. Obtener tipos de documentos desde AFIP
            var tiposAfip = await _facturacionElectronicaService.ObtenerTiposDocumentoAsync();
            result.TotalObtenidos = tiposAfip.Count;

            _logger.LogInformation($"Se obtuvieron {tiposAfip.Count} tipos de documentos desde AFIP");

            if (!tiposAfip.Any())
            {
                result.Exitoso = true;
                result.Mensaje = "No se encontraron tipos de documentos en AFIP";
                return result;
            }

            // 2. Obtener todos los tipos de documentos existentes en BD
            var tiposExistentes = await Repository.GetListAsync();

            // 3. Procesar cada tipo de documento de AFIP
            foreach (var tipoAfip in tiposAfip)
            {
                try
                {
                    // Buscar si ya existe en BD por CodigoAfip
                    var tipoExistente = tiposExistentes.FirstOrDefault(t => t.CodigoAfip == tipoAfip.Id);

                    if (tipoExistente != null)
                    {
                        // ACTUALIZAR registro existente
                        tipoExistente.Descripcion = tipoAfip.Descripcion;
                        tipoExistente.FechaDesde = ParseFechaAfip(tipoAfip.FechaDesde);
                        tipoExistente.FechaHasta = ParseFechaAfip(tipoAfip.FechaHasta);
                        tipoExistente.Enabled = true;

                        await Repository.UpdateAsync(tipoExistente);
                        result.Actualizados++;

                        _logger.LogInformation($"Actualizado: {tipoAfip.Id} - {tipoAfip.Descripcion}");
                    }
                    else
                    {
                        // INSERTAR nuevo registro
                        var nuevoTipo = new TipoDocumento
                        {
                            CodigoAfip = tipoAfip.Id,
                            Descripcion = tipoAfip.Descripcion,
                            FechaDesde = ParseFechaAfip(tipoAfip.FechaDesde),
                            FechaHasta = ParseFechaAfip(tipoAfip.FechaHasta),
                            Enabled = true
                        };

                        await Repository.InsertAsync(nuevoTipo);
                        result.Insertados++;

                        _logger.LogInformation($"Insertado: {tipoAfip.Id} - {tipoAfip.Descripcion}");
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Error procesando tipo {tipoAfip.Id} - {tipoAfip.Descripcion}: {ex.Message}";
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
            _logger.LogError(ex, "Error en sincronización de tipos de documentos");
        }

        return result;
    }

    /// <summary>
    /// Parsea fechas en formato AFIP (yyyyMMdd) a DateOnly
    /// </summary>
    private DateOnly? ParseFechaAfip(string? fecha)
    {
        if (string.IsNullOrWhiteSpace(fecha) || fecha == "00000000")
            return null;

        try
        {
            if (fecha.Length == 8)
            {
                var year = int.Parse(fecha.Substring(0, 4));
                var month = int.Parse(fecha.Substring(4, 2));
                var day = int.Parse(fecha.Substring(6, 2));
                return new DateOnly(year, month, day);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"No se pudo parsear la fecha '{fecha}': {ex.Message}");
        }

        return null;
    }
}

