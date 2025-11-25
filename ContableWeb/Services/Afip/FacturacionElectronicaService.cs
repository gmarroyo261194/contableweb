using ContableWeb.Services.Afip.WSFEv1;
using static ContableWeb.Services.Afip.WSFEv1.CondicionesIVA;

namespace ContableWeb.Services.Afip
{
    /// <summary>
    /// Servicio de alto nivel para generar facturas electrónicas usando WSFEv1
    /// </summary>
    public interface IFacturacionElectronicaService
    {
        /// <summary>
        /// Genera una factura tipo C (Consumidor Final)
        /// </summary>
        Task<FacturaElectronicaResult> GenerarFacturaTipoCAsync(DatosFacturaTipoC datos);

        /// <summary>
        /// Obtiene el próximo número de comprobante disponible
        /// </summary>
        Task<int> ObtenerProximoNumeroComprobanteAsync(int puntoVenta, int tipoComprobante);

        /// <summary>
        /// Verifica la conexión con AFIP WSFEv1
        /// </summary>
        Task<bool> VerificarConexionAsync();

        /// <summary>
        /// Obtiene los tipos de comprobante disponibles
        /// </summary>
        Task<List<TipoComprobanteInfo>> ObtenerTiposComprobanteAsync();

        /// <summary>
        /// Obtiene las condiciones frente al IVA disponibles
        /// </summary>
        Task<List<CondicionIvaInfo>> ObtenerCondicionesIvaAsync();
    }

    /// <summary>
    /// Implementación del servicio de facturación electrónica
    /// </summary>
    public class FacturacionElectronicaService : IFacturacionElectronicaService
    {
        private readonly WsfEv1Client _wsfeClient;
        private readonly IAfipTokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FacturacionElectronicaService> _logger;
        
        private long _cuitEmisor;
        private bool _isProduction;

        public FacturacionElectronicaService(
            IAfipTokenService tokenService,
            IConfiguration configuration,
            ILogger<FacturacionElectronicaService> logger)
        {
            _tokenService = tokenService;
            _configuration = configuration;
            _logger = logger;
            
            _isProduction = _configuration.GetValue<bool>("Afip:IsProduction", false);
            _cuitEmisor = _configuration.GetValue<long>("Afip:CuitEmisor", 20262367429);
            
            _wsfeClient = new WsfEv1Client(_isProduction);
        }

        /// <summary>
        /// Genera una factura tipo C (Consumidor Final)
        /// </summary>
        public async Task<FacturaElectronicaResult> GenerarFacturaTipoCAsync(DatosFacturaTipoC datos)
        {
            try
            {
                Console.WriteLine("=== GENERANDO FACTURA TIPO C ===");
                Console.WriteLine($"Cliente: {datos.Cliente.Nombre}");
                Console.WriteLine($"Total: ${datos.ImporteTotal:F2}");

                // 1. Obtener token AFIP válido
                var token = await _tokenService.GetValidTokenAsync("wsfe");
                var auth = new FEAuthRequest
                {
                    Token = token.Token,
                    Sign = token.Sign,
                    Cuit = _cuitEmisor
                };

                // 2. Obtener próximo número de comprobante
                var proximoNumero = await ObtenerProximoNumeroComprobanteAsync(datos.PuntoVenta, TiposComprobante.FacturaC);
                
                Console.WriteLine($"✅ Número de comprobante obtenido: {proximoNumero}");
                Console.WriteLine($"Fecha del comprobante: {DateTime.Now:yyyyMMdd}");
                
                // Validación adicional del número de comprobante
                if (proximoNumero <= 0)
                {
                    throw new ArgumentException($"Número de comprobante inválido: {proximoNumero}. Debe ser mayor a 0.");
                }

                // 3. Preparar request para AFIP
                var request = CrearRequestFacturaTipoC(datos, proximoNumero);

                // 4. Solicitar CAE a AFIP
                var response = await _wsfeClient.SolicitarCAEAsync(auth, request);

                // 5. Procesar respuesta
                return ProcesarRespuestaCAE(response, datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando factura tipo C");
                return new FacturaElectronicaResult
                {
                    Exitoso = false,
                    Error = $"Error generando factura: {ex.Message}",
                    Excepcion = ex
                };
            }
        }

        /// <summary>
        /// Obtiene el próximo número de comprobante disponible
        /// </summary>
        public async Task<int> ObtenerProximoNumeroComprobanteAsync(int puntoVenta, int tipoComprobante)
        {
            try
            {
                Console.WriteLine($"=== OBTENIENDO PRÓXIMO NÚMERO DE COMPROBANTE ===");
                Console.WriteLine($"Punto de Venta: {puntoVenta}");
                Console.WriteLine($"Tipo Comprobante: {tipoComprobante} (Factura C)");
                Console.WriteLine($"CUIT Emisor: {_cuitEmisor}");

                var token = await _tokenService.GetValidTokenAsync("wsfe");
                var auth = new FEAuthRequest
                {
                    Token = token.Token,
                    Sign = token.Sign,
                    Cuit = _cuitEmisor
                };

                Console.WriteLine($"Token obtenido correctamente (longitud: {token.Token.Length})");

                var response = await _wsfeClient.ObtenerUltimoComprobanteAsync(auth, puntoVenta, tipoComprobante);
                
                Console.WriteLine($"Respuesta de AFIP recibida:");
                Console.WriteLine($"  - Último comprobante autorizado: {response.CbteNro}");
                Console.WriteLine($"  - Punto de venta consultado: {response.PtoVta}");
                Console.WriteLine($"  - Tipo comprobante consultado: {response.CbteTipo}");

                // Verificar si hay errores en la respuesta
                if (response.Errors != null && response.Errors.Length > 0)
                {
                    Console.WriteLine("⚠️ Errores en respuesta de FECompUltimoAutorizado:");
                    foreach (var error in response.Errors)
                    {
                        Console.WriteLine($"  Error {error.Code}: {error.Msg}");
                    }
                    throw new Exception($"Error de AFIP: {string.Join(", ", response.Errors.Select(e => $"{e.Code}: {e.Msg}"))}");
                }

                // El próximo número es el último + 1
                var proximoNumero = response.CbteNro + 1;
                Console.WriteLine($"✅ Próximo número de comprobante: {proximoNumero}");
                
                return proximoNumero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error obteniendo próximo número de comprobante: {ex.Message}");
                _logger.LogError(ex, "Error obteniendo próximo número de comprobante para PV:{PuntoVenta} Tipo:{TipoComprobante}", puntoVenta, tipoComprobante);
                throw;
            }
        }

        /// <summary>
        /// Verifica la conexión con AFIP WSFEv1
        /// </summary>
        public async Task<bool> VerificarConexionAsync()
        {
            try
            {
                var response = await _wsfeClient.TestConexionAsync();
                return !string.IsNullOrEmpty(response.AppServer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando conexión con WSFEv1");
                return false;
            }
        }

        /// <summary>
        /// Obtiene los tipos de comprobante disponibles
        /// </summary>
        public async Task<List<TipoComprobanteInfo>> ObtenerTiposComprobanteAsync()
        {
            try
            {
                var token = await _tokenService.GetValidTokenAsync("wsfe");
                var auth = new FEAuthRequest
                {
                    Token = token.Token,
                    Sign = token.Sign,
                    Cuit = _cuitEmisor
                };

                var response = await _wsfeClient.ObtenerTiposComprobanteAsync(auth);
                
                if (response.ResultGet != null)
                {
                    return response.ResultGet.Select(ct => new TipoComprobanteInfo
                    {
                        Id = ct.Id,
                        Descripcion = ct.Desc,
                        FechaDesde = ct.FchDesde,
                        FechaHasta = ct.FchHasta
                    }).ToList();
                }

                return new List<TipoComprobanteInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo tipos de comprobante");
                throw;
            }
        }

        /// <summary>
        /// Obtiene las condiciones frente al IVA disponibles
        /// </summary>
        public async Task<List<CondicionIvaInfo>> ObtenerCondicionesIvaAsync()
        {
            try
            {
                var token = await _tokenService.GetValidTokenAsync("wsfe");
                var auth = new FEAuthRequest
                {
                    Token = token.Token,
                    Sign = token.Sign,
                    Cuit = _cuitEmisor
                };

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo condiciones IVA");
                throw;
            }
        }

        private FECAERequest CrearRequestFacturaTipoC(DatosFacturaTipoC datos, int numeroComprobante)
        {
            Console.WriteLine($"=== CREANDO REQUEST FACTURA TIPO C ===");
            Console.WriteLine($"Importe Total: ${datos.ImporteTotal:F2}");
            Console.WriteLine($"Número de comprobante: {numeroComprobante}");
            
            // Para facturas tipo C: el importe total es el importe final (SIN IVA discriminado)
            var importeTotal = datos.ImporteTotal;
            
            Console.WriteLine($"Factura Tipo C - Importe Total: ${importeTotal:F2}");
            Console.WriteLine("Nota: Las facturas C NO discriminan IVA - el total es el importe final");

            // Validaciones
            if (datos.ImporteTotal <= 0)
                throw new ArgumentException("El importe total debe ser mayor a cero");
                
            if (numeroComprobante <= 0)
                throw new ArgumentException("El número de comprobante debe ser mayor a cero");
                
            if (datos.PuntoVenta <= 0 || datos.PuntoVenta > 9999)
                throw new ArgumentException("El punto de venta debe estar entre 1 y 9999");

            return new FECAERequest
            {
                FeCabReq = new FECAECabRequest
                {
                    CantReg = 1,
                    PtoVta = datos.PuntoVenta,
                    CbteTipo = TiposComprobante.FacturaC
                },
                FeDetReq = new[]
                {
                    new FECAEDetRequest
                    {
                        Concepto = datos.EsServicio ? ConceptosComprobante.Servicios : ConceptosComprobante.Productos,
                        DocTipo = datos.Cliente.TipoDocumento,
                        DocNro = datos.Cliente.NumeroDocumento,
                        CbteDesde = numeroComprobante,
                        CbteHasta = numeroComprobante,
                        CbteFch = DateTime.Now.ToString("yyyyMMdd"),
                        ImpTotal = importeTotal,
                        ImpTotConc = 0, // Sin conceptos no gravados
                        ImpNeto = importeTotal, // En factura C, el neto es el total (sin discriminar IVA)
                        ImpOpEx = 0, // Sin operaciones exentas
                        ImpIVA = 0, // Factura C NO discrimina IVA
                        ImpTrib = 0, // Sin otros tributos
                        MonId = "PES",
                        MonCotiz = 1,
                        FchServDesde = datos.EsServicio ? datos.FechaServicioDesde?.ToString("yyyyMMdd") : null,
                        FchServHasta = datos.EsServicio ? datos.FechaServicioHasta?.ToString("yyyyMMdd") : null,
                        FchVtoPago = datos.FechaVencimiento?.ToString("yyyyMMdd"),
                        Iva = null, // Factura C NO lleva discriminación de IVA
                        CondicionFrenteIva = datos.Cliente.CondicionIva // OBLIGATORIO según RG 5616
                    }
                }
            };
        }

        private FacturaElectronicaResult ProcesarRespuestaCAE(FECAESolicitarResponse response, DatosFacturaTipoC datos)
        {
            var result = new FacturaElectronicaResult();

            if (response?.FECAESolicitarResult?.FeCabResp != null)
            {
                var cabResp = response.FECAESolicitarResult.FeCabResp;
                var detResp = response.FECAESolicitarResult.FeDetResp?.FirstOrDefault();

                result.Exitoso = cabResp.Resultado == "A"; // A = Aprobado
                result.NumeroComprobante = detResp?.CbteDesde ?? 0;
                result.CAE = detResp?.CAE ?? string.Empty;
                result.FechaVencimientoCAE = detResp?.CAEFchVto ?? string.Empty;
                result.FechaProceso = cabResp.FchProceso;

                // Procesar errores si los hay
                if (response.FECAESolicitarResult.Errors != null)
                {
                    result.Errores = response.FECAESolicitarResult.Errors
                        .Select(e => $"Error {e.Code}: {e.Msg}")
                        .ToList();
                }

                // Procesar observaciones si las hay
                if (detResp?.Observaciones != null)
                {
                    result.Observaciones = detResp.Observaciones
                        .Select(o => $"Obs {o.Code}: {o.Msg}")
                        .ToList();
                }

                if (!result.Exitoso)
                {
                    result.Error = string.Join("; ", result.Errores);
                }

                Console.WriteLine($"=== RESULTADO FACTURA TIPO C ===");
                Console.WriteLine($"Exitoso: {result.Exitoso}");
                Console.WriteLine($"Número: {result.NumeroComprobante}");
                Console.WriteLine($"CAE: {result.CAE}");
                Console.WriteLine($"Vencimiento CAE: {result.FechaVencimientoCAE}");
                if (result.Errores.Any())
                {
                    Console.WriteLine($"Errores: {string.Join(", ", result.Errores)}");
                }
            }
            else
            {
                result.Exitoso = false;
                result.Error = "No se recibió respuesta válida de AFIP";
            }

            return result;
        }

        /// <summary>
        /// Verifica que el punto de venta esté habilitado para facturas tipo C
        /// </summary>
        private async Task ValidarPuntoVentaAsync(int puntoVenta)
        {
            try
            {
                Console.WriteLine($"=== VALIDANDO PUNTO DE VENTA {puntoVenta} ===");
                
                // Intentar obtener el último comprobante para validar que el punto de venta existe
                var ultimoComprobante = await ObtenerProximoNumeroComprobanteAsync(puntoVenta, TiposComprobante.FacturaC);
                
                Console.WriteLine($"✅ Punto de venta {puntoVenta} válido. Último comprobante: {ultimoComprobante - 1}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error validando punto de venta {puntoVenta}: {ex.Message}");
                throw new ArgumentException($"El punto de venta {puntoVenta} no está habilitado o no existe en AFIP: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Datos necesarios para generar una factura tipo C
    /// </summary>
    public class DatosFacturaTipoC
    {
        public int PuntoVenta { get; set; } = 1;
        public DatosCliente Cliente { get; set; } = new();
        public List<ItemFactura> Items { get; set; } = new();
        public decimal ImporteTotal => Items.Sum(i => i.ImporteTotal);
        public bool EsServicio { get; set; } = false;
        public DateTime? FechaServicioDesde { get; set; }
        public DateTime? FechaServicioHasta { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }

    /// <summary>
    /// Datos del cliente para factura tipo C
    /// </summary>
    public class DatosCliente
    {
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        // Campos obligatorios según RG 5616
        public int TipoDocumento { get; set; } = TiposDocumento.ConsumidorFinal; // 99 por defecto
        public long NumeroDocumento { get; set; } = 0; // 0 para consumidor final
        public int CondicionIva { get; set; } = ConsumidorFinal; // 5 = Consumidor Final (por defecto)
    }

    /// <summary>
    /// Item de una factura
    /// </summary>
    public class ItemFactura
    {
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; } = 1;
        public decimal PrecioUnitario { get; set; }
        public decimal ImporteTotal => Cantidad * PrecioUnitario;
        public bool GravaIVA { get; set; } = true;
        public decimal AlicuotaIVA { get; set; } = 21; // Porcentaje
    }

    /// <summary>
    /// Resultado de la generación de una factura electrónica
    /// </summary>
    public class FacturaElectronicaResult
    {
        public bool Exitoso { get; set; }
        public long NumeroComprobante { get; set; }
        public string CAE { get; set; } = string.Empty;
        public string FechaVencimientoCAE { get; set; } = string.Empty;
        public string FechaProceso { get; set; } = string.Empty;
        public List<string> Errores { get; set; } = new();
        public List<string> Observaciones { get; set; } = new();
        public string Error { get; set; } = string.Empty;
        public Exception? Excepcion { get; set; }
    }

    /// <summary>
    /// Información de un tipo de comprobante
    /// </summary>
    public class TipoComprobanteInfo
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string FechaDesde { get; set; } = string.Empty;
        public string FechaHasta { get; set; } = string.Empty;
    }

    /// <summary>
    /// Información sobre las condiciones IVA
    /// </summary>
    public class CondicionIvaInfo
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }
}
