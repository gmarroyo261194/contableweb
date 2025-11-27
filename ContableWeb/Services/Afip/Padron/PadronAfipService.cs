namespace ContableWeb.Services.Afip.Padron;

/// <summary>
/// Servicio de alto nivel para consultar el Padrón AFIP
/// </summary>
public interface IPadronAfipService
{
    /// <summary>
    /// Consulta los datos de una persona por CUIT/CUIL
    /// </summary>
    Task<PersonaReturn> ConsultarPersonaAsync(long cuit);
    
    /// <summary>
    /// Consulta los datos de múltiples personas
    /// </summary>
    Task<PersonaListReturn> ConsultarPersonasAsync(long[] cuits);
    
    /// <summary>
    /// Verifica la conectividad con el servicio
    /// </summary>
    Task<bool> VerificarConectividadAsync();
    
    /// <summary>
    /// Obtiene la condición frente al IVA de una persona
    /// </summary>
    Task<string?> ObtenerCondicionIvaAsync(long cuit);
    
    /// <summary>
    /// Verifica si una persona es Monotributista
    /// </summary>
    Task<bool> EsMonotributistaAsync(long cuit);
    
    /// <summary>
    /// Obtiene el domicilio fiscal de una persona
    /// </summary>
    Task<string?> ObtenerDomicilioFiscalAsync(long cuit);
}

public class PadronAfipService : IPadronAfipService
{
    private readonly PadronA5Client _padronClient;
    private readonly IAfipTokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PadronAfipService> _logger;
    private readonly long _cuitEmisor;

    public PadronAfipService(
        PadronA5Client padronClient,
        IAfipTokenService tokenService,
        IConfiguration configuration,
        ILogger<PadronAfipService> logger)
    {
        _padronClient = padronClient;
        _tokenService = tokenService;
        _configuration = configuration;
        _logger = logger;
        
        // Obtener CUIT desde configuración (Afip:CuitEmisor)
        _cuitEmisor = configuration.GetValue<long>("Afip:CuitEmisor");
        
        if (_cuitEmisor == 0)
        {
            _logger.LogError("⚠️ CUIT no configurado en appsettings.json. Verifique la configuración 'Afip:CuitEmisor'");
            throw new PadronAfipException("CUIT del emisor no configurado. Verifique 'Afip:CuitEmisor' en appsettings.json");
        }
        
        _logger.LogInformation("PadronAfipService inicializado con CUIT: {Cuit}", _cuitEmisor);
    }

    /// <summary>
    /// Consulta los datos de una persona por CUIT/CUIL
    /// </summary>
    public async Task<PersonaReturn> ConsultarPersonaAsync(long cuit)
    {
        try
        {
            _logger.LogInformation("Consultando persona en Padrón AFIP: {Cuit}", cuit);
            _logger.LogInformation("CUIT Representada (Emisor): {CuitEmisor}", _cuitEmisor);
            
            // Obtener token válido para ws_sr_constancia_inscripcion
            var token = await _tokenService.GetValidTokenAsync("ws_sr_constancia_inscripcion");
            
            if (token == null)
            {
                throw new PadronAfipException("No se pudo obtener token de AFIP para consulta de padrón");
            }
            
            _logger.LogInformation("Token obtenido, expira: {Expiration}", token.ExpirationTime);
            
            // Consultar en padrón
            var resultado = await _padronClient.GetPersonaAsync(
                token.Token,
                token.Sign,
                _cuitEmisor,
                cuit
            );
            
            _logger.LogInformation("Persona consultada exitosamente: {Cuit}", cuit);
            
            // Verificar si hay errores
            if (resultado.Persona?.ErrorConstancia != null)
            {
                var errores = string.Join(", ", resultado.Persona.ErrorConstancia.Errores ?? Array.Empty<string>());
                _logger.LogWarning("Errores en constancia: {Errores}", errores);
            }
            
            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando persona {Cuit}", cuit);
            throw;
        }
    }

    /// <summary>
    /// Consulta los datos de múltiples personas
    /// </summary>
    public async Task<PersonaListReturn> ConsultarPersonasAsync(long[] cuits)
    {
        try
        {
            _logger.LogInformation("Consultando {Count} personas en Padrón AFIP", cuits.Length);
            
            // Obtener token válido
            var token = await _tokenService.GetValidTokenAsync("ws_sr_constancia_inscripcion");
            
            if (token == null)
            {
                throw new PadronAfipException("No se pudo obtener token de AFIP para consulta de padrón");
            }
            
            // Consultar en padrón
            var resultado = await _padronClient.GetPersonaListAsync(
                token.Token,
                token.Sign,
                _cuitEmisor,
                cuits
            );
            
            _logger.LogInformation("Personas consultadas exitosamente: {Count}", cuits.Length);
            
            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando lista de personas");
            throw;
        }
    }

    /// <summary>
    /// Verifica la conectividad con el servicio
    /// </summary>
    public async Task<bool> VerificarConectividadAsync()
    {
        try
        {
            _logger.LogInformation("Verificando conectividad con Padrón AFIP");
            
            var dummy = await _padronClient.DummyAsync();
            
            _logger.LogInformation("Conectividad OK - AppServer: {AppServer}, DbServer: {DbServer}, AuthServer: {AuthServer}",
                dummy.AppServer, dummy.DbServer, dummy.AuthServer);
            
            return !string.IsNullOrEmpty(dummy.AppServer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando conectividad");
            return false;
        }
    }

    /// <summary>
    /// Obtiene la condición frente al IVA de una persona
    /// </summary>
    public async Task<string?> ObtenerCondicionIvaAsync(long cuit)
    {
        try
        {
            var resultado = await ConsultarPersonaAsync(cuit);
            
            if (resultado.Persona?.DatosGenerales != null)
            {
                // La condición IVA se puede inferir de varios lugares
                // Según los impuestos activos y el régimen
                
                var impuestos = resultado.Persona.DatosRegimenGeneral?.Impuestos;
                if (impuestos != null)
                {
                    // Buscar impuesto IVA (ID 30)
                    var iva = impuestos.FirstOrDefault(i => i.IdImpuesto == 30);
                    if (iva != null)
                    {
                        // Si tiene IVA activo, es Responsable Inscripto
                        if (iva.EstadoImpuesto == "ACTIVO")
                        {
                            return "Responsable Inscripto";
                        }
                    }
                }
                
                // Verificar si es Monotributista
                if (resultado.Persona.DatosMonotributo?.CategoriaMonotributo != null)
                {
                    return "Responsable Monotributo";
                }
                
                // Por defecto, si no tiene ningún régimen especial
                return "Consumidor Final";
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo condición IVA para {Cuit}", cuit);
            return null;
        }
    }

    /// <summary>
    /// Verifica si una persona es Monotributista
    /// </summary>
    public async Task<bool> EsMonotributistaAsync(long cuit)
    {
        try
        {
            var resultado = await ConsultarPersonaAsync(cuit);
            return resultado.Persona?.DatosMonotributo?.CategoriaMonotributo != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando si {Cuit} es monotributista", cuit);
            return false;
        }
    }

    /// <summary>
    /// Obtiene el domicilio fiscal de una persona
    /// </summary>
    public async Task<string?> ObtenerDomicilioFiscalAsync(long cuit)
    {
        try
        {
            var resultado = await ConsultarPersonaAsync(cuit);
            
            var domicilio = resultado.Persona?.DatosGenerales?.DomicilioFiscal;
            
            if (domicilio == null) return null;
            
            // Construir dirección completa
            var direccionCompleta = new List<string>();
            
            if (!string.IsNullOrEmpty(domicilio.Direccion))
                direccionCompleta.Add(domicilio.Direccion);
                
            if (!string.IsNullOrEmpty(domicilio.Localidad))
                direccionCompleta.Add(domicilio.Localidad);
                
            if (!string.IsNullOrEmpty(domicilio.DescripcionProvincia))
                direccionCompleta.Add(domicilio.DescripcionProvincia);
                
            if (!string.IsNullOrEmpty(domicilio.CodigoPostal))
                direccionCompleta.Add($"CP {domicilio.CodigoPostal}");
            
            return string.Join(", ", direccionCompleta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo domicilio fiscal para {Cuit}", cuit);
            return null;
        }
    }
}

