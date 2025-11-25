using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ContableWeb.Services.Afip
{
    /// <summary>
    /// Configuración del servicio AFIP
    /// </summary>
    public class AfipConfiguration
    {
        public string CertificatePath { get; set; } = string.Empty;
        public string CertificatePassword { get; set; } = string.Empty;
        public bool IsProduction { get; set; }
        public bool UsePowerShell { get; set; } = true;
        public string? PowerShellScriptPath { get; set; }
    }

    /// <summary>
    /// Interfaz del servicio de autenticación AFIP
    /// </summary>
    public interface IAfipAuthService
    {
        Task<AfipAuthResult> GetAuthenticationAsync(string serviceId);
        Task<AfipAuthResult> GetAuthenticationWithCacheAsync(string serviceId);
        void ClearCache(string serviceId);
    }

    /// <summary>
    /// Servicio de autenticación AFIP con caché
    /// </summary>
    public class AfipAuthService : IAfipAuthService
    {
        private readonly AfipWsaaClient _wsaaClient;
        private readonly PowerShellAfipService _powerShellService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AfipAuthService> _logger;
        private readonly bool _usePowerShell;

        public AfipAuthService(
            IOptions<AfipConfiguration> options,
            IMemoryCache cache,
            ILogger<AfipAuthService> logger,
            PowerShellAfipService powerShellService)
        {
            var config = options.Value;
            _wsaaClient = new AfipWsaaClient(
                config.CertificatePath,
                config.CertificatePassword,
                config.IsProduction
            );
            _powerShellService = powerShellService;
            _cache = cache;
            _logger = logger;
            _usePowerShell = config.UsePowerShell;
        }

        /// <summary>
        /// Obtiene autenticación sin caché
        /// </summary>
        public async Task<AfipAuthResult> GetAuthenticationAsync(string serviceId)
        {
            _logger.LogInformation("Obteniendo autenticación AFIP para servicio: {ServiceId} (UsePowerShell: {UsePowerShell})", serviceId, _usePowerShell);

            // Intentar primero con PowerShell si está habilitado
            if (_usePowerShell)
            {
                try
                {
                    Console.WriteLine("=== INTENTANDO AUTENTICACIÓN CON POWERSHELL ===");
                    var powershellResult = await _powerShellService.ExecuteAfipAuthenticationAsync(serviceId);
                    
                    if (powershellResult.IsSuccess)
                    {
                        _logger.LogInformation("Autenticación exitosa con PowerShell. Expira: {ExpirationTime}", powershellResult.Token?.ExpirationTime);
                        return powershellResult;
                    }
                    else
                    {
                        _logger.LogWarning("PowerShell falló, intentando con implementación C#: {Error}", powershellResult.Error?.Message);
                        Console.WriteLine($"⚠️ PowerShell falló: {powershellResult.Error?.Message}");
                        Console.WriteLine("=== INTENTANDO CON IMPLEMENTACIÓN C# ===");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error ejecutando PowerShell, intentando con implementación C#");
                    Console.WriteLine($"⚠️ Excepción en PowerShell: {ex.Message}");
                    Console.WriteLine("=== INTENTANDO CON IMPLEMENTACIÓN C# ===");
                }
            }

            // Fallback a implementación C# original
            try
            {
                var response = await _wsaaClient.LoginAsync(serviceId);
                _logger.LogInformation("Autenticación exitosa con C#. Expira: {ExpirationTime}", response.ExpirationTime);
                return AfipAuthResult.Success(response);
            }
            catch (AfipSoapFaultException ex)
            {
                _logger.LogError(ex, "SOAP Fault de AFIP: {FaultCode} - {FaultString}", ex.FaultCode, ex.FaultString);
                return AfipAuthResult.Failure(new ErrorResponse
                {
                    Message = ex.Message,
                    ExceptionType = ex.GetType().FullName,
                    FaultCode = ex.FaultCode,
                    FaultString = ex.FaultString,
                    ExceptionName = ex.ExceptionName,
                    Hostname = ex.Hostname
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener autenticación AFIP");
                return AfipAuthResult.Failure(new ErrorResponse
                {
                    Message = ex.Message,
                    ExceptionType = ex.GetType().FullName,
                    StackTrace = ex.ToString()
                });
            }
        }

        /// <summary>
        /// Obtiene autenticación con caché (recomendado)
        /// </summary>
        public async Task<AfipAuthResult> GetAuthenticationWithCacheAsync(string serviceId)
        {
            var cacheKey = $"afip_auth_{serviceId}";

            // Intentar obtener del caché
            if (_cache.TryGetValue(cacheKey, out WsaaResponse? cachedResponse) &&
                cachedResponse != null &&
                !cachedResponse.IsExpired)
            {
                _logger.LogInformation("Usando autenticación en caché para servicio: {ServiceId}", serviceId);
                return AfipAuthResult.Success(cachedResponse);
            }

            // Si no está en caché o expiró, obtener nueva autenticación
            _logger.LogInformation("Caché no disponible o expirado, obteniendo nueva autenticación para: {ServiceId}", serviceId);
            var result = await GetAuthenticationAsync(serviceId);

            if (result.IsSuccess && result.Token != null)
            {
                // Guardar en caché (expira 5 minutos antes del tiempo real de expiración)
                var cacheExpiration = result.Token.ExpirationTime.AddMinutes(-5);
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(cacheExpiration);

                _cache.Set(cacheKey, result.Token, cacheEntryOptions);
                _logger.LogInformation("Autenticación guardada en caché hasta: {CacheExpiration}", cacheExpiration);
            }

            return result;
        }

        /// <summary>
        /// Limpia el caché para un servicio específico
        /// </summary>
        public void ClearCache(string serviceId)
        {
            var cacheKey = $"afip_auth_{serviceId}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Caché limpiado para servicio: {ServiceId}", serviceId);
        }
    }

    /// <summary>
    /// Resultado de la autenticación AFIP (éxito o error estructurado)
    /// </summary>
    public class AfipAuthResult
    {
        public bool IsSuccess { get; set; }
        public WsaaResponse? Token { get; set; }
        public ErrorResponse? Error { get; set; }

        public static AfipAuthResult Success(WsaaResponse token) => new AfipAuthResult { IsSuccess = true, Token = token };
        public static AfipAuthResult Failure(ErrorResponse error) => new AfipAuthResult { IsSuccess = false, Error = error };
    }

    /// <summary>
    /// Representación estructurada de un error retornado por el flujo AFIP
    /// </summary>
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? ExceptionType { get; set; }
        public string? StackTrace { get; set; }

        // Opcionales para SOAP Fault
        public string? FaultCode { get; set; }
        public string? FaultString { get; set; }
        public string? ExceptionName { get; set; }
        public string? Hostname { get; set; }
    }
}
