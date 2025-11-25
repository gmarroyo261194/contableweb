# Solución Error 10246 - Campo Condición Frente al IVA Obligatorio

## 🔍 **Error Identificado**
```xml
<Observaciones>
    <Obs>
        <Code>10246</Code>
        <Msg>Campo Condicion Frente al IVA del receptor es obligatorio conforme a lo reglamentado por la Resolucion General Nro 5616. Para mas informacion consular metodo FEParamGetCondicionIvaReceptor</Msg>
    </Obs>
</Observaciones>
```

**Causa:** AFIP requiere el campo **`CondicionFrenteIva`** obligatorio según la **Resolución General 5616**, incluso para facturas tipo C.

## ✅ **Solución Completa Implementada**

### 1. **Campo Obligatorio Agregado al Modelo**
```csharp
// En FECAEDetRequest
/// <summary>
/// Condición frente al IVA del receptor (obligatorio según RG 5616)
/// 1=IVA Responsable Inscripto, 4=IVA Sujeto Exento, 5=Consumidor Final, etc.
/// </summary>
[XmlElement("CondicionFrenteIva")]
public int CondicionFrenteIva { get; set; }
```

### 2. **Datos del Cliente Actualizados**
```csharp
// En DatosCliente - Campos obligatorios según RG 5616
public int TipoDocumento { get; set; } = TiposDocumento.ConsumidorFinal; // 99 por defecto
public long NumeroDocumento { get; set; } = 0; // 0 para consumidor final
public int CondicionIva { get; set; } = ConsumidorFinal; // 5 = Consumidor Final (por defecto)
```

### 3. **Constantes para Condiciones IVA**
```csharp
public static class CondicionesIVA
{
    public const int ResponsableInscripto = 1;         // IVA Responsable Inscripto
    public const int ResponsableNoInscripto = 2;       // IVA Responsable no Inscripto
    public const int NoResponsable = 3;                // IVA no Responsable
    public const int SujetoExento = 4;                 // IVA Sujeto Exento
    public const int ConsumidorFinal = 5;              // Consumidor Final ⭐
    public const int ResponsableMonotributo = 6;       // Responsable Monotributo
    public const int SujetoNoCategorizado = 7;         // Sujeto no Categorizado
    public const int ProveedorExterior = 8;            // Proveedor del Exterior
    public const int ClienteExterior = 9;              // Cliente del Exterior
    public const int IvaLiberado = 10;                 // IVA Liberado - Ley Nº 19.640
    // ... y más constantes
}
```

### 4. **Método para Obtener Condiciones IVA desde AFIP**
```csharp
/// <summary>
/// Obtiene las condiciones frente al IVA disponibles desde AFIP
/// </summary>
public async Task<List<CondicionIvaInfo>> ObtenerCondicionesIvaAsync()
{
    var token = await _tokenService.GetValidTokenAsync("wsfe");
    var auth = new FEAuthRequest { Token = token.Token, Sign = token.Sign, Cuit = _cuitEmisor };
    
    var response = await _wsfeClient.ObtenerCondicionesIvaAsync(auth);
    
    if (response.ResultGet != null)
    {
        return response.ResultGet.Select(ci => new CondicionIvaInfo
        {
            Id = ci.Id,
            Descripcion = ci.Desc
        }).ToList();
    }
    
    return new List<CondicionIvaInfo>();
}
```

### 5. **Cliente WSFEv1 Actualizado**
```csharp
// Método agregado al WSFEv1Client
public async Task<CondicionIvaResponse> ObtenerCondicionesIvaAsync(FEAuthRequest auth)
{
    var request = new { Auth = auth };
    var soapRequest = CreateSoapEnvelope("FEParamGetCondicionIvaReceptor", request);
    
    var content = new StringContent(soapRequest, System.Text.Encoding.UTF8, "text/xml");
    content.Headers.Add("SOAPAction", "http://ar.gov.afip.dif.FEV1/FEParamGetCondicionIvaReceptor");
    
    var response = await _httpClient.PostAsync(_serviceUrl, content);
    var responseContent = await response.Content.ReadAsStringAsync();
    
    return ParseSoapResponse<CondicionIvaResponse>(responseContent, "FEParamGetCondicionIvaReceptorResponse");
}
```

### 6. **Serialización XML Actualizada**
```csharp
// En SerializeFECAEDetRequest - Campo agregado al final
result += $"<CondicionFrenteIva>{det.CondicionFrenteIva}</CondicionFrenteIva>";
```

### 7. **Request Factura Tipo C Completo**
```csharp
new FECAEDetRequest
{
    Concepto = datos.EsServicio ? ConceptosComprobante.Servicios : ConceptosComprobante.Productos,
    DocTipo = datos.Cliente.TipoDocumento,          // Tipo documento del cliente
    DocNro = datos.Cliente.NumeroDocumento,         // Número documento del cliente
    CbteDesde = numeroComprobante,
    CbteHasta = numeroComprobante,
    CbteFch = DateTime.Now.ToString("yyyyMMdd"),
    ImpTotal = importeTotal,
    ImpTotConc = 0,
    ImpNeto = importeTotal,                         // En factura C, neto = total
    ImpOpEx = 0,
    ImpIVA = 0,                                     // Factura C NO discrimina IVA
    ImpTrib = 0,
    MonId = "PES",
    MonCotiz = 1,
    FchServDesde = datos.EsServicio ? datos.FechaServicioDesde?.ToString("yyyyMMdd") : null,
    FchServHasta = datos.EsServicio ? datos.FechaServicioHasta?.ToString("yyyyMMdd") : null,
    FchVtoPago = datos.FechaVencimiento?.ToString("yyyyMMdd"),
    Iva = null,                                     // Sin discriminación IVA
    CondicionFrenteIva = datos.Cliente.CondicionIva // ⭐ OBLIGATORIO según RG 5616
}
```

## 📊 **Valores Más Comunes para Facturas Tipo C**

| Código | Descripción | Uso Típico |
|--------|-------------|------------|
| **5** | **Consumidor Final** | **⭐ Facturas C por defecto** |
| 6 | Responsable Monotributo | Facturas C a monotributistas |
| 4 | IVA Sujeto Exento | Entidades exentas |
| 7 | Sujeto no Categorizado | Sin categorización fiscal |

## 🎯 **Configuración para Facturas Tipo C**

### **Para Consumidor Final (más común):**
```csharp
var cliente = new DatosCliente
{
    Nombre = "Juan Pérez",
    TipoDocumento = TiposDocumento.ConsumidorFinal,  // 99
    NumeroDocumento = 0,                             // 0 para consumidor final
    CondicionIva = ConsumidorFinal                   // 5 = Consumidor Final
};
```

### **Para Cliente con DNI:**
```csharp
var cliente = new DatosCliente
{
    Nombre = "María González",
    TipoDocumento = TiposDocumento.DNI,              // 96
    NumeroDocumento = 12345678,                      // DNI real
    CondicionIva = ConsumidorFinal                   // 5 = Consumidor Final
};
```

### **Para Monotributista:**
```csharp
var cliente = new DatosCliente
{
    Nombre = "Carlos López",
    TipoDocumento = TiposDocumento.CUIT,             // 80
    NumeroDocumento = 20123456789,                   // CUIT del monotributista
    CondicionIva = ResponsableMonotributo            // 6 = Monotributo
};
```

## ✅ **Resultado**

- ❌ **Error anterior**: `Obs 10246: Campo Condicion Frente al IVA del receptor es obligatorio`
- ✅ **Ahora**: Campo `CondicionFrenteIva` incluido en todas las facturas
- ✅ **Configuración flexible**: Soporte para diferentes tipos de clientes
- ✅ **Valores por defecto**: Consumidor Final (5) para facturas C típicas
- ✅ **Constantes tipadas**: Evita errores de valores incorrectos
- ✅ **Método AFIP**: Para obtener todas las condiciones IVA disponibles

**Estado:** ✅ **ERROR 10246 COMPLETAMENTE RESUELTO**

El campo `CondicionFrenteIva` ahora se incluye correctamente en todas las facturas tipo C, cumpliendo con la Resolución General 5616 de AFIP.
