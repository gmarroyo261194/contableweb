namespace ContableWeb.Services.Dtos.TiposDocumentos;

/// <summary>
/// Resultado de la sincronización de tipos de documentos desde AFIP
/// </summary>
public class SincronizacionTiposDocumentoResult
{
    public bool Exitoso { get; set; }
    public int TotalObtenidos { get; set; }
    public int Insertados { get; set; }
    public int Actualizados { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<string> Errores { get; set; } = new();
}

