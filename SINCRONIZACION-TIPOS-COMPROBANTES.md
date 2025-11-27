# Sincronización de Tipos de Comprobantes desde AFIP

## Descripción
Este módulo implementa la sincronización automática de tipos de comprobantes desde AFIP a la base de datos local.

## Uso

### Desde la interfaz web
1. Navegar a `/tipo-comprobantes`
2. Hacer clic en el botón **"Sincronizar desde AFIP"**
3. El sistema descargará automáticamente todos los tipos de comprobantes disponibles

### Desde código
```csharp
public class MiServicio
{
    private readonly ITipoComprobanteAppService _tipoComprobanteService;

    public MiServicio(ITipoComprobanteAppService tipoComprobanteService)
    {
        _tipoComprobanteService = tipoComprobanteService;
    }

    public async Task SincronizarTiposAsync()
    {
        var resultado = await _tipoComprobanteService.SincronizarDesdeAfipAsync();
        
        if (resultado.Exitoso)
        {
            Console.WriteLine($"✅ Sincronización exitosa");
            Console.WriteLine($"   Total: {resultado.TotalObtenidos}");
            Console.WriteLine($"   Insertados: {resultado.Insertados}");
            Console.WriteLine($"   Actualizados: {resultado.Actualizados}");
        }
        else
        {
            Console.WriteLine($"❌ Error: {resultado.Mensaje}");
        }
    }
}
```

## Estructura de Datos

### TipoComprobante (Entidad)
- `Id` (int): Identificador interno auto-incremental
- `CodigoAfip` (int): Código del tipo de comprobante en AFIP (único)
- `Nombre` (string): Descripción del tipo de comprobante
- `Abreviatura` (string): Letra o código corto (ej: "A", "B", "C")
- `FechaDesde` (DateOnly?): Fecha desde la cual está vigente
- `FechaHasta` (DateOnly?): Fecha hasta la cual está vigente
- `EsFiscal` (bool): Indica si es un comprobante fiscal
- `Enabled` (bool): Indica si está habilitado

### SincronizacionTiposComprobanteResult
- `Exitoso` (bool): Indica si la sincronización fue exitosa
- `TotalObtenidos` (int): Total de tipos obtenidos desde AFIP
- `Insertados` (int): Cantidad de nuevos registros insertados
- `Actualizados` (int): Cantidad de registros actualizados
- `Mensaje` (string): Mensaje descriptivo del resultado
- `Errores` (List<string>): Lista de errores si los hay

## Lógica de Sincronización

1. **Obtención de token AFIP**: Se obtiene automáticamente un token válido
2. **Consulta a AFIP**: Se llama al método `FEParamGetTiposCbte`
3. **Comparación**: Se compara cada tipo de AFIP con la BD local usando `CodigoAfip`
4. **Actualización/Inserción**:
   - Si existe: actualiza nombre y fechas
   - Si no existe: inserta nuevo registro
5. **Resultado**: Retorna estadísticas de la operación

## Migración de Base de Datos

La migración `20251127000000_AddCodigoAfipToTiposComprobantes` realiza:
- Elimina columna `Descripcion` (obsoleta)
- Agrega columna `CodigoAfip` (int, obligatorio)
- Agrega columnas `FechaDesde` y `FechaHasta` (date, nullable)
- Crea índice único en `CodigoAfip`

Para aplicar:
```bash
dotnet ef database update
```

## Arquitectura

```
┌─────────────────────────────────────────────┐
│  TiposComprobantes.razor (Presentación)    │
│  - Botón "Sincronizar desde AFIP"          │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│  ITipoComprobanteAppService (Interfaz)     │
│  + SincronizarDesdeAfipAsync()             │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│  TipoComprobanteAppService (Implementación)│
│  - Inyecta IFacturacionElectronicaService  │
│  - Inyecta IRepository<TipoComprobante>    │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│  IFacturacionElectronicaService            │
│  + ObtenerTiposComprobanteAsync()          │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│  WsfEv1Client (Cliente SOAP AFIP)          │
│  - Llama a FEParamGetTiposCbte             │
└─────────────────────────────────────────────┘
```

## Notas Importantes

- ✅ **Idempotente**: Puede ejecutarse múltiples veces sin duplicar datos
- ✅ **Thread-safe**: ABP maneja la concurrencia automáticamente
- ✅ **Auditable**: Registra quién y cuándo creó/modificó cada registro
- ✅ **Logging**: Todos los pasos se registran en la consola
- ✅ **Manejo de errores**: Los errores individuales no detienen el proceso

## Fecha de Implementación
27 de noviembre de 2025

## Autor
Implementado siguiendo la arquitectura ABP Framework

