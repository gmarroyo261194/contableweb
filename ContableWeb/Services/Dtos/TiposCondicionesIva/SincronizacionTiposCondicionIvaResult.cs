namespace ContableWeb.Services.Dtos.TiposCondicionesIva;

/// <summary>
/// Resultado de la sincronización de tipos de condiciones IVA desde AFIP
/// </summary>
public class SincronizacionTiposCondicionIvaResult
{
    public bool Exitoso { get; set; }
    public int TotalObtenidos { get; set; }
    public int Insertados { get; set; }
    public int Actualizados { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<string> Errores { get; set; } = new();
}

