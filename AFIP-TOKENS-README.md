# Sistema de Tokens AFIP - Manejo de "CEE ya posee un TA válido"

## Problema

Cuando AFIP responde con el error **"CEE ya posee un TA válido para el acceso al WSN solicitado"**, significa que ya existe un Token de Acceso (TA) válido para tu CUIT y el servicio web solicitado. Esto es común cuando:

1. La aplicación se reinicia pero el token sigue válido en AFIP
2. Se intenta generar un nuevo token cuando ya existe uno válido
3. Hay problemas de sincronización entre la aplicación y AFIP

## Solución Implementada

### 🔧 **Persistencia en Base de Datos**

Los tokens ahora se guardan en la tabla `AfipTokens` con la siguiente información:
- `ServiceId`: ID del servicio (wsfe, ws_sr_constancia_inscripcion, etc.)
- `Token`: Token de acceso
- `Sign`: Firma del token
- `ExpirationTime`: Fecha de expiración
- `ObtainedAt`: Cuándo se obtuvo el token
- `RawXml`: XML completo del Login Ticket Response

### 🔄 **Proceso Automático de Recuperación**

El sistema maneja automáticamente la recuperación de tokens siguiendo estos pasos:

1. **Verificar memoria**: Busca tokens válidos en la memoria de la aplicación
2. **Buscar en BD**: Si no hay en memoria, busca en la base de datos
3. **Cargar al iniciar**: Al iniciar la aplicación, carga automáticamente todos los tokens válidos desde BD
4. **Generar nuevo**: Solo si no encuentra ningún token válido, intenta generar uno nuevo
5. **Manejo del error**: Si AFIP responde "TA válido", espera 30 segundos y busca de nuevo

## Páginas de la Aplicación

### 📊 **Dashboard AFIP** (`/afip-dashboard`)
- Monitoreo en tiempo real de tokens
- Estado de todos los servicios AFIP
- Ejemplos de uso programático
- Log de eventos

### 🔄 **Recuperación de Tokens** (`/afip-token-recovery`)
- Demostración paso a paso del proceso
- Herramientas para buscar tokens existentes
- Simulación del manejo de errores
- Guía de resolución de problemas

### 📝 **Tipos Comprobantes** (`/tipo-comprobantes`)
- Página original mejorada con el nuevo sistema
- Botones para obtener tokens AFIP
- Muestra ejemplos de errores y respuestas

### 🧾 **Facturas Tipo C** (`/facturas-tipo-c`)
- Generación de facturas electrónicas
- Usa automáticamente el sistema de tokens global

## Uso Programático

### Obtener Token Válido (Recomendado)
```csharp
@inject IAfipTokenService AfipTokenService

// Obtiene un token válido, lo regenera automáticamente si es necesario
var token = await AfipTokenService.GetValidTokenAsync("wsfe");

// El token siempre será válido (no expirado)
Console.WriteLine($"Token: {token.Token}");
Console.WriteLine($"Expira: {token.ExpirationTime}");
```

### Verificar Token Actual
```csharp
// Obtener token actual sin regenerar
var currentToken = AfipTokenService.GetCurrentToken("wsfe");

// Verificar si existe un token válido
bool hasValidToken = AfipTokenService.HasValidToken("wsfe");
```

### Recuperar Token Existente
```csharp
// Intenta recuperar un token válido existente (útil para el error "TA válido")
var existingToken = await AfipTokenService.IntentarRecuperarTokenExistenteAsync("wsfe");
```

### Eventos en Tiempo Real
```csharp
// Suscribirse a eventos del servicio
AfipTokenService.TokenObtained += (sender, e) => {
    Console.WriteLine($"Nuevo token obtenido para {e.ServiceId}");
};

AfipTokenService.TokenExpired += (sender, e) => {
    Console.WriteLine($"Token expirado para {e.ServiceId}");
};

AfipTokenService.TokenError += (sender, e) => {
    Console.WriteLine($"Error en token {e.ServiceId}: {e.ErrorMessage}");
};
```

## Configuración

### appsettings.json
```json
{
  "Afip": {
    "CertificatePath": "W:\\cert.p12",
    "CertificatePassword": "261194",
    "IsProduction": false,
    "UsePowerShell": true,
    "PowerShellScriptPath": "path\\to\\wsaa-cliente-noopenssl.ps1",
    "CuitEmisor": 20000000001
  }
}
```

## Resolución de Problemas

### ❌ "CEE ya posee un TA válido"

**Opción 1: Automática**
- El sistema maneja esto automáticamente
- Espera 30 segundos y busca el token existente
- Si lo encuentra, lo carga en memoria

**Opción 2: Manual**
1. Ve a **Dashboard AFIP** (`/afip-dashboard`)
2. Haz clic en **"Buscar Existente"**
3. El sistema recuperará el token de la BD

**Opción 3: Página de recuperación**
1. Ve a **Recuperación de Tokens** (`/afip-token-recovery`)
2. Sigue el proceso paso a paso
3. Usa las herramientas de demostración

### 🔄 Reinicio de Aplicación

Cuando la aplicación se reinicia:
1. **Automáticamente** carga todos los tokens válidos desde BD
2. Los tokens están disponibles inmediatamente
3. **No es necesario** generar nuevos tokens si ya existen válidos

### 🗑️ Limpiar Tokens Expirados

```csharp
// Los tokens expirados se limpian automáticamente cada 5 minutos
// También se pueden limpiar manualmente desde el Dashboard AFIP
```

## Beneficios

✅ **Sin pérdida de tokens** después de reinicios
✅ **Manejo automático** del error "TA válido"
✅ **Recuperación inteligente** de tokens existentes
✅ **Monitoreo en tiempo real** del estado de tokens
✅ **Thread-safe** para aplicaciones con múltiples usuarios
✅ **Logging completo** para depuración

## Estructura de Archivos

```
Services/Afip/
├── IAfipTokenService.cs          # Interfaz del servicio de tokens
├── AfipTokenService.cs           # Implementación con persistencia
├── IAfipTokenRepository.cs       # Interfaz del repositorio
├── AfipTokenRepository.cs        # Implementación con Entity Framework
├── AfipAuthService.cs           # Servicio de autenticación (mejorado)
└── WSFEv1/                      # Servicios de facturación electrónica

Entities/Afip/
└── AfipTokenEntity.cs           # Entidad para base de datos

Components/
├── Shared/
│   ├── AfipTokenStatus.razor    # Componente de estado de token
│   └── AfipTokenManager.razor   # Componente de gestión de tokens
└── Pages/
    ├── AfipDashboard.razor      # Dashboard principal
    ├── AfipTokenRecovery.razor  # Página de recuperación
    ├── TiposComprobantes.razor  # Página mejorada
    └── FacturasTipoC.razor      # Generación de facturas
```

## Base de Datos

### Migración Aplicada
```bash
dotnet ef migrations add AddAfipTokensTable
dotnet ef database update
```

### Tabla AfipTokens
- **PK**: ServiceId (string)
- Token, Sign (string, 2000 chars)
- ExpirationTime, ObtainedAt (DateTime UTC)
- RawXml (string, 4000 chars)
- CreatedAt, UpdatedAt (DateTime UTC)

Este sistema resuelve completamente el problema de "CEE ya posee un TA válido" y proporciona una experiencia fluida para trabajar con tokens AFIP.
