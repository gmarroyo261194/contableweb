using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace ContableWeb.Services.Afip
{
    /// <summary>
    /// Servicio global para manejo de tokens AFIP
    /// Mantiene tokens válidos en memoria y los regenera automáticamente cuando expiran
    /// Incluye persistencia en base de datos para recuperar tokens después de reinicios
    /// </summary>
    public class AfipTokenService : IAfipTokenService, IDisposable
    {
        private readonly IAfipAuthService _afipAuthService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AfipTokenService> _logger;
        private readonly ConcurrentDictionary<string, AfipToken> _tokens;
        private readonly SemaphoreSlim _semaphore;
        private readonly Timer _cleanupTimer;
        private readonly IAfipTokenRepository _tokenRepository;

        public event EventHandler<AfipTokenEventArgs>? TokenObtained;
        public event EventHandler<AfipTokenEventArgs>? TokenExpired;
        public event EventHandler<AfipTokenErrorEventArgs>? TokenError;

        public AfipTokenService(
            IAfipAuthService afipAuthService,
            IMemoryCache cache,
            ILogger<AfipTokenService> logger,
            IAfipTokenRepository tokenRepository)
        {
            _afipAuthService = afipAuthService;
            _cache = cache;
            _logger = logger;
            _tokenRepository = tokenRepository;
            _tokens = new ConcurrentDictionary<string, AfipToken>();
            _semaphore = new SemaphoreSlim(1, 1);

            // Timer para limpiar tokens expirados cada 5 minutos
            _cleanupTimer = new Timer(CleanupExpiredTokens, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            // Cargar tokens existentes desde la base de datos al inicializar
            _ = Task.Run(CargarTokensExistentesAsync);

            _logger.LogInformation("AfipTokenService inicializado con persistencia en BD");
        }

        /// <summary>
        /// Obtiene un token válido, lo regenera automáticamente si es necesario
        /// Primero busca en memoria, luego en BD, y finalmente genera uno nuevo
        /// </summary>
        public async Task<AfipToken> GetValidTokenAsync(string serviceId = "wsfe")
        {
            Console.WriteLine($"=== OBTENIENDO TOKEN VÁLIDO PARA {serviceId} ===");
            
            // 1. Verificar si ya tenemos un token válido en memoria
            if (_tokens.TryGetValue(serviceId, out var existingToken) && !existingToken.IsExpired)
            {
                Console.WriteLine($"✅ Token en memoria válido para {serviceId} (expira en {existingToken.TimeToExpiry.TotalMinutes:F1} minutos)");
                return existingToken;
            }

            // 2. Si no hay token en memoria o está expirado, buscar en BD
            var tokenFromDb = await _tokenRepository.GetValidTokenAsync(serviceId);
            if (tokenFromDb != null && !tokenFromDb.IsExpired)
            {
                Console.WriteLine($"✅ Token recuperado de BD para {serviceId} (expira en {tokenFromDb.TimeToExpiry.TotalMinutes:F1} minutos)");
                
                // Cargar en memoria para uso futuro
                _tokens.AddOrUpdate(serviceId, tokenFromDb, (key, oldValue) => tokenFromDb);
                
                return tokenFromDb;
            }

            // 3. Si el token está expirado, notificar
            if (existingToken != null && existingToken.IsExpired)
            {
                Console.WriteLine($"⚠️ Token para {serviceId} está expirado, regenerando...");
                TokenExpired?.Invoke(this, new AfipTokenEventArgs(existingToken, serviceId));
            }

            // 4. Si AFIP responde "CEE ya posee un TA válido", intentar esperar y buscar de nuevo
            try
            {
                return await RefreshTokenAsync(serviceId);
            }
            catch (AfipTokenAlreadyExistsException ex)
            {
                Console.WriteLine($"⚠️ AFIP indica que ya existe un token válido: {ex.Message}");
                Console.WriteLine("Esperando 30 segundos antes de buscar el token existente...");
                
                // Esperar un momento y buscar de nuevo en BD por si AFIP actualizó algo
                await Task.Delay(TimeSpan.FromSeconds(30));
                
                var retryToken = await _tokenRepository.GetValidTokenAsync(serviceId);
                if (retryToken != null && !retryToken.IsExpired)
                {
                    Console.WriteLine($"✅ Token encontrado después de espera para {serviceId}");
                    _tokens.AddOrUpdate(serviceId, retryToken, (key, oldValue) => retryToken);
                    return retryToken;
                }
                
                // Si aún no encontramos el token, relanzar la excepción
                throw;
            }
        }

        /// <summary>
        /// Fuerza la regeneración del token
        /// </summary>
        public async Task<AfipToken> RefreshTokenAsync(string serviceId = "wsfe")
        {
            Console.WriteLine($"=== REGENERANDO TOKEN PARA {serviceId} ===");

            // Usar semáforo para evitar múltiples regeneraciones simultáneas
            await _semaphore.WaitAsync();
            try
            {
                // Verificar otra vez por si otro hilo ya regeneró el token
                if (_tokens.TryGetValue(serviceId, out var recentToken) && !recentToken.IsExpired)
                {
                    Console.WriteLine($"✅ Otro hilo ya regeneró el token para {serviceId}");
                    return recentToken;
                }

                _logger.LogInformation("Obteniendo nuevo token AFIP para servicio: {ServiceId}", serviceId);

                try
                {
                    // Obtener nuevo token usando el servicio de autenticación
                    var result = await _afipAuthService.GetAuthenticationAsync(serviceId);

                    if (result.IsSuccess && result.Token != null)
                    {
                        var afipToken = AfipToken.FromWsaaResponse(result.Token, serviceId);
                        
                        // Guardar en memoria local
                        _tokens.AddOrUpdate(serviceId, afipToken, (key, oldValue) => afipToken);

                        // Guardar en base de datos para persistencia
                        await _tokenRepository.SaveTokenAsync(afipToken);

                        // Guardar en caché con expiración
                        var cacheKey = $"afip_token_{serviceId}";
                        var cacheExpiration = afipToken.ExpirationTime.AddMinutes(-2); // 2 minutos antes de expirar
                        _cache.Set(cacheKey, afipToken, cacheExpiration);

                        Console.WriteLine($"✅ Nuevo token obtenido para {serviceId}");
                        Console.WriteLine($"   Expira: {afipToken.ExpirationTime}");
                        Console.WriteLine($"   Token length: {afipToken.Token.Length}");
                        Console.WriteLine($"   Sign length: {afipToken.Sign.Length}");

                        // Notificar que se obtuvo un nuevo token
                        TokenObtained?.Invoke(this, new AfipTokenEventArgs(afipToken, serviceId));

                        _logger.LogInformation("Token AFIP obtenido exitosamente para {ServiceId}. Expira: {ExpirationTime}", 
                            serviceId, afipToken.ExpirationTime);

                        return afipToken;
                    }
                    else
                    {
                        var errorMsg = $"Error obteniendo token AFIP para {serviceId}: {result.Error?.Message}";
                        Console.WriteLine($"❌ {errorMsg}");
                        
                        // Verificar si el error es por token ya existente
                        if (result.Error?.Message?.Contains("CEE ya posee un TA valido") == true ||
                            result.Error?.FaultString?.Contains("CEE ya posee un TA valido") == true)
                        {
                            var existsException = new AfipTokenAlreadyExistsException(serviceId, result.Error.Message);
                            TokenError?.Invoke(this, new AfipTokenErrorEventArgs(serviceId, existsException, errorMsg));
                            throw existsException;
                        }
                        
                        var exception = new Exception(errorMsg);
                        TokenError?.Invoke(this, new AfipTokenErrorEventArgs(serviceId, exception, errorMsg));
                        
                        _logger.LogError("Error obteniendo token AFIP para {ServiceId}: {Error}", serviceId, result.Error?.Message);
                        throw exception;
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Excepción obteniendo token AFIP para {serviceId}: {ex.Message}";
                    Console.WriteLine($"❌ {errorMsg}");
                    
                    TokenError?.Invoke(this, new AfipTokenErrorEventArgs(serviceId, ex, errorMsg));
                    
                    _logger.LogError(ex, "Excepción obteniendo token AFIP para {ServiceId}", serviceId);
                    throw;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Obtiene el token actual sin regenerarlo
        /// </summary>
        public AfipToken? GetCurrentToken(string serviceId = "wsfe")
        {
            _tokens.TryGetValue(serviceId, out var token);
            return token;
        }

        /// <summary>
        /// Verifica si existe un token válido
        /// </summary>
        public bool HasValidToken(string serviceId = "wsfe")
        {
            return _tokens.TryGetValue(serviceId, out var token) && !token.IsExpired;
        }

        /// <summary>
        /// Limpia tokens expirados del diccionario
        /// </summary>
        private void CleanupExpiredTokens(object? state)
        {
            try
            {
                var expiredKeys = new List<string>();
                
                foreach (var kvp in _tokens)
                {
                    if (kvp.Value.IsExpired)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    if (_tokens.TryRemove(key, out var expiredToken))
                    {
                        Console.WriteLine($"🗑️ Token expirado removido para servicio: {key}");
                        _logger.LogInformation("Token expirado removido para servicio: {ServiceId}", key);
                    }
                }

                if (expiredKeys.Count > 0)
                {
                    Console.WriteLine($"Cleanup completado: {expiredKeys.Count} tokens expirados removidos");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante cleanup de tokens expirados");
            }
        }

        /// <summary>
        /// Obtiene información de todos los tokens actuales (para debugging/monitoring)
        /// </summary>
        public Dictionary<string, object> GetTokensStatus()
        {
            var status = new Dictionary<string, object>();
            
            foreach (var kvp in _tokens)
            {
                status[kvp.Key] = new
                {
                    ServiceId = kvp.Value.ServiceId,
                    IsExpired = kvp.Value.IsExpired,
                    ExpirationTime = kvp.Value.ExpirationTime,
                    TimeToExpiry = kvp.Value.TimeToExpiry,
                    ObtainedAt = kvp.Value.ObtainedAt,
                    TokenLength = kvp.Value.Token.Length,
                    SignLength = kvp.Value.Sign.Length
                };
            }

            return status;
        }

        /// <summary>
        /// Carga tokens existentes desde la base de datos al inicializar el servicio
        /// </summary>
        private async Task CargarTokensExistentesAsync()
        {
            try
            {
                _logger.LogInformation("Cargando tokens existentes desde BD...");
                
                var tokensExistentes = await _tokenRepository.GetAllValidTokensAsync();
                
                foreach (var token in tokensExistentes)
                {
                    if (!token.IsExpired)
                    {
                        _tokens.AddOrUpdate(token.ServiceId, token, (key, oldValue) => token);
                        Console.WriteLine($"✅ Token cargado desde BD: {token.ServiceId} (expira en {token.TimeToExpiry.TotalMinutes:F1} minutos)");
                    }
                    else
                    {
                        // Limpiar tokens expirados de la BD
                        await _tokenRepository.DeleteTokenAsync(token.ServiceId);
                        Console.WriteLine($"🗑️ Token expirado eliminado de BD: {token.ServiceId}");
                    }
                }
                
                _logger.LogInformation("Tokens cargados desde BD: {Count} válidos", _tokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando tokens existentes desde BD");
            }
        }

        /// <summary>
        /// Intenta recuperar un token válido desde AFIP cuando ya existe uno
        /// </summary>
        public async Task<AfipToken?> IntentarRecuperarTokenExistenteAsync(string serviceId)
        {
            try
            {
                Console.WriteLine($"=== INTENTANDO RECUPERAR TOKEN EXISTENTE PARA {serviceId} ===");
                
                // Buscar primero en BD por si hay uno que no esté en memoria
                var tokenFromDb = await _tokenRepository.GetValidTokenAsync(serviceId);
                if (tokenFromDb != null && !tokenFromDb.IsExpired)
                {
                    Console.WriteLine($"✅ Token existente encontrado en BD para {serviceId}");
                    _tokens.AddOrUpdate(serviceId, tokenFromDb, (key, oldValue) => tokenFromDb);
                    return tokenFromDb;
                }
                
                // Si no encontramos nada válido, devolver null
                Console.WriteLine($"❌ No se encontró token válido existente para {serviceId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error intentando recuperar token existente para {ServiceId}", serviceId);
                return null;
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _semaphore?.Dispose();
            _logger.LogInformation("AfipTokenService disposed");
        }
    }
}
