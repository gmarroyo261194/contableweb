## Corrección del Bucle Infinito en WSFEv1Client

### 🔍 Problema Identificado
El método `SerializeObject` en `WSFEv1Client.cs` estaba causando un bucle infinito debido a:

1. **Referencias circulares**: Los objetos se referenciaban entre sí causando bucles
2. **Recursión sin límite**: No había protección contra profundidad excesiva
3. **Serialización genérica problemática**: El método intentaba serializar cualquier objeto de forma genérica

### ✅ Solución Implementada

#### **1. Serialización Específica por Tipo**
Reemplazé la serialización genérica con métodos específicos para cada tipo de objeto AFIP:

- `SerializeFEAuthRequest()` - Para autenticación
- `SerializeFECAERequest()` - Para solicitudes de CAE
- `SerializeFECAEDetRequest()` - Para detalles de comprobantes
- `SerializeFERecuperaLastCbteRequest()` - Para consultas

#### **2. Protecciones Implementadas**
- **Límite de profundidad**: Máximo 10 niveles de recursión
- **Detección de referencias circulares**: `HashSet<object>` para rastrear objetos visitados
- **Manejo de errores**: Try-catch para propiedades problemáticas
- **Logging detallado**: Para depuración del proceso de serialización

#### **3. Validación de Datos**
- **Escape de XML**: Todos los strings se escapan correctamente
- **Formato decimal**: Números con formato `F2` (2 decimales)
- **Manejo de nulos**: Verificación de valores nulos antes de serializar

### 🚀 Beneficios
- ✅ **No más bucles infinitos**
- ✅ **Serialización correcta** de estructuras AFIP
- ✅ **XML válido** para el servicio WSFEv1
- ✅ **Logging detallado** para depuración
- ✅ **Manejo robusto de errores**

### 🧪 Cómo Probar
1. Ve a `/facturas-tipo-c`
2. Completa los datos de una factura
3. Haz clic en "Generar Factura"
4. **Ya no debería haber bucle infinito**
5. Revisa la consola para ver el progreso de la serialización

La solución está completa y lista para generar facturas tipo C sin problemas de bucle infinito.
