using System.Xml.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContableWeb.Services.Afip.WSFEv1
{
    /// <summary>
    /// Cliente para el servicio de Facturación Electrónica WSFEv1 de AFIP
    /// Permite generar facturas tipo C y otros comprobantes electrónicos
    /// </summary>
    public class WsfEv1Client
    {
        private readonly HttpClient _httpClient;
        private readonly string _serviceUrl;
        private readonly ILogger<WsfEv1Client> _logger;

        public WsfEv1Client(bool isProduction = false, ILogger<WsfEv1Client>? logger = null)
        {
            _httpClient = new HttpClient();
            _serviceUrl = isProduction 
                ? "https://servicios1.afip.gov.ar/wsfev1/service.asmx"
                : "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
            _logger = logger ?? NullLogger<WsfEv1Client>.Instance;
        }

        /// <summary>
        /// Solicita autorización para generar un comprobante electrónico (CAE)
        /// </summary>
        public async Task<FECAESolicitarResponse> SolicitarCAEAsync(FEAuthRequest auth, FECAERequest request)
        {
            var soapRequest = CreateSoapEnvelope("FECAESolicitar", auth, request);
            
            Console.WriteLine("=== SOLICITANDO CAE A AFIP WSFEv1 ===");
            Console.WriteLine($"URL: {_serviceUrl}");
            Console.WriteLine($"SOAP Request (longitud: {soapRequest.Length}):");
            Console.WriteLine(soapRequest);
            
            // Escribir también el XML a un archivo temporal para análisis detallado
            var tempFile = Path.Combine(Path.GetTempPath(), $"afip_request_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
            try
            {
                File.WriteAllText(tempFile, soapRequest, System.Text.Encoding.UTF8);
                Console.WriteLine($"XML guardado en: {tempFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No se pudo guardar XML temporal: {ex.Message}");
            }
            
            var content = new StringContent(soapRequest, System.Text.Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://ar.gov.afip.dif.FEV1/FECAESolicitar");
            
            try
            {
                var response = await _httpClient.PostAsync(_serviceUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine("=== RESPUESTA DE AFIP WSFEv1 ===");
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response:");
                Console.WriteLine(responseContent);
                
                return ParseSoapResponse<FECAESolicitarResponse>(responseContent, "FECAESolicitarResponse");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error llamando a WSFEv1 FECAESolicitar");
                throw new WSFEv1Exception("Error en la comunicación con WSFEv1", ex);
            }
        }

        /// <summary>
        /// Obtiene el último número de comprobante autorizado
        /// </summary>
        public async Task<FERecuperaLastCbteResponse> ObtenerUltimoComprobanteAsync(FEAuthRequest auth, int puntoVenta, int tipoComprobante)
        {
            Console.WriteLine($"=== CONSULTANDO ÚLTIMO COMPROBANTE ===");
            Console.WriteLine($"PV: {puntoVenta}, Tipo: {tipoComprobante}, CUIT: {auth.Cuit}");
            
            var request = new FERecuperaLastCbteRequest
            {
                Auth = auth,
                PtoVta = puntoVenta,
                CbteTipo = tipoComprobante
            };

            var soapRequest = CreateSoapEnvelope("FECompUltimoAutorizado", request);
            
            Console.WriteLine($"SOAP Request para FECompUltimoAutorizado:");
            Console.WriteLine(soapRequest);
            
            var content = new StringContent(soapRequest, System.Text.Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://ar.gov.afip.dif.FEV1/FECompUltimoAutorizado");
            
            try
            {
                var response = await _httpClient.PostAsync(_serviceUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"=== RESPUESTA FECompUltimoAutorizado ===");
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response: {responseContent}");
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new WSFEv1Exception($"Error HTTP {response.StatusCode}: {responseContent}");
                }
                
                var result = ParseSoapResponse<FERecuperaLastCbteResponse>(responseContent, "FECompUltimoAutorizadoResponse");
                
                Console.WriteLine($"✅ Resultado parseado - PV: {result.PtoVta}, Nro: {result.CbteNro}, Tipo: {result.CbteTipo}");
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en FECompUltimoAutorizado: {ex.Message}");
                _logger.LogError(ex, "Error obteniendo último comprobante para PV:{PuntoVenta} Tipo:{TipoComprobante}", puntoVenta, tipoComprobante);
                throw new WSFEv1Exception("Error obteniendo último comprobante", ex);
            }
        }

        /// <summary>
        /// Obtiene los tipos de comprobante disponibles
        /// </summary>
        public async Task<CbteTipoResponse> ObtenerTiposComprobanteAsync(FEAuthRequest auth)
        {
            var request = new { Auth = auth };
            var soapRequest = CreateSoapEnvelope("FEParamGetTiposCbte", request);
            
            var content = new StringContent(soapRequest, System.Text.Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://ar.gov.afip.dif.FEV1/FEParamGetTiposCbte");
            
            var response = await _httpClient.PostAsync(_serviceUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return ParseSoapResponse<CbteTipoResponse>(responseContent, "FEParamGetTiposCbteResponse");
        }

        /// <summary>
        /// Método dummy para verificar conectividad
        /// </summary>
        public async Task<DummyResponse> TestConexionAsync()
        {
            var soapRequest = CreateSoapEnvelope("FEDummy", new { });
            
            var content = new StringContent(soapRequest, System.Text.Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://ar.gov.afip.dif.FEV1/FEDummy");
            
            var response = await _httpClient.PostAsync(_serviceUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return ParseSoapResponse<DummyResponse>(responseContent, "FEDummyResponse");
        }

        /// <summary>
        /// Obtiene las condiciones frente al IVA disponibles
        /// </summary>
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

        private string CreateSoapEnvelope(string methodName, params object[] parameters)
        {
            Console.WriteLine($"=== CREANDO SOAP ENVELOPE PARA {methodName} ===");
            
            var parametersXml = "";
            try
            {
                parametersXml = SerializeParameters(parameters);
                Console.WriteLine($"Parámetros serializados correctamente. Longitud: {parametersXml.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializando parámetros: {ex.Message}");
                throw new WSFEv1Exception($"Error serializando parámetros para {methodName}: {ex.Message}", ex);
            }

            var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>
        <{methodName} xmlns=""http://ar.gov.afip.dif.FEV1/"">
            {parametersXml}
        </{methodName}>
    </soap:Body>
</soap:Envelope>";
            
            Console.WriteLine($"SOAP Envelope creado exitosamente para {methodName}");
            return envelope;
        }

        private string SerializeParameters(object[] parameters)
        {
            var result = "";
            var paramCount = 0;
            
            foreach (var param in parameters)
            {
                if (param != null)
                {
                    paramCount++;
                    Console.WriteLine($"Serializando parámetro {paramCount}: {param.GetType().Name}");
                    
                    try
                    {
                        var serializedParam = SerializeObject(param);
                        result += serializedParam;
                        Console.WriteLine($"Parámetro {paramCount} serializado correctamente (longitud: {serializedParam.Length})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error serializando parámetro {paramCount}: {ex.Message}");
                        throw;
                    }
                }
            }
            
            Console.WriteLine($"Total parámetros serializados: {paramCount}");
            return result;
        }

        private string SerializeObject(object obj)
        {
            if (obj == null) return "";

            Console.WriteLine($"Serializando objeto tipo: {obj.GetType().Name}");

            // Manejo específico por tipo de objeto AFIP
            return obj switch
            {
                FEAuthRequest auth => SerializeFEAuthRequest(auth),
                FECAERequest caeReq => SerializeFECAERequest(caeReq),
                FERecuperaLastCbteRequest lastReq => SerializeFERecuperaLastCbteRequest(lastReq),
                _ => SerializeGenericObject(obj)
            };
        }

        private string SerializeFEAuthRequest(FEAuthRequest auth)
        {
            Console.WriteLine($"Serializando Auth - CUIT: {auth.Cuit}, Token length: {auth.Token?.Length ?? 0}, Sign length: {auth.Sign?.Length ?? 0}");
            
            return $@"<Auth>
                <Token>{System.Security.SecurityElement.Escape(auth.Token)}</Token>
                <Sign>{System.Security.SecurityElement.Escape(auth.Sign)}</Sign>
                <Cuit>{auth.Cuit}</Cuit>
            </Auth>";
        }

        private string SerializeFECAERequest(FECAERequest request)
        {
            var result = "<FeCAEReq>";
            
            if (request.FeCabReq != null)
            {
                result += $@"<FeCabReq>
                    <CantReg>{request.FeCabReq.CantReg}</CantReg>
                    <PtoVta>{request.FeCabReq.PtoVta}</PtoVta>
                    <CbteTipo>{request.FeCabReq.CbteTipo}</CbteTipo>
                </FeCabReq>";
            }

            if (request.FeDetReq != null && request.FeDetReq.Length > 0)
            {
                result += "<FeDetReq>";
                foreach (var det in request.FeDetReq)
                {
                    result += SerializeFECAEDetRequest(det);
                }
                result += "</FeDetReq>";
            }

            result += "</FeCAEReq>";
            return result;
        }

        private string SerializeFECAEDetRequest(FECAEDetRequest det)
        {
            var cult = System.Globalization.CultureInfo.InvariantCulture;
            
            var result = $@"<FECAEDetRequest>
                <Concepto>{det.Concepto}</Concepto>
                <DocTipo>{det.DocTipo}</DocTipo>
                <DocNro>{det.DocNro}</DocNro>
                <CbteDesde>{det.CbteDesde}</CbteDesde>
                <CbteHasta>{det.CbteHasta}</CbteHasta>
                <CbteFch>{System.Security.SecurityElement.Escape(det.CbteFch)}</CbteFch>
                <ImpTotal>{det.ImpTotal.ToString("F2", cult)}</ImpTotal>
                <ImpTotConc>{det.ImpTotConc.ToString("F2", cult)}</ImpTotConc>
                <ImpNeto>{det.ImpNeto.ToString("F2", cult)}</ImpNeto>
                <ImpOpEx>{det.ImpOpEx.ToString("F2", cult)}</ImpOpEx>
                <ImpIVA>{det.ImpIVA.ToString("F2", cult)}</ImpIVA>
                <ImpTrib>{det.ImpTrib.ToString("F2", cult)}</ImpTrib>
                <MonId>{System.Security.SecurityElement.Escape(det.MonId)}</MonId>
                <MonCotiz>{det.MonCotiz.ToString("F2", cult)}</MonCotiz>";

            if (!string.IsNullOrEmpty(det.FchServDesde))
                result += $"<FchServDesde>{System.Security.SecurityElement.Escape(det.FchServDesde)}</FchServDesde>";
            
            if (!string.IsNullOrEmpty(det.FchServHasta))
                result += $"<FchServHasta>{System.Security.SecurityElement.Escape(det.FchServHasta)}</FchServHasta>";
            
            if (!string.IsNullOrEmpty(det.FchVtoPago))
                result += $"<FchVtoPago>{System.Security.SecurityElement.Escape(det.FchVtoPago)}</FchVtoPago>";

            // Serializar IVA
            if (det.Iva != null && det.Iva.Length > 0)
            {
                result += "<Iva>";
                foreach (var iva in det.Iva)
                {
                    result += $@"<AlicIva>
                        <Id>{iva.Id}</Id>
                        <BaseImp>{iva.BaseImp.ToString("F2", cult)}</BaseImp>
                        <Importe>{iva.Importe.ToString("F2", cult)}</Importe>
                    </AlicIva>";
                }
                result += "</Iva>";
            }

            // Serializar Tributos
            if (det.Tributos != null && det.Tributos.Length > 0)
            {
                result += "<Tributos>";
                foreach (var trib in det.Tributos)
                {
                    result += $@"<Tributo>
                        <Id>{trib.Id}</Id>
                        <Desc>{System.Security.SecurityElement.Escape(trib.Desc ?? "")}</Desc>
                        <BaseImp>{trib.BaseImp.ToString("F2", cult)}</BaseImp>
                        <Alic>{trib.Alic.ToString("F2", cult)}</Alic>
                        <Importe>{trib.Importe.ToString("F2", cult)}</Importe>
                    </Tributo>";
                }
                result += "</Tributos>";
            }

            // Agregar condición frente al IVA (obligatorio según RG 5616)
            result += $"<CondicionFrenteIva>{det.CondicionFrenteIva}</CondicionFrenteIva>";

            result += "</FECAEDetRequest>";
            return result;
        }

        private string SerializeFERecuperaLastCbteRequest(FERecuperaLastCbteRequest request)
        {
            var authXml = request.Auth != null ? SerializeFEAuthRequest(request.Auth) : "";
            return $@"{authXml}
                <PtoVta>{request.PtoVta}</PtoVta>
                <CbteTipo>{request.CbteTipo}</CbteTipo>";
        }

        private string SerializeGenericObject(object obj)
        {
            // Fallback para objetos simples
            if (obj is string str) return System.Security.SecurityElement.Escape(str);
            if (obj.GetType().IsValueType) return obj.ToString() ?? "";
            
            // Para objetos anónimos simples
            var result = "";
            var type = obj.GetType();
            
            foreach (var prop in type.GetProperties().Take(10)) // Limitar a 10 propiedades para evitar bucles
            {
                try
                {
                    var value = prop.GetValue(obj);
                    if (value != null && (value is string || value.GetType().IsValueType))
                    {
                        result += $"<{prop.Name}>{System.Security.SecurityElement.Escape(value.ToString() ?? "")}</{prop.Name}>";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: No se pudo serializar propiedad {prop.Name}: {ex.Message}");
                }
            }
            
            return result;
        }

        private T ParseSoapResponse<T>(string soapResponse, string elementName) where T : new()
        {
            try
            {
                Console.WriteLine($"=== PARSEANDO RESPUESTA SOAP: {elementName} ===");
                Console.WriteLine($"Respuesta XML (primeros 500 chars): {soapResponse.Substring(0, Math.Min(500, soapResponse.Length))}...");
                
                var doc = System.Xml.Linq.XDocument.Parse(soapResponse);
                var soapNs = System.Xml.Linq.XNamespace.Get("http://schemas.xmlsoap.org/soap/envelope/");
                var afipNs = System.Xml.Linq.XNamespace.Get("http://ar.gov.afip.dif.FEV1/");
                
                var responseElement = doc.Root?.Element(soapNs + "Body")?.Element(afipNs + elementName);
                
                if (responseElement == null)
                {
                    Console.WriteLine($"❌ No se encontró elemento {elementName} en la respuesta");
                    throw new WSFEv1Exception($"No se encontró el elemento {elementName} en la respuesta");
                }
                
                // Parsing específico por tipo
                if (typeof(T) == typeof(FERecuperaLastCbteResponse))
                {
                    return (T)(object)ParseFERecuperaLastCbteResponse(responseElement, afipNs);
                }
                
                // Para otros tipos, devolver objeto básico
                Console.WriteLine($"⚠️ Usando parsing genérico para tipo {typeof(T).Name}");
                return new T();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error parseando respuesta SOAP: {ex.Message}");
                throw new WSFEv1Exception($"Error parseando respuesta SOAP: {ex.Message}", ex);
            }
        }

        private FERecuperaLastCbteResponse ParseFERecuperaLastCbteResponse(System.Xml.Linq.XElement responseElement, System.Xml.Linq.XNamespace ns)
        {
            var result = new FERecuperaLastCbteResponse();
            
            try
            {
                var resultElement = responseElement.Element(ns + "FECompUltimoAutorizadoResult");
                
                if (resultElement != null)
                {
                    // Parsear valores principales
                    var ptoVtaElement = resultElement.Element(ns + "PtoVta");
                    var cbteNroElement = resultElement.Element(ns + "CbteNro"); 
                    var cbteTipoElement = resultElement.Element(ns + "CbteTipo");
                    
                    if (ptoVtaElement != null && int.TryParse(ptoVtaElement.Value, out var ptoVta))
                        result.PtoVta = ptoVta;
                        
                    if (cbteNroElement != null && int.TryParse(cbteNroElement.Value, out var cbteNro))
                        result.CbteNro = cbteNro;
                        
                    if (cbteTipoElement != null && int.TryParse(cbteTipoElement.Value, out var cbteTipo))
                        result.CbteTipo = cbteTipo;
                    
                    Console.WriteLine($"✅ Último comprobante parseado - PV: {result.PtoVta}, Tipo: {result.CbteTipo}, Nro: {result.CbteNro}");
                    
                    // Parsear errores si existen
                    var errorsElement = resultElement.Element(ns + "Errors");
                    if (errorsElement != null)
                    {
                        var errorList = new List<Err>();
                        foreach (var errorElement in errorsElement.Elements(ns + "Err"))
                        {
                            var codeElement = errorElement.Element(ns + "Code");
                            var msgElement = errorElement.Element(ns + "Msg");
                            
                            if (codeElement != null && msgElement != null && 
                                int.TryParse(codeElement.Value, out var errorCode))
                            {
                                errorList.Add(new Err { Code = errorCode, Msg = msgElement.Value });
                                Console.WriteLine($"⚠️ Error parseado: {errorCode} - {msgElement.Value}");
                            }
                        }
                        result.Errors = errorList.ToArray();
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ No se encontró FECompUltimoAutorizadoResult en la respuesta");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error parseando FERecuperaLastCbteResponse: {ex.Message}");
            }
            
            return result;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Excepción específica del servicio WSFEv1
    /// </summary>
    public class WSFEv1Exception : Exception
    {
        public WSFEv1Exception(string message) : base(message) { }
        public WSFEv1Exception(string message, Exception innerException) : base(message, innerException) { }
    }
}
