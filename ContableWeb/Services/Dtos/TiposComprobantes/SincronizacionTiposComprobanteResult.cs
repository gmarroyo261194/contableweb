namespace ContableWeb.Services.Dtos.TiposComprobantes;

/// <summary>
/// Resultado de la sincronización de tipos de comprobantes desde AFIP
/// </summary>
public class SincronizacionTiposComprobanteResult
{
    public bool Exitoso { get; set; }
    public int TotalObtenidos { get; set; }
    public int Insertados { get; set; }
    public int Actualizados { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<string> Errores { get; set; } = new();
}

