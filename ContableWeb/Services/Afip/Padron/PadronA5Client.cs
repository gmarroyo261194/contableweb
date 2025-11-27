﻿using System.Text;
using System.Xml.Linq;

namespace ContableWeb.Services.Afip.Padron;

/// <summary>
/// Cliente SOAP para el servicio de Padrón AFIP (PersonaServiceA5)
/// </summary>
public class PadronA5Client
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceUrl;
    private readonly ILogger<PadronA5Client> _logger;

    public PadronA5Client(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PadronA5Client> logger)
    {
        _httpClient = httpClientFactory.CreateClient("AfipPadron");
        _logger = logger;
        
        // URL del servicio (producción o homologación)
        var isProduction = configuration.GetValue<bool>("Afip:IsProduction", false);
        _serviceUrl = isProduction
            ? "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA5"
            : "https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5";
    }

    /// <summary>
    /// Consulta los datos de una persona por CUIT/CUIL
    /// </summary>
    public async Task<PersonaReturn> GetPersonaAsync(string token, string sign, long cuitRepresentada, long idPersona)
    {
        try
        {
            _logger.LogInformation($"=== Consultando Persona en Padrón AFIP: {idPersona} ===");
            
            var soapRequest = CreateSoapEnvelope("getPersona_v2", new
            {
                token,
                sign,
                cuitRepresentada,
                idPersona
            });
            
            _logger.LogDebug("SOAP Request: {Request}", soapRequest.Substring(0, Math.Min(500, soapRequest.Length)));
            
            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "");
            
            var response = await _httpClient.PostAsync(_serviceUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("SOAP Response Status: {StatusCode}", response.StatusCode);
            _logger.LogDebug("SOAP Response Length: {Length} characters", responseContent.Length);
            
            // Mostrar el XML completo si es razonable
            if (responseContent.Length < 5000)
            {
                _logger.LogDebug("SOAP Response completo:\n{Response}", responseContent);
            }
            else
            {
                _logger.LogDebug("SOAP Response (primeros 2000 chars):\n{Response}", 
                    responseContent.Substring(0, 2000));
            }
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error HTTP: {StatusCode} - {Content}", response.StatusCode, responseContent);
                
                // Intentar extraer el mensaje de error del SOAP Fault
                try
                {
                    var doc = System.Xml.Linq.XDocument.Parse(responseContent);
                    var faultString = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value;
                    if (!string.IsNullOrEmpty(faultString))
                    {
                        throw new PadronAfipException($"Error SOAP: {faultString}");
                    }
                }
                catch (PadronAfipException)
                {
                    throw;
                }
                catch
                {
                    // Si no se puede parsear, lanzar error genérico
                }
                
                throw new PadronAfipException($"Error HTTP: {response.StatusCode}");
            }
            
            return ParsePersonaResponse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando persona {IdPersona}", idPersona);
            throw;
        }
    }

    /// <summary>
    /// Consulta los datos de múltiples personas
    /// </summary>
    public async Task<PersonaListReturn> GetPersonaListAsync(string token, string sign, long cuitRepresentada, long[] idPersonas)
    {
        try
        {
            _logger.LogInformation($"=== Consultando {idPersonas.Length} Personas en Padrón AFIP ===");
            
            var soapRequest = CreateSoapEnvelopeForList("getPersonaList_v2", token, sign, cuitRepresentada, idPersonas);
            
            _logger.LogDebug("SOAP Request: {Request}", soapRequest.Substring(0, Math.Min(500, soapRequest.Length)));
            
            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "");
            
            var response = await _httpClient.PostAsync(_serviceUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug("SOAP Response Length: {Length}", responseContent.Length);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error HTTP: {StatusCode} - {Content}", response.StatusCode, responseContent);
                
                // Intentar extraer el mensaje de error del SOAP Fault
                try
                {
                    var doc = System.Xml.Linq.XDocument.Parse(responseContent);
                    var faultString = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value;
                    if (!string.IsNullOrEmpty(faultString))
                    {
                        throw new PadronAfipException($"Error SOAP: {faultString}");
                    }
                }
                catch (PadronAfipException)
                {
                    throw;
                }
                catch
                {
                    // Si no se puede parsear, lanzar error genérico
                }
                
                throw new PadronAfipException($"Error HTTP: {response.StatusCode}");
            }
            
            return ParsePersonaListResponse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando lista de personas");
            throw;
        }
    }

    /// <summary>
    /// Verifica la conectividad con el servicio
    /// </summary>
    public async Task<PadronDummyResponse> DummyAsync()
    {
        try
        {
            _logger.LogInformation("=== Verificando conectividad con Padrón AFIP ===");
            
            var soapRequest = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:sr=""http://a5.soap.ws.server.puc.sr/"">
  <soap:Body>
    <sr:dummy/>
  </soap:Body>
</soap:Envelope>";
            
            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "");
            
            var response = await _httpClient.PostAsync(_serviceUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug("Dummy Response: {Response}", responseContent);
            
            return ParseDummyResponse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en dummy call");
            throw;
        }
    }

    #region Private Methods

    /// <summary>
    /// Helper para buscar un elemento con o sin namespace (AFIP no usa prefijo en elementos hijos)
    /// </summary>
    private XElement? GetElement(XElement parent, XNamespace ns, string localName)
    {
        return parent.Element(ns + localName) ?? parent.Element(localName);
    }

    private string CreateSoapEnvelope(string methodName, dynamic parameters)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
        sb.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:sr=""http://a5.soap.ws.server.puc.sr/"">");
        sb.AppendLine("  <soap:Body>");
        sb.AppendLine($"    <sr:{methodName}>");
        sb.AppendLine($"      <token>{parameters.token}</token>");
        sb.AppendLine($"      <sign>{parameters.sign}</sign>");
        sb.AppendLine($"      <cuitRepresentada>{parameters.cuitRepresentada}</cuitRepresentada>");
        sb.AppendLine($"      <idPersona>{parameters.idPersona}</idPersona>");
        sb.AppendLine($"    </sr:{methodName}>");
        sb.AppendLine("  </soap:Body>");
        sb.AppendLine("</soap:Envelope>");
        
        return sb.ToString();
    }

    private string CreateSoapEnvelopeForList(string methodName, string token, string sign, long cuitRepresentada, long[] idPersonas)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
        sb.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:sr=""http://a5.soap.ws.server.puc.sr/"">");
        sb.AppendLine("  <soap:Body>");
        sb.AppendLine($"    <sr:{methodName}>");
        sb.AppendLine($"      <token>{token}</token>");
        sb.AppendLine($"      <sign>{sign}</sign>");
        sb.AppendLine($"      <cuitRepresentada>{cuitRepresentada}</cuitRepresentada>");
        
        foreach (var id in idPersonas)
        {
            sb.AppendLine($"      <idPersona>{id}</idPersona>");
        }
        
        sb.AppendLine($"    </sr:{methodName}>");
        sb.AppendLine("  </soap:Body>");
        sb.AppendLine("</soap:Envelope>");
        
        return sb.ToString();
    }

    private PersonaReturn ParsePersonaResponse(string soapResponse)
    {
        try
        {
            _logger.LogDebug("=== PARSEANDO RESPUESTA PERSONA ===");
            _logger.LogDebug("XML Response (primeros 1000 chars): {Response}", 
                soapResponse.Substring(0, Math.Min(1000, soapResponse.Length)));
            
            var doc = XDocument.Parse(soapResponse);
            var ns = XNamespace.Get("http://a5.soap.ws.server.puc.sr/");
            var soapNs = XNamespace.Get("http://schemas.xmlsoap.org/soap/envelope/");
            
            // Verificar si hay un SOAP Fault
            var fault = doc.Root?.Element(soapNs + "Body")?.Element(soapNs + "Fault");
            if (fault != null)
            {
                var faultString = fault.Element("faultstring")?.Value ?? "Error desconocido";
                _logger.LogError("SOAP Fault detectado: {FaultString}", faultString);
                throw new PadronAfipException($"Error SOAP: {faultString}");
            }
            
            var responseElement = doc.Root?.Element(soapNs + "Body")?.Element(ns + "getPersona_v2Response");
            
            if (responseElement == null)
            {
                _logger.LogError("No se encontró getPersona_v2Response en la respuesta");
                
                // Mostrar estructura del Body para debug
                var body = doc.Root?.Element(soapNs + "Body");
                if (body != null)
                {
                    _logger.LogError("Elementos en Body:");
                    foreach (var elem in body.Elements())
                    {
                        _logger.LogError("  - {Name} (Namespace: {Namespace})", 
                            elem.Name.LocalName, elem.Name.NamespaceName);
                    }
                }
                
                throw new PadronAfipException("Respuesta SOAP inválida");
            }
            
            _logger.LogDebug("✓ getPersona_v2Response encontrado");
            
            // personaReturn puede venir con o sin el namespace prefijado
            var personaReturn = responseElement.Element(ns + "personaReturn") 
                             ?? responseElement.Element("personaReturn");
            
            if (personaReturn == null)
            {
                _logger.LogError("No se encontró personaReturn en la respuesta");
                
                // Mostrar estructura del response para debug
                _logger.LogError("Elementos en getPersona_v2Response:");
                foreach (var elem in responseElement.Elements())
                {
                    _logger.LogError("  - {Name} (Namespace: {Namespace})", 
                        elem.Name.LocalName, elem.Name.NamespaceName);
                }
                
                throw new PadronAfipException("Respuesta sin datos de persona");
            }
            
            _logger.LogDebug("✓ personaReturn encontrado");
            
            return ParsePersonaReturn(personaReturn, ns);
        }
        catch (PadronAfipException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parseando respuesta de persona");
            throw new PadronAfipException("Error parseando respuesta SOAP", ex);
        }
    }

    private PersonaListReturn ParsePersonaListResponse(string soapResponse)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);
            var ns = XNamespace.Get("http://a5.soap.ws.server.puc.sr/");
            var soapNs = XNamespace.Get("http://schemas.xmlsoap.org/soap/envelope/");
            
            var responseElement = doc.Root?.Element(soapNs + "Body")?.Element(ns + "getPersonaList_v2Response");
            
            if (responseElement == null)
            {
                _logger.LogError("No se encontró getPersonaList_v2Response en la respuesta");
                throw new PadronAfipException("Respuesta SOAP inválida");
            }
            
            var personaListReturn = responseElement.Element(ns + "personaListReturn");
            
            if (personaListReturn == null)
            {
                _logger.LogError("No se encontró personaListReturn en la respuesta");
                throw new PadronAfipException("Respuesta sin datos de personas");
            }
            
            return ParsePersonaListReturn(personaListReturn, ns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parseando respuesta de lista de personas");
            throw new PadronAfipException("Error parseando respuesta SOAP", ex);
        }
    }

    private PadronDummyResponse ParseDummyResponse(string soapResponse)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);
            var ns = XNamespace.Get("http://a5.soap.ws.server.puc.sr/");
            var soapNs = XNamespace.Get("http://schemas.xmlsoap.org/soap/envelope/");
            
            var responseElement = doc.Root?.Element(soapNs + "Body")?.Element(ns + "dummyResponse");
            var returnElement = responseElement?.Element(ns + "return");
            
            if (returnElement == null)
            {
                throw new PadronAfipException("No se encontró elemento return en dummyResponse");
            }
            
            return new PadronDummyResponse
            {
                AppServer = returnElement.Element(ns + "appserver")?.Value ?? "",
                AuthServer = returnElement.Element(ns + "authserver")?.Value ?? "",
                DbServer = returnElement.Element(ns + "dbserver")?.Value ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parseando dummy response");
            throw;
        }
    }

    private PersonaReturn ParsePersonaReturn(XElement personaReturnElement, XNamespace ns)
    {
        var result = new PersonaReturn
        {
            Metadata = ParseMetadata(personaReturnElement.Element(ns + "metadata") ?? personaReturnElement.Element("metadata"), ns)
        };
        
        // Verificar si hay errorConstancia a nivel de personaReturn (sin persona)
        var errorConstanciaElement = personaReturnElement.Element(ns + "errorConstancia") 
                                   ?? personaReturnElement.Element("errorConstancia");
        if (errorConstanciaElement != null)
        {
            // Hay error a nivel de personaReturn, crear Persona con el error
            result.Persona = new Persona
            {
                ErrorConstancia = ParseErrorConstancia(errorConstanciaElement, ns)
            };
            
            var errores = result.Persona.ErrorConstancia?.Errores;
            if (errores != null && errores.Length > 0)
            {
                var mensajeError = string.Join("; ", errores);
                _logger.LogWarning("AFIP retornó errorConstancia: {Error}", mensajeError);
            }
        }
        else
        {
            // En el XML de AFIP, los datos vienen directamente en personaReturn
            // Puede tener un elemento <persona> o los datos directamente
            var personaElement = personaReturnElement.Element(ns + "persona") 
                              ?? personaReturnElement.Element("persona");
            
            if (personaElement != null)
            {
                // Caso 1: Hay elemento <persona> intermedio
                result.Persona = ParsePersona(personaElement, ns);
            }
            else
            {
                // Caso 2: Los datos están directamente en personaReturn (caso más común)
                result.Persona = new Persona
                {
                    DatosGenerales = ParseDatosGenerales(
                        personaReturnElement.Element(ns + "datosGenerales") ?? personaReturnElement.Element("datosGenerales"), ns),
                    DatosMonotributo = ParseDatosMonotributo(
                        personaReturnElement.Element(ns + "datosMonotributo") ?? personaReturnElement.Element("datosMonotributo"), ns),
                    DatosRegimenGeneral = ParseDatosRegimenGeneral(
                        personaReturnElement.Element(ns + "datosRegimenGeneral") ?? personaReturnElement.Element("datosRegimenGeneral"), ns),
                    ErrorConstancia = ParseErrorConstancia(
                        personaReturnElement.Element(ns + "errorConstancia") ?? personaReturnElement.Element("errorConstancia"), ns),
                    ErrorMonotributo = ParseErrorMonotributo(
                        personaReturnElement.Element(ns + "errorMonotributo") ?? personaReturnElement.Element("errorMonotributo"), ns),
                    ErrorRegimenGeneral = ParseErrorRegimenGeneral(
                        personaReturnElement.Element(ns + "errorRegimenGeneral") ?? personaReturnElement.Element("errorRegimenGeneral"), ns)
                };
                
                _logger.LogDebug("✓ Datos parseados directamente desde personaReturn");
            }
        }
        
        return result;
    }

    private PersonaListReturn ParsePersonaListReturn(XElement personaListReturnElement, XNamespace ns)
    {
        var result = new PersonaListReturn
        {
            Metadata = ParseMetadata(personaListReturnElement.Element(ns + "metadata"), ns),
            Personas = personaListReturnElement.Elements(ns + "persona")
                .Select(p => ParsePersona(p, ns))
                .Where(p => p != null)
                .ToArray()!
        };
        
        return result;
    }

    private Metadata? ParseMetadata(XElement? metadataElement, XNamespace ns)
    {
        if (metadataElement == null) return null;
        
        return new Metadata
        {
            FechaHora = DateTime.Parse(GetElement(metadataElement, ns, "fechaHora")?.Value ?? DateTime.Now.ToString()),
            Servidor = GetElement(metadataElement, ns, "servidor")?.Value ?? ""
        };
    }

    private Persona? ParsePersona(XElement? personaElement, XNamespace ns)
    {
        if (personaElement == null) return null;
        
        return new Persona
        {
            DatosGenerales = ParseDatosGenerales(
                GetElement(personaElement, ns, "datosGenerales"), ns),
            DatosMonotributo = ParseDatosMonotributo(
                GetElement(personaElement, ns, "datosMonotributo"), ns),
            DatosRegimenGeneral = ParseDatosRegimenGeneral(
                GetElement(personaElement, ns, "datosRegimenGeneral"), ns),
            ErrorConstancia = ParseErrorConstancia(
                GetElement(personaElement, ns, "errorConstancia"), ns),
            ErrorMonotributo = ParseErrorMonotributo(
                GetElement(personaElement, ns, "errorMonotributo"), ns),
            ErrorRegimenGeneral = ParseErrorRegimenGeneral(
                GetElement(personaElement, ns, "errorRegimenGeneral"), ns)
        };
    }

    private DatosGenerales? ParseDatosGenerales(XElement? element, XNamespace ns)
    {
        if (element == null) return null;
        
        return new DatosGenerales
        {
            Apellido = GetElement(element, ns, "apellido")?.Value,
            DomicilioFiscal = ParseDomicilio(GetElement(element, ns, "domicilioFiscal"), ns),
            EstadoClave = GetElement(element, ns, "estadoClave")?.Value,
            IdPersona = long.Parse(GetElement(element, ns, "idPersona")?.Value ?? "0"),
            MesCierre = int.Parse(GetElement(element, ns, "mesCierre")?.Value ?? "0"),
            Nombre = GetElement(element, ns, "nombre")?.Value,
            RazonSocial = GetElement(element, ns, "razonSocial")?.Value,
            TipoPersona = GetElement(element, ns, "tipoPersona")?.Value,
            TipoClave = GetElement(element, ns, "tipoClave")?.Value
        };
    }

    private Domicilio? ParseDomicilio(XElement? element, XNamespace ns)
    {
        if (element == null) return null;
        
        return new Domicilio
        {
            CodigoPostal = GetElement(element, ns, "codPostal")?.Value,
            DescripcionProvincia = GetElement(element, ns, "descripcionProvincia")?.Value,
            Direccion = GetElement(element, ns, "direccion")?.Value,
            IdProvincia = int.Parse(GetElement(element, ns, "idProvincia")?.Value ?? "0"),
            Localidad = GetElement(element, ns, "localidad")?.Value,
            TipoDomicilio = GetElement(element, ns, "tipoDomicilio")?.Value
        };
    }

    private DatosMonotributo? ParseDatosMonotributo(XElement? element, XNamespace ns)
    {
        if (element == null) return null;
        
        var result = new DatosMonotributo();
        
        // Parsear actividades monotributo - buscar con y sin namespace
        var actividades = new List<ActividadMonotributo>();
        var actividadesElements = element.Elements(ns + "actividad").Any() 
            ? element.Elements(ns + "actividad") 
            : element.Elements("actividad");
            
        foreach (var actElement in actividadesElements)
        {
            var actividad = new ActividadMonotributo
            {
                DescripcionActividad = GetElement(actElement, ns, "descripcionActividad")?.Value,
                IdActividad = long.Parse(GetElement(actElement, ns, "idActividad")?.Value ?? "0"),
                Nomenclador = int.Parse(GetElement(actElement, ns, "nomenclador")?.Value ?? "0"),
                Orden = int.Parse(GetElement(actElement, ns, "orden")?.Value ?? "0"),
                Periodo = int.Parse(GetElement(actElement, ns, "periodo")?.Value ?? "0")
            };
            actividades.Add(actividad);
        }
        result.Actividades = actividades.ToArray();
        
        // Parsear categoría monotributo
        var categoriaElement = GetElement(element, ns, "categoriaMonotributo");
        if (categoriaElement != null)
        {
            result.CategoriaMonotributo = new CategoriaMonotributo
            {
                DescripcionCategoria = GetElement(categoriaElement, ns, "descripcionCategoria")?.Value,
                IdCategoria = int.Parse(GetElement(categoriaElement, ns, "idCategoria")?.Value ?? "0"),
                IdImpuesto = int.Parse(GetElement(categoriaElement, ns, "idImpuesto")?.Value ?? "0"),
                Periodo = int.Parse(GetElement(categoriaElement, ns, "periodo")?.Value ?? "0")
            };
        }
        
        // Parsear componente
        var componenteElement = GetElement(element, ns, "componente");
        if (componenteElement != null)
        {
            result.Componente = new ComponenteMonotributo
            {
                DescripcionComponente = GetElement(componenteElement, ns, "descripcionComponente")?.Value,
                IdComponente = int.Parse(GetElement(componenteElement, ns, "idComponente")?.Value ?? "0")
            };
        }
        
        // Parsear impuestos monotributo - buscar con y sin namespace
        var impuestos = new List<ImpuestoMonotributo>();
        var impuestosElements = element.Elements(ns + "impuesto").Any() 
            ? element.Elements(ns + "impuesto") 
            : element.Elements("impuesto");
            
        foreach (var impElement in impuestosElements)
        {
            var impuesto = new ImpuestoMonotributo
            {
                DescripcionImpuesto = GetElement(impElement, ns, "descripcionImpuesto")?.Value,
                EstadoImpuesto = GetElement(impElement, ns, "estadoImpuesto")?.Value,
                IdImpuesto = int.Parse(GetElement(impElement, ns, "idImpuesto")?.Value ?? "0"),
                Motivo = GetElement(impElement, ns, "motivo")?.Value,
                Periodo = int.Parse(GetElement(impElement, ns, "periodo")?.Value ?? "0")
            };
            impuestos.Add(impuesto);
        }
        result.Impuestos = impuestos.ToArray();
        
        return result;
    }

    private DatosRegimenGeneral? ParseDatosRegimenGeneral(XElement? element, XNamespace ns)
    {
        if (element == null) return null;
        
        var result = new DatosRegimenGeneral();
        
        // Parsear actividades - buscar con y sin namespace
        var actividades = new List<Actividad>();
        var actividadesElements = element.Elements(ns + "actividad").Any() 
            ? element.Elements(ns + "actividad") 
            : element.Elements("actividad");
            
        foreach (var actElement in actividadesElements)
        {
            var actividad = new Actividad
            {
                DescripcionActividad = GetElement(actElement, ns, "descripcionActividad")?.Value,
                IdActividad = long.Parse(GetElement(actElement, ns, "idActividad")?.Value ?? "0"),
                Nomenclador = int.Parse(GetElement(actElement, ns, "nomenclador")?.Value ?? "0"),
                Orden = int.Parse(GetElement(actElement, ns, "orden")?.Value ?? "0"),
                Periodo = int.Parse(GetElement(actElement, ns, "periodo")?.Value ?? "0")
            };
            actividades.Add(actividad);
        }
        result.Actividades = actividades.ToArray();
        
        // Parsear impuestos - buscar con y sin namespace
        var impuestos = new List<Impuesto>();
        var impuestosElements = element.Elements(ns + "impuesto").Any() 
            ? element.Elements(ns + "impuesto") 
            : element.Elements("impuesto");
            
        foreach (var impElement in impuestosElements)
        {
            var impuesto = new Impuesto
            {
                DescripcionImpuesto = GetElement(impElement, ns, "descripcionImpuesto")?.Value,
                EstadoImpuesto = GetElement(impElement, ns, "estadoImpuesto")?.Value,
                IdImpuesto = int.Parse(GetElement(impElement, ns, "idImpuesto")?.Value ?? "0"),
                Motivo = GetElement(impElement, ns, "motivo")?.Value,
                Periodo = int.Parse(GetElement(impElement, ns, "periodo")?.Value ?? "0")
            };
            impuestos.Add(impuesto);
        }
        result.Impuestos = impuestos.ToArray();
        
        // Parsear regímenes - buscar con y sin namespace
        var regimenes = new List<Regimen>();
        var regimenesElements = element.Elements(ns + "regimen").Any() 
            ? element.Elements(ns + "regimen") 
            : element.Elements("regimen");
            
        foreach (var regElement in regimenesElements)
        {
            var regimen = new Regimen
            {
                DescripcionRegimen = GetElement(regElement, ns, "descripcionRegimen")?.Value,
                IdImpuesto = int.Parse(GetElement(regElement, ns, "idImpuesto")?.Value ?? "0"),
                IdRegimen = int.Parse(GetElement(regElement, ns, "idRegimen")?.Value ?? "0"),
                Periodo = int.Parse(GetElement(regElement, ns, "periodo")?.Value ?? "0"),
                TipoRegimen = GetElement(regElement, ns, "tipoRegimen")?.Value
            };
            regimenes.Add(regimen);
        }
        result.Regimenes = regimenes.ToArray();
        
        // Parsear categoría autónomo si existe
        var categoriaElement = GetElement(element, ns, "categoriaAutonomo");
        if (categoriaElement != null)
        {
            result.CategoriaAutonomo = new Categoria
            {
                DescripcionCategoria = GetElement(categoriaElement, ns, "descripcionCategoria")?.Value,
                IdCategoria = int.Parse(GetElement(categoriaElement, ns, "idCategoria")?.Value ?? "0"),
                IdImpuesto = int.Parse(GetElement(categoriaElement, ns, "idImpuesto")?.Value ?? "0"),
                Periodo = int.Parse(GetElement(categoriaElement, ns, "periodo")?.Value ?? "0")
            };
        }
        
        return result;
    }

    private ErrorConstancia? ParseErrorConstancia(XElement? element, XNamespace ns)
    {
        if (element == null) return null;
        
        // Los elementos error pueden venir con o sin namespace
        var erroresElements = element.Elements(ns + "error").Any() 
            ? element.Elements(ns + "error") 
            : element.Elements("error");
        
        return new ErrorConstancia
        {
            Apellido = GetElement(element, ns, "apellido")?.Value,
            Errores = erroresElements.Select(e => e.Value).ToArray(),
            IdPersona = long.Parse(GetElement(element, ns, "idPersona")?.Value ?? "0"),
            Nombre = GetElement(element, ns, "nombre")?.Value
        };
    }

    private ErrorMonotributo? ParseErrorMonotributo(XElement? element, XNamespace ns)
    {
        if (element == null) return null;
        
        // Los elementos error pueden venir con o sin namespace
        var erroresElements = element.Elements(ns + "error").Any() 
            ? element.Elements(ns + "error") 
            : element.Elements("error");
        
        return new ErrorMonotributo
        {
            Errores = erroresElements.Select(e => e.Value).ToArray(),
            Mensaje = GetElement(element, ns, "mensaje")?.Value
        };
    }

    private ErrorRegimenGeneral? ParseErrorRegimenGeneral(XElement? element, XNamespace ns)
    {
        if (element == null) return null;
        
        // Los elementos error pueden venir con o sin namespace
        var erroresElements = element.Elements(ns + "error").Any() 
            ? element.Elements(ns + "error") 
            : element.Elements("error");
        
        return new ErrorRegimenGeneral
        {
            Errores = erroresElements.Select(e => e.Value).ToArray(),
            Mensaje = GetElement(element, ns, "mensaje")?.Value
        };
    }

    #endregion
}

/// <summary>
/// Excepción personalizada para errores del servicio de Padrón AFIP
/// </summary>
public class PadronAfipException : Exception
{
    public PadronAfipException(string message) : base(message) { }
    public PadronAfipException(string message, Exception innerException) : base(message, innerException) { }
}

