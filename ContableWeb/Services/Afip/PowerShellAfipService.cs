using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace ContableWeb.Services.Afip
{
    /// <summary>
    /// Servicio para ejecutar autenticación AFIP usando el script PowerShell
    /// </summary>
    public class PowerShellAfipService
    {
        private readonly string _scriptPath;
        private readonly string _certificatePath;
        private readonly string _password;
        private readonly ILogger<PowerShellAfipService> _logger;

        public PowerShellAfipService(IConfiguration configuration, ILogger<PowerShellAfipService> logger)
        {
            _scriptPath = configuration["Afip:PowerShellScriptPath"] ?? "wsaa-cliente-noopenssl.ps1";
            _certificatePath = configuration["Afip:CertificatePath"] ?? "W:\\cert.p12";
            _password = configuration["Afip:CertificatePassword"] ?? "261194";
            _logger = logger;
        }

        /// <summary>
        /// Ejecuta el script PowerShell para obtener autenticación AFIP
        /// </summary>
        /// <param name="serviceId">ID del servicio AFIP (ej: "wsfe", "ws_sr_constancia_inscripcion")</param>
        /// <returns>Respuesta de autenticación AFIP</returns>
        public async Task<AfipAuthResult> ExecuteAfipAuthenticationAsync(string serviceId = "wsfe")
        {
            try
            {
                Console.WriteLine("=== INICIANDO AUTENTICACIÓN AFIP CON POWERSHELL ===");
                Console.WriteLine($"Script: {_scriptPath}");
                Console.WriteLine($"Certificado: {_certificatePath}");
                Console.WriteLine($"Servicio: {serviceId}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = BuildPowerShellArguments(serviceId),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(_scriptPath) ?? Environment.CurrentDirectory
                };

                Console.WriteLine($"Comando completo: {startInfo.FileName} {startInfo.Arguments}");

                using var process = new Process { StartInfo = startInfo };
                
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        outputBuilder.AppendLine(args.Data);
                        Console.WriteLine($"PS Output: {args.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        errorBuilder.AppendLine(args.Data);
                        Console.WriteLine($"PS Error: {args.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                var output = outputBuilder.ToString();
                var error = errorBuilder.ToString();

                Console.WriteLine($"PowerShell Exit Code: {process.ExitCode}");
                Console.WriteLine("=== OUTPUT COMPLETO DEL POWERSHELL ===");
                Console.WriteLine(output);
                Console.WriteLine("=== FIN OUTPUT POWERSHELL ===");

                if (process.ExitCode != 0)
                {
                    var errorMsg = $"PowerShell script failed with exit code {process.ExitCode}. Error: {error}";
                    Console.WriteLine($"ERROR: {errorMsg}");
                    
                    // Si hay un SOAP Fault, extraerlo del output
                    var soapFault = ExtractSoapFaultFromOutput(output + error);
                    if (soapFault != null)
                    {
                        return AfipAuthResult.Failure(soapFault);
                    }
                    
                    return AfipAuthResult.Failure(new ErrorResponse
                    {
                        Message = errorMsg,
                        ExceptionType = "PowerShellExecutionException",
                        StackTrace = error
                    });
                }

                // Extraer el Login Ticket del output
                var loginTicketXml = ExtractLoginTicketFromOutput(output);
                if (string.IsNullOrEmpty(loginTicketXml))
                {
                    return AfipAuthResult.Failure(new ErrorResponse
                    {
                        Message = "No se pudo extraer el Login Ticket del output de PowerShell",
                        ExceptionType = "LoginTicketExtractionException",
                        StackTrace = output
                    });
                }

                // Parsear el Login Ticket
                var wsaaResponse = ParseLoginTicketXml(loginTicketXml);
                Console.WriteLine("=== AUTENTICACIÓN POWERSHELL EXITOSA ===");
                return AfipAuthResult.Success(wsaaResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepción ejecutando PowerShell: {ex.Message}");
                _logger.LogError(ex, "Error ejecutando script PowerShell AFIP");
                
                return AfipAuthResult.Failure(new ErrorResponse
                {
                    Message = ex.Message,
                    ExceptionType = ex.GetType().FullName,
                    StackTrace = ex.ToString()
                });
            }
        }

        private string BuildPowerShellArguments(string serviceId)
        {
            // Construir los argumentos para el script PowerShell
            var args = new StringBuilder();
            args.Append("-ExecutionPolicy Bypass ");
            args.Append($"-File \"{_scriptPath}\" ");
            args.Append($"-Certificado \"{_certificatePath}\" ");
            args.Append($"-Password \"{_password}\" ");
            args.Append($"-ServicioId \"{serviceId}\" ");
            args.Append("-WsaaWsdl \"https://wsaahomo.afip.gov.ar/ws/services/LoginCms?wsdl\"");
            
            return args.ToString();
        }

        private string? ExtractLoginTicketFromOutput(string output)
        {
            try
            {
                Console.WriteLine("=== EXTRAYENDO LOGIN TICKET DEL OUTPUT ===");
                
                // El script PowerShell devuelve el XML directamente al final
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var xmlLines = new List<string>();
                bool foundXmlStart = false;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    // Buscar el inicio del XML del Login Ticket
                    if (trimmedLine.StartsWith("<?xml") || trimmedLine.StartsWith("<loginTicketResponse"))
                    {
                        foundXmlStart = true;
                        xmlLines.Add(trimmedLine);
                    }
                    else if (foundXmlStart && (trimmedLine.StartsWith("<") || xmlLines.Count > 0))
                    {
                        xmlLines.Add(trimmedLine);
                        
                        // Si encontramos el cierre del elemento raíz, terminamos
                        if (trimmedLine.EndsWith("</loginTicketResponse>"))
                        {
                            break;
                        }
                    }
                }

                var loginTicketXml = string.Join("", xmlLines);
                
                Console.WriteLine($"XML extraído (longitud: {loginTicketXml.Length}):");
                Console.WriteLine(loginTicketXml);
                Console.WriteLine("=== FIN EXTRACCIÓN LOGIN TICKET ===");

                return string.IsNullOrEmpty(loginTicketXml) ? null : loginTicketXml;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extrayendo Login Ticket: {ex.Message}");
                return null;
            }
        }

        private ErrorResponse? ExtractSoapFaultFromOutput(string output)
        {
            try
            {
                // Buscar patrones de SOAP Fault en el output
                if (output.Contains("SOAP FAULT") || output.Contains("SOAP Fault"))
                {
                    var lines = output.Split('\n');
                    string? faultCode = null;
                    string? faultString = null;
                    string? exceptionName = null;
                    string? hostname = null;

                    foreach (var line in lines)
                    {
                        if (line.Contains("Código:"))
                            faultCode = ExtractValueAfterColon(line);
                        else if (line.Contains("Mensaje:"))
                            faultString = ExtractValueAfterColon(line);
                        else if (line.Contains("Excepción:"))
                            exceptionName = ExtractValueAfterColon(line);
                        else if (line.Contains("Servidor:"))
                            hostname = ExtractValueAfterColon(line);
                    }

                    if (!string.IsNullOrEmpty(faultCode) || !string.IsNullOrEmpty(faultString))
                    {
                        return new ErrorResponse
                        {
                            Message = faultString ?? "SOAP Fault sin mensaje",
                            ExceptionType = "AfipSoapFaultException",
                            FaultCode = faultCode,
                            FaultString = faultString,
                            ExceptionName = exceptionName,
                            Hostname = hostname
                        };
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extrayendo SOAP Fault: {ex.Message}");
                return null;
            }
        }

        private string? ExtractValueAfterColon(string line)
        {
            var colonIndex = line.IndexOf(':');
            return colonIndex >= 0 && colonIndex < line.Length - 1 
                ? line.Substring(colonIndex + 1).Trim() 
                : null;
        }

        private WsaaResponse ParseLoginTicketXml(string loginTicketXml)
        {
            try
            {
                Console.WriteLine("=== PARSEANDO LOGIN TICKET XML ===");
                Console.WriteLine($"XML a parsear: {loginTicketXml}");

                if (string.IsNullOrWhiteSpace(loginTicketXml))
                    throw new ArgumentException("Login Ticket XML está vacío");

                var doc = XDocument.Parse(loginTicketXml);
                
                // Buscar el elemento credentials
                var credentials = doc.Descendants("credentials").FirstOrDefault();
                if (credentials == null)
                    throw new Exception("No se encontró el elemento 'credentials' en el Login Ticket");

                // Extraer token y sign
                var token = credentials.Element("token")?.Value;
                var sign = credentials.Element("sign")?.Value;
                
                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(sign))
                    throw new Exception("Token o Sign faltantes en el Login Ticket");

                // Buscar fecha de expiración
                var header = doc.Descendants("header").FirstOrDefault();
                var expirationTimeStr = header?.Element("expirationTime")?.Value;
                
                DateTime expirationTime;
                if (!string.IsNullOrEmpty(expirationTimeStr))
                {
                    if (!DateTime.TryParse(expirationTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out expirationTime))
                    {
                        // Si falla el parsing, usar una fecha por defecto
                        expirationTime = DateTime.UtcNow.AddMinutes(10);
                    }
                }
                else
                {
                    expirationTime = DateTime.UtcNow.AddMinutes(10);
                }

                Console.WriteLine("=== LOGIN TICKET PARSEADO EXITOSAMENTE ===");
                Console.WriteLine($"Token length: {token.Length}");
                Console.WriteLine($"Sign length: {sign.Length}");
                Console.WriteLine($"Expiration: {expirationTime}");

                return new WsaaResponse
                {
                    Token = token,
                    Sign = sign,
                    ExpirationTime = expirationTime,
                    RawXml = loginTicketXml
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parseando Login Ticket XML: {ex.Message}");
                Console.WriteLine($"XML recibido: {loginTicketXml}");
                throw new Exception($"Error parseando Login Ticket: {ex.Message}", ex);
            }
        }
    }
}
