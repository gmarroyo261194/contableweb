using ContableWeb.Data;
using ContableWeb.Entities.Afip;
using Microsoft.EntityFrameworkCore;

namespace ContableWeb.Services.Afip
{
    /// <summary>
    /// Repositorio para persistir tokens AFIP en base de datos
    /// </summary>
    public interface IAfipTokenRepository
    {
        /// <summary>
        /// Guarda un token en la base de datos
        /// </summary>
        Task SaveTokenAsync(AfipToken token);

        /// <summary>
        /// Obtiene un token válido desde la base de datos
        /// </summary>
        Task<AfipToken?> GetValidTokenAsync(string serviceId);

        /// <summary>
        /// Obtiene todos los tokens válidos
        /// </summary>
        Task<List<AfipToken>> GetAllValidTokensAsync();

        /// <summary>
        /// Elimina un token de la base de datos
        /// </summary>
        Task DeleteTokenAsync(string serviceId);

        /// <summary>
        /// Limpia todos los tokens expirados
        /// </summary>
        Task CleanupExpiredTokensAsync();
    }

    /// <summary>
    /// Implementación del repositorio de tokens AFIP usando Entity Framework
    /// </summary>
    public class AfipTokenRepository : IAfipTokenRepository
    {
        private readonly ContableWebDbContext _dbContext;
        private readonly ILogger<AfipTokenRepository> _logger;

        public AfipTokenRepository(ContableWebDbContext dbContext, ILogger<AfipTokenRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Guarda un token en la base de datos
        /// </summary>
        public async Task SaveTokenAsync(AfipToken token)
        {
            try
            {
                var entity = await _dbContext.AfipTokens
                    .FirstOrDefaultAsync(t => t.ServiceId == token.ServiceId);

                if (entity != null)
                {
                    // Actualizar token existente
                    entity.Token = token.Token;
                    entity.Sign = token.Sign;
                    entity.ExpirationTime = token.ExpirationTime;
                    entity.ObtainedAt = token.ObtainedAt;
                    entity.RawXml = token.RawXml;
                    
                    _logger.LogInformation("Token AFIP actualizado en BD para servicio: {ServiceId}", token.ServiceId);
                }
                else
                {
                    // Crear nuevo token
                    entity = new AfipTokenEntity
                    {
                        ServiceId = token.ServiceId,
                        Token = token.Token,
                        Sign = token.Sign,
                        ExpirationTime = token.ExpirationTime,
                        ObtainedAt = token.ObtainedAt,
                        RawXml = token.RawXml
                    };
                    
                    _dbContext.AfipTokens.Add(entity);
                    _logger.LogInformation("Nuevo token AFIP guardado en BD para servicio: {ServiceId}", token.ServiceId);
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando token AFIP en BD para servicio: {ServiceId}", token.ServiceId);
                throw;
            }
        }

        /// <summary>
        /// Obtiene un token válido desde la base de datos
        /// </summary>
        public async Task<AfipToken?> GetValidTokenAsync(string serviceId)
        {
            try
            {
                var entity = await _dbContext.AfipTokens
                    .Where(t => t.ServiceId == serviceId && t.ExpirationTime > DateTime.UtcNow)
                    .OrderByDescending(t => t.ExpirationTime)
                    .FirstOrDefaultAsync();

                if (entity != null)
                {
                    return new AfipToken
                    {
                        ServiceId = entity.ServiceId,
                        Token = entity.Token,
                        Sign = entity.Sign,
                        ExpirationTime = entity.ExpirationTime,
                        ObtainedAt = entity.ObtainedAt,
                        RawXml = entity.RawXml
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo token AFIP desde BD para servicio: {ServiceId}", serviceId);
                return null;
            }
        }

        /// <summary>
        /// Obtiene todos los tokens válidos
        /// </summary>
        public async Task<List<AfipToken>> GetAllValidTokensAsync()
        {
            try
            {
                var entities = await _dbContext.AfipTokens
                    .Where(t => t.ExpirationTime > DateTime.UtcNow)
                    .ToListAsync();

                return entities.Select(e => new AfipToken
                {
                    ServiceId = e.ServiceId,
                    Token = e.Token,
                    Sign = e.Sign,
                    ExpirationTime = e.ExpirationTime,
                    ObtainedAt = e.ObtainedAt,
                    RawXml = e.RawXml
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo todos los tokens AFIP desde BD");
                return new List<AfipToken>();
            }
        }

        /// <summary>
        /// Elimina un token de la base de datos
        /// </summary>
        public async Task DeleteTokenAsync(string serviceId)
        {
            try
            {
                var entity = await _dbContext.AfipTokens
                    .FirstOrDefaultAsync(t => t.ServiceId == serviceId);

                if (entity != null)
                {
                    _dbContext.AfipTokens.Remove(entity);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Token AFIP eliminado de BD para servicio: {ServiceId}", serviceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando token AFIP de BD para servicio: {ServiceId}", serviceId);
                throw;
            }
        }

        /// <summary>
        /// Limpia todos los tokens expirados
        /// </summary>
        public async Task CleanupExpiredTokensAsync()
        {
            try
            {
                var expiredTokens = await _dbContext.AfipTokens
                    .Where(t => t.ExpirationTime <= DateTime.UtcNow)
                    .ToListAsync();

                if (expiredTokens.Any())
                {
                    _dbContext.AfipTokens.RemoveRange(expiredTokens);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Limpieza de tokens expirados: {Count} tokens eliminados", expiredTokens.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error limpiando tokens expirados de BD");
                throw;
            }
        }
    }

    /// <summary>
    /// Excepción específica para cuando AFIP indica que ya existe un token válido
    /// </summary>
    public class AfipTokenAlreadyExistsException : Exception
    {
        public string ServiceId { get; }

        public AfipTokenAlreadyExistsException(string serviceId, string message) 
            : base($"AFIP indica que ya existe un token válido para el servicio '{serviceId}': {message}")
        {
            ServiceId = serviceId;
        }
    }
}
