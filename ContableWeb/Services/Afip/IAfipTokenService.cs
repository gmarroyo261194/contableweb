using System.ComponentModel;

namespace ContableWeb.Services.Afip
{
    /// <summary>
    /// Interfaz para el servicio de token AFIP global
    /// </summary>
    public interface IAfipTokenService
    {
        /// <summary>
        /// Obtiene el token AFIP válido (lo regenera automáticamente si no existe o está expirado)
        /// </summary>
        /// <param name="serviceId">ID del servicio AFIP (por defecto "wsfe")</param>
        /// <returns>Token AFIP válido</returns>
        Task<AfipToken> GetValidTokenAsync(string serviceId = "wsfe");

        /// <summary>
        /// Fuerza la regeneración del token
        /// </summary>
        /// <param name="serviceId">ID del servicio AFIP</param>
        /// <returns>Nuevo token AFIP</returns>
        Task<AfipToken> RefreshTokenAsync(string serviceId = "wsfe");

        /// <summary>
        /// Obtiene el token actual sin regenerarlo (puede estar expirado o no existir)
        /// </summary>
        /// <param name="serviceId">ID del servicio AFIP</param>
        /// <returns>Token actual o null si no existe</returns>
        AfipToken? GetCurrentToken(string serviceId = "wsfe");

        /// <summary>
        /// Verifica si existe un token válido
        /// </summary>
        /// <param name="serviceId">ID del servicio AFIP</param>
        /// <returns>True si existe un token válido</returns>
        bool HasValidToken(string serviceId = "wsfe");

        /// <summary>
        /// Evento que se dispara cuando se obtiene un nuevo token
        /// </summary>
        event EventHandler<AfipTokenEventArgs>? TokenObtained;

        /// <summary>
        /// Evento que se dispara cuando el token expira
        /// </summary>
        event EventHandler<AfipTokenEventArgs>? TokenExpired;

        /// <summary>
        /// Evento que se dispara cuando hay un error obteniendo el token
        /// </summary>
        event EventHandler<AfipTokenErrorEventArgs>? TokenError;
    }

    /// <summary>
    /// Token AFIP con información completa
    /// </summary>
    public class AfipToken
    {
        public string ServiceId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Sign { get; set; } = string.Empty;
        public DateTime ExpirationTime { get; set; }
        public DateTime ObtainedAt { get; set; }
        public string RawXml { get; set; } = string.Empty;
        public bool IsExpired => DateTime.UtcNow >= ExpirationTime;
        public TimeSpan TimeToExpiry => ExpirationTime - DateTime.UtcNow;
        
        /// <summary>
        /// Crea un AfipToken desde WsaaResponse
        /// </summary>
        public static AfipToken FromWsaaResponse(WsaaResponse response, string serviceId)
        {
            return new AfipToken
            {
                ServiceId = serviceId,
                Token = response.Token,
                Sign = response.Sign,
                ExpirationTime = response.ExpirationTime,
                ObtainedAt = DateTime.UtcNow,
                RawXml = response.RawXml
            };
        }
    }

    /// <summary>
    /// Argumentos del evento de token
    /// </summary>
    public class AfipTokenEventArgs : EventArgs
    {
        public AfipToken Token { get; }
        public string ServiceId { get; }

        public AfipTokenEventArgs(AfipToken token, string serviceId)
        {
            Token = token;
            ServiceId = serviceId;
        }
    }

    /// <summary>
    /// Argumentos del evento de error de token
    /// </summary>
    public class AfipTokenErrorEventArgs : EventArgs
    {
        public string ServiceId { get; }
        public Exception Exception { get; }
        public string ErrorMessage { get; }

        public AfipTokenErrorEventArgs(string serviceId, Exception exception, string errorMessage)
        {
            ServiceId = serviceId;
            Exception = exception;
            ErrorMessage = errorMessage;
        }
    }
}
