using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContableWeb.Entities.Afip
{
    /// <summary>
    /// Entidad para persistir tokens AFIP en base de datos
    /// </summary>
    [Table("AfipTokens")]
    public class AfipTokenEntity
    {
        /// <summary>
        /// ID del servicio AFIP (wsfe, ws_sr_constancia_inscripcion, etc.)
        /// </summary>
        [Key]
        [Required]
        [StringLength(50)]
        public string ServiceId { get; set; } = string.Empty;

        /// <summary>
        /// Token de acceso de AFIP
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Firma del token
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string Sign { get; set; } = string.Empty;

        /// <summary>
        /// Fecha y hora de expiración del token (UTC)
        /// </summary>
        [Required]
        public DateTime ExpirationTime { get; set; }

        /// <summary>
        /// Fecha y hora cuando se obtuvo el token (UTC)
        /// </summary>
        [Required]
        public DateTime ObtainedAt { get; set; }

        /// <summary>
        /// XML completo del Login Ticket Response
        /// </summary>
        [StringLength(4000)]
        public string RawXml { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indica si el token está expirado
        /// </summary>
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ExpirationTime;

        /// <summary>
        /// Tiempo restante hasta la expiración
        /// </summary>
        [NotMapped]
        public TimeSpan TimeToExpiry => ExpirationTime - DateTime.UtcNow;
    }
}
