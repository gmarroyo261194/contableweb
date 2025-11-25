using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;

namespace ContableWeb.Services.Afip
{
    /// <summary>
    /// Cliente para el Web Service de Autenticación y Autorización (WSAA) de AFIP
    /// </summary>
    public class AfipWsaaClient
    {
        private readonly string _certificatePath;
        private readonly string _certificatePassword;
        private readonly string _wsaaUrl;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Constructor del cliente WSAA
        /// </summary>
        /// <param name="certificatePath">Ruta del certificado .p12/.pfx</param>
        /// <param name="certificatePassword">Contraseña del certificado</param>
        /// <param name="isProduction">true para producción, false para homologación</param>
        public AfipWsaaClient(string certificatePath, string certificatePassword, bool isProduction = false)
        {
            _certificatePath = certificatePath;
            _certificatePassword = certificatePassword;
            var homologUrl = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
            var prodUrl = "https://wsaa.afip.gov.ar/ws/services/LoginCms";
            _wsaaUrl = isProduction ? prodUrl : homologUrl;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Genera un Login Ticket Request (TRA)
        /// </summary>
        /// <param name="serviceId">ID del servicio a acceder (ej: "wsfe", "wsfev1")</param>
        /// <param name="expirationMinutes">Minutos de validez del ticket (por defecto 20)</param>
        /// <returns>XML del Login Ticket Request</returns>
        public string GenerateLoginTicketRequest(string serviceId, int expirationMinutes = 20)
        {
            // Validar que el serviceId no exceda 35 caracteres
            if (serviceId.Length > 35)
            {
                throw new ArgumentException($"El serviceId '{serviceId}' excede los 35 caracteres permitidos (actual: {serviceId.Length})");
            }

            var uniqueId = DateTime.Now.ToString("yyMMddHHmmss");
            var generationTime = DateTime.Now;
            var expirationTime = generationTime.AddMinutes(expirationMinutes);

            // IMPORTANTE: No usar indentación ni saltos de línea
            var xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.Append("<loginTicketRequest version=\"1.0\">");
            xml.Append("<header>");
            xml.Append($"<uniqueId>{uniqueId}</uniqueId>");
            xml.Append($"<generationTime>{generationTime:yyyy-MM-ddTHH:mm:ss}</generationTime>");
            xml.Append($"<expirationTime>{expirationTime:yyyy-MM-ddTHH:mm:ss}</expirationTime>");
            xml.Append("</header>");
            xml.Append($"<service>{serviceId}</service>");
            xml.Append("</loginTicketRequest>");

            var xmlString = xml.ToString();

            // Validar que el XML sea válido antes de devolverlo
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlString);
            }
            catch (XmlException ex)
            {
                throw new AfipWsaaException($"Error al generar XML del TRA: {ex.Message}. XML generado: {xmlString}", ex);
            }

            // Validar fechas
            if (expirationTime <= generationTime)
            {
                throw new AfipWsaaException("La fecha de expiración debe ser posterior a la de generación");
            }

            Console.WriteLine($"=== VALIDACIÓN TRA ===");
            Console.WriteLine($"Service ID: '{serviceId}' (longitud: {serviceId.Length})");
            Console.WriteLine($"Unique ID: {uniqueId}");
            Console.WriteLine($"Generation Time: {generationTime:yyyy-MM-ddTHH:mm:ss}");
            Console.WriteLine($"Expiration Time: {expirationTime:yyyy-MM-ddTHH:mm:ss}");
            Console.WriteLine($"Ventana de tiempo: {expirationMinutes} minutos");
            Console.WriteLine($"XML válido: ✓");

            return xmlString;
        }

        /// <summary>
        /// Firma el Login Ticket Request usando el certificado
        /// </summary>
        /// <param name="loginTicketRequest">XML del Login Ticket Request</param>
        /// <returns>Mensaje CMS firmado en Base64</returns>
        public string SignLoginTicketRequest(string loginTicketRequest)
        {
            // Cargar el certificado - asegurar que la clave privada sea accesible
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            var certificate = new X509Certificate2(_certificatePath, _certificatePassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057

            // Convertir el XML a bytes
            var encoding = Encoding.UTF8;
            byte[] messageBytes = encoding.GetBytes(loginTicketRequest);

            // Crear el contenido a firmar
            var contentInfo = new ContentInfo(messageBytes);
            var signedCms = new SignedCms(contentInfo);

            // Configurar el firmante
            var cmsSigner = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate)
            {
                IncludeOption = X509IncludeOption.EndCertOnly
            };

            // Firmar el contenido
            signedCms.ComputeSignature(cmsSigner);

            // Codificar a Base64
            byte[] encodedSignedCms = signedCms.Encode();
            var base64 = Convert.ToBase64String(encodedSignedCms);

            // --- Validación adicional (solo para depuración) ---
            try
            {
                // Intentar decodificar y validar la firma localmente para detectar problemas antes de enviar
                var verifier = new SignedCms();
                verifier.Decode(Convert.FromBase64String(base64));
                // Verificar la firma; no lanzamos si falla aquí, capturamos y convertimos en excepción descriptiva
                verifier.CheckSignature(verifySignatureOnly: true);
            }
            catch (Exception ex)
            {
                // Lanzar una excepción clara para que el llamador pueda registrar/inspeccionar
                throw new AfipWsaaException("Fallo al validar el CMS firmado localmente antes de enviar: " + ex.Message, ex);
            }

            return base64;
        }

        /// <summary>
        /// Llama al servicio WSAA LoginCms
        /// </summary>
        /// <param name="signedCms">Mensaje CMS firmado en Base64</param>
        /// <returns>Login Ticket Response de AFIP</returns>
        public async Task<WsaaResponse> CallLoginCmsAsync(string signedCms)
        {
            // Limpiar el CMS firmado para eliminar espacios, saltos de línea y retornos de carro
            if (string.IsNullOrEmpty(signedCms))
            {
                throw new AfipWsaaException("El CMS firmado está vacío o es nulo");
            }

            var cleanSignedCms = signedCms.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\t", "");
            
            // Validar que el CMS no esté vacío después de la limpieza
            if (string.IsNullOrEmpty(cleanSignedCms))
            {
                throw new AfipWsaaException("El CMS firmado está vacío o contiene solo espacios en blanco");
            }

            Console.WriteLine($"=== LIMPIEZA CMS ===");
            Console.WriteLine($"CMS original longitud: {signedCms.Length}");
            Console.WriteLine($"CMS limpio longitud: {cleanSignedCms.Length}");
            if (signedCms.Length != cleanSignedCms.Length)
            {
                Console.WriteLine("⚠️  Se encontraron y eliminaron espacios/saltos de línea en el CMS");
            }
            Console.WriteLine("=== FIN LIMPIEZA CMS ===");

            // Crear el sobre SOAP
            var soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:wsaa=""http://wsaa.view.sua.dvadac.desein.afip.gov""> 
   <soapenv:Header/> 
   <soapenv:Body>
      <wsaa:loginCms>
         <wsaa:in0>{cleanSignedCms}</wsaa:in0>
      </wsaa:loginCms>
   </soapenv:Body>
</soapenv:Envelope>";

            // *** LOGGING A CONSOLA - SOAP ENVELOPE COMPLETO ***
            Console.WriteLine("=== SOAP ENVELOPE ENVIADO A AFIP ===");
            Console.WriteLine(soapEnvelope);
            Console.WriteLine("=== FIN SOAP ENVELOPE ===");

            // Escribir petición a archivo temporal para depuración local (no en producción sin permiso)
            try
            {
                var tempReq = Path.Combine(Path.GetTempPath(), $"afip_request_{DateTime.Now:yyyyMMddHHmmss}.xml");
                File.WriteAllText(tempReq, soapEnvelope, Encoding.UTF8);
                Console.WriteLine($"=== ARCHIVO TEMPORAL CREADO: {tempReq} ===");
            }
            catch (Exception)
            {
                // no bloquear en caso de fallo de escritura
            }

            // Crear la petición HTTP
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "\"\"");

            try
            {
                // Enviar la petición
                var response = await _httpClient.PostAsync(_wsaaUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // *** LOGGING A CONSOLA - RESPUESTA DE AFIP ***
                Console.WriteLine("=== RESPUESTA RECIBIDA DE AFIP ===");
                Console.WriteLine($"HTTP Status: {response.StatusCode}");
                Console.WriteLine($"Content-Length: {responseContent.Length}");
                Console.WriteLine("Contenido:");
                Console.WriteLine(responseContent);
                Console.WriteLine("=== FIN RESPUESTA AFIP ===");

                // Guardar respuesta en archivo temporal para inspección si viene con Fault
                try
                {
                    var tempResp = Path.Combine(Path.GetTempPath(), $"afip_response_{DateTime.Now:yyyyMMddHHmmss}.xml");
                    File.WriteAllText(tempResp, responseContent, Encoding.UTF8);
                    Console.WriteLine($"=== ARCHIVO RESPUESTA CREADO: {tempResp} ===");
                }
                catch (Exception)
                {
                    // no bloquear en caso de fallo de escritura
                }

                // Parsear la respuesta
                return ParseSoapResponse(responseContent);
            }
            catch (HttpRequestException ex)
            {
                throw new AfipWsaaException("Error al comunicarse con el servidor AFIP", ex);
            }
        }

        /// <summary>
        /// Proceso completo: genera TRA, firma y obtiene el Login Ticket
        /// </summary>
        /// <param name="serviceId">ID del servicio a acceder</param>
        /// <returns>Login Ticket Response de AFIP</returns>
        public async Task<WsaaResponse> LoginAsync(string serviceId)
        {
            // Generar el Login Ticket Request
            var loginTicketRequest = GenerateLoginTicketRequest(serviceId);

            // *** LOGGING A CONSOLA - TRA GENERADO ***
            Console.WriteLine("=== TRA (Login Ticket Request) GENERADO ===");
            Console.WriteLine(loginTicketRequest);
            Console.WriteLine("=== FIN TRA ===");

            // Firmar el Login Ticket Request
            var signedCms = SignLoginTicketRequest(loginTicketRequest);

            // *** LOGGING A CONSOLA - CMS FIRMADO ***
            Console.WriteLine("=== CMS FIRMADO (BASE64) ===");
            Console.WriteLine($"Longitud: {signedCms.Length} caracteres");
            Console.WriteLine($"Primeros 100 chars: {signedCms.Substring(0, Math.Min(100, signedCms.Length))}...");
            Console.WriteLine("=== FIN CMS ===");

            // Llamar al servicio WSAA
            return await CallLoginCmsAsync(signedCms);
        }

        /// <summary>
        /// Parsea la respuesta SOAP
        /// </summary>
        private WsaaResponse ParseSoapResponse(string soapResponse)
        {
            Console.WriteLine("=== INICIANDO PARSING DE RESPUESTA SOAP ===");
            Console.WriteLine($"Longitud del soapResponse: {soapResponse?.Length ?? 0} caracteres");
            
            // *** ENVIAR SOAP RESPONSE COMPLETO A CONSOLA COMO TEXTO PLANO ***
            Console.WriteLine("=== SOAP RESPONSE COMPLETO (TEXTO PLANO) ===");
            Console.WriteLine(soapResponse ?? "NULL");
            Console.WriteLine("=== FIN SOAP RESPONSE ===");

            // Verificar que el soapResponse no esté vacío
            if (string.IsNullOrEmpty(soapResponse))
            {
                throw new AfipWsaaException("El soapResponse está vacío o es nulo");
            }

            XDocument xDoc;
            try
            {
                Console.WriteLine("=== INTENTANDO PARSEAR XML ===");
                xDoc = XDocument.Parse(soapResponse);
                Console.WriteLine("✅ XML parseado correctamente");
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"❌ ERROR AL PARSEAR XML: {ex.Message}");
                Console.WriteLine($"Línea: {ex.LineNumber}, Posición: {ex.LinePosition}");
                throw new AfipWsaaException($"Error al parsear XML de respuesta SOAP: {ex.Message} (Línea: {ex.LineNumber}, Posición: {ex.LinePosition})", ex);
            }

            XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
            Console.WriteLine($"=== BUSCANDO ELEMENTOS SOAP ===");
            Console.WriteLine($"Namespace SOAP: {soapNs}");
            Console.WriteLine($"Root element: {xDoc.Root?.Name}");

            // Verificar si es un SOAP Fault
            var soapBody = xDoc.Root?.Element(soapNs + "Body");
            Console.WriteLine($"SOAP Body encontrado: {soapBody != null}");
            
            var fault = soapBody?.Element(soapNs + "Fault");
            Console.WriteLine($"SOAP Fault encontrado: {fault != null}");
            
            if (fault != null)
            {
                Console.WriteLine("=== PROCESANDO SOAP FAULT ===");
                var faultCode = fault.Element("faultcode")?.Value ?? "Unknown";
                var faultString = fault.Element("faultstring")?.Value ?? "Unknown error";
                var exceptionName = fault.Element("detail")?.Element(XName.Get("exceptionName", "http://xml.apache.org/axis/"))?.Value;
                var hostname = fault.Element("detail")?.Element(XName.Get("hostname", "http://xml.apache.org/axis/"))?.Value;

                Console.WriteLine($"FaultCode: {faultCode}");
                Console.WriteLine($"FaultString: {faultString}");
                Console.WriteLine($"ExceptionName: {exceptionName}");
                Console.WriteLine($"Hostname: {hostname}");

                throw new AfipSoapFaultException(faultCode, faultString, exceptionName, hostname);
            }

            // Extraer el loginCmsReturn
            Console.WriteLine("=== BUSCANDO loginCmsReturn ===");
            XNamespace wsaaNs = "http://wsaa.view.sua.dvadac.desein.afip.gov";
            Console.WriteLine($"Namespace WSAA: {wsaaNs}");
            
            var loginCmsResponse = soapBody?.Element(wsaaNs + "loginCmsResponse");
            Console.WriteLine($"loginCmsResponse encontrado: {loginCmsResponse != null}");
            
            var loginCmsReturn = loginCmsResponse?.Element(wsaaNs + "loginCmsReturn")?.Value;
            Console.WriteLine($"loginCmsReturn encontrado: {!string.IsNullOrEmpty(loginCmsReturn)}");
            Console.WriteLine($"loginCmsReturn longitud: {loginCmsReturn?.Length ?? 0}");

            if (string.IsNullOrEmpty(loginCmsReturn))
            {
                Console.WriteLine("❌ ERROR: No se pudo obtener el loginCmsReturn");
                // Mostrar estructura del Body para depuración
                Console.WriteLine("=== ESTRUCTURA DEL SOAP BODY ===");
                if (soapBody != null)
                {
                    foreach (var element in soapBody.Elements())
                    {
                        Console.WriteLine($"Elemento encontrado: {element.Name}");
                    }
                }
                throw new AfipWsaaException("No se pudo obtener el loginCmsReturn de la respuesta SOAP");
            }

            Console.WriteLine("=== LOGIN CMS RETURN OBTENIDO ===");
            Console.WriteLine(loginCmsReturn);
            Console.WriteLine("=== FIN LOGIN CMS RETURN ===");

            // Parsear el Login Ticket Response
            XDocument ticketDoc;
            try
            {
                Console.WriteLine("=== PARSEANDO LOGIN TICKET RESPONSE ===");
                ticketDoc = XDocument.Parse(loginCmsReturn);
                Console.WriteLine("✅ Login Ticket Response parseado correctamente");
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"❌ ERROR AL PARSEAR LOGIN TICKET RESPONSE: {ex.Message}");
                throw new AfipWsaaException($"Error al parsear Login Ticket Response: {ex.Message}", ex);
            }

            var credentials = ticketDoc.Root?.Element("credentials");
            Console.WriteLine($"Credentials encontrado: {credentials != null}");

            var expirationTimeStr = ticketDoc.Root?.Element("header")?.Element("expirationTime")?.Value ?? DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            Console.WriteLine($"ExpirationTime: {expirationTimeStr}");

            var token = credentials?.Element("token")?.Value ?? string.Empty;
            var sign = credentials?.Element("sign")?.Value ?? string.Empty;

            Console.WriteLine($"Token longitud: {token.Length}");
            Console.WriteLine($"Sign longitud: {sign.Length}");
            Console.WriteLine("=== FIN PARSING EXITOSO ===");

            return new WsaaResponse
            {
                Token = token,
                Sign = sign,
                ExpirationTime = DateTime.Parse(expirationTimeStr, CultureInfo.InvariantCulture),
                RawXml = loginCmsReturn
            };
        }
    }

    /// <summary>
    /// Respuesta del servicio WSAA
    /// </summary>
    public class WsaaResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Sign { get; set; } = string.Empty;
        public DateTime ExpirationTime { get; set; }
        public string RawXml { get; set; } = string.Empty;

        public bool IsExpired => DateTime.UtcNow >= ExpirationTime;
    }

    /// <summary>
    /// Excepción general del cliente WSAA
    /// </summary>
    public class AfipWsaaException : Exception
    {
        public AfipWsaaException(string message) : base(message) { }
        public AfipWsaaException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Excepción específica para SOAP Faults de AFIP
    /// </summary>
    public class AfipSoapFaultException : AfipWsaaException
    {
        public string FaultCode { get; }
        public string FaultString { get; }
        public string? ExceptionName { get; }
        public string? Hostname { get; }

        public AfipSoapFaultException(string faultCode, string faultString, string? exceptionName, string? hostname)
            : base($"SOAP Fault: [{faultCode}] {faultString}")
        {
            FaultCode = faultCode;
            FaultString = faultString;
            ExceptionName = exceptionName;
            Hostname = hostname;
        }
    }
}
