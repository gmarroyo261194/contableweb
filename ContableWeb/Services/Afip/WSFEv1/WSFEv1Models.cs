using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace ContableWeb.Services.Afip.WSFEv1
{
    /// <summary>
    /// Request de autenticación para WSFEv1
    /// </summary>
    [XmlRoot("Auth")]
    public class FEAuthRequest
    {
        [XmlElement("Token")]
        public string Token { get; set; } = string.Empty;

        [XmlElement("Sign")]
        public string Sign { get; set; } = string.Empty;

        [XmlElement("Cuit")]
        public long Cuit { get; set; }
    }

    /// <summary>
    /// Request principal para solicitar CAE
    /// </summary>
    [XmlRoot("FeCAEReq")]
    public class FECAERequest
    {
        [XmlElement("FeCabReq")]
        public FECAECabRequest? FeCabReq { get; set; }

        [XmlArray("FeDetReq")]
        [XmlArrayItem("FECAEDetRequest")]
        public FECAEDetRequest[]? FeDetReq { get; set; }
    }

    /// <summary>
    /// Cabecera del request CAE
    /// </summary>
    public class FECAECabRequest
    {
        /// <summary>
        /// Cantidad de registros en el lote
        /// </summary>
        [XmlElement("CantReg")]
        public int CantReg { get; set; }

        /// <summary>
        /// Punto de venta
        /// </summary>
        [XmlElement("PtoVta")]
        public int PtoVta { get; set; }

        /// <summary>
        /// Tipo de comprobante (11 = Factura C)
        /// </summary>
        [XmlElement("CbteTipo")]
        public int CbteTipo { get; set; }
    }

    /// <summary>
    /// Detalle del comprobante para CAE
    /// </summary>
    public class FECAEDetRequest
    {
        /// <summary>
        /// Concepto del comprobante (1=Productos, 2=Servicios, 3=Productos y Servicios)
        /// </summary>
        [XmlElement("Concepto")]
        public int Concepto { get; set; }

        /// <summary>
        /// Tipo de documento del receptor (80=CUIT, 86=CUIL, 96=DNI, 99=Consumidor Final)
        /// </summary>
        [XmlElement("DocTipo")]
        public int DocTipo { get; set; }

        /// <summary>
        /// Número de documento del receptor
        /// </summary>
        [XmlElement("DocNro")]
        public long DocNro { get; set; }

        /// <summary>
        /// Número de comprobante desde
        /// </summary>
        [XmlElement("CbteDesde")]
        public long CbteDesde { get; set; }

        /// <summary>
        /// Número de comprobante hasta
        /// </summary>
        [XmlElement("CbteHasta")]
        public long CbteHasta { get; set; }

        /// <summary>
        /// Fecha del comprobante (YYYYMMDD)
        /// </summary>
        [XmlElement("CbteFch")]
        public string CbteFch { get; set; } = string.Empty;

        /// <summary>
        /// Importe total del comprobante
        /// </summary>
        [XmlElement("ImpTotal")]
        public decimal ImpTotal { get; set; }

        /// <summary>
        /// Importe neto gravado
        /// </summary>
        [XmlElement("ImpTotConc")]
        public decimal ImpTotConc { get; set; }

        /// <summary>
        /// Importe neto no gravado
        /// </summary>
        [XmlElement("ImpNeto")]
        public decimal ImpNeto { get; set; }

        /// <summary>
        /// Importe exento
        /// </summary>
        [XmlElement("ImpOpEx")]
        public decimal ImpOpEx { get; set; }

        /// <summary>
        /// Importe de IVA
        /// </summary>
        [XmlElement("ImpIVA")]
        public decimal ImpIVA { get; set; }

        /// <summary>
        /// Importe de tributos
        /// </summary>
        [XmlElement("ImpTrib")]
        public decimal ImpTrib { get; set; }

        /// <summary>
        /// Fecha de vencimiento para servicios (YYYYMMDD)
        /// </summary>
        [XmlElement("FchServDesde")]
        public string? FchServDesde { get; set; }

        /// <summary>
        /// Fecha de vencimiento para servicios (YYYYMMDD)
        /// </summary>
        [XmlElement("FchServHasta")]
        public string? FchServHasta { get; set; }

        /// <summary>
        /// Fecha de vencimiento de pago (YYYYMMDD)
        /// </summary>
        [XmlElement("FchVtoPago")]
        public string? FchVtoPago { get; set; }

        /// <summary>
        /// Código de moneda (PES = Peso Argentino)
        /// </summary>
        [XmlElement("MonId")]
        public string MonId { get; set; } = "PES";

        /// <summary>
        /// Cotización de la moneda
        /// </summary>
        [XmlElement("MonCotiz")]
        public decimal MonCotiz { get; set; } = 1;

        /// <summary>
        /// Array de alícuotas de IVA
        /// </summary>
        [XmlArray("Iva")]
        [XmlArrayItem("AlicIva")]
        public AlicIva[]? Iva { get; set; }

        /// <summary>
        /// Array de tributos
        /// </summary>
        [XmlArray("Tributos")]
        [XmlArrayItem("Tributo")]
        public Tributo[]? Tributos { get; set; }
        
        /// <summary>
        /// Condición frente al IVA del receptor (obligatorio según RG 5616)
        /// 1=IVA Responsable Inscripto, 4=IVA Sujeto Exento, 5=Consumidor Final, 6=Responsable Monotributo, etc.
        /// </summary>
        [XmlElement("CondicionFrenteIva")]
        public int CondicionFrenteIva { get; set; }
    }

    /// <summary>
    /// Alícuota de IVA
    /// </summary>
    public class AlicIva
    {
        /// <summary>
        /// Código de alícuota (3=0%, 4=10.5%, 5=21%, 6=27%, 8=5%, 9=2.5%)
        /// </summary>
        [XmlElement("Id")]
        public int Id { get; set; }

        /// <summary>
        /// Base imponible
        /// </summary>
        [XmlElement("BaseImp")]
        public decimal BaseImp { get; set; }

        /// <summary>
        /// Importe del IVA
        /// </summary>
        [XmlElement("Importe")]
        public decimal Importe { get; set; }
    }

    /// <summary>
    /// Tributo
    /// </summary>
    public class Tributo
    {
        /// <summary>
        /// Código de tributo
        /// </summary>
        [XmlElement("Id")]
        public short Id { get; set; }

        /// <summary>
        /// Descripción del tributo
        /// </summary>
        [XmlElement("Desc")]
        public string Desc { get; set; } = string.Empty;

        /// <summary>
        /// Base imponible
        /// </summary>
        [XmlElement("BaseImp")]
        public decimal BaseImp { get; set; }

        /// <summary>
        /// Alícuota
        /// </summary>
        [XmlElement("Alic")]
        public decimal Alic { get; set; }

        /// <summary>
        /// Importe del tributo
        /// </summary>
        [XmlElement("Importe")]
        public decimal Importe { get; set; }
    }

    /// <summary>
    /// Response de solicitud de CAE
    /// </summary>
    [XmlRoot("FECAESolicitarResponse")]
    public class FECAESolicitarResponse
    {
        [XmlElement("FECAESolicitarResult")]
        public FECAEResponse? FECAESolicitarResult { get; set; }
    }

    /// <summary>
    /// Response CAE
    /// </summary>
    public class FECAEResponse
    {
        [XmlElement("FeCabResp")]
        public FECAECabResponse? FeCabResp { get; set; }

        [XmlArray("FeDetResp")]
        [XmlArrayItem("FECAEDetResponse")]
        public FECAEDetResponse[]? FeDetResp { get; set; }

        [XmlArray("Events")]
        [XmlArrayItem("Evt")]
        public Evt[]? Events { get; set; }

        [XmlArray("Errors")]
        [XmlArrayItem("Err")]
        public Err[]? Errors { get; set; }
    }

    /// <summary>
    /// Cabecera de respuesta CAE
    /// </summary>
    public class FECAECabResponse
    {
        [XmlElement("Cuit")]
        public long Cuit { get; set; }

        [XmlElement("PtoVta")]
        public int PtoVta { get; set; }

        [XmlElement("CbteTipo")]
        public int CbteTipo { get; set; }

        [XmlElement("FchProceso")]
        public string FchProceso { get; set; } = string.Empty;

        [XmlElement("CantReg")]
        public int CantReg { get; set; }

        [XmlElement("Resultado")]
        public string Resultado { get; set; } = string.Empty;

        [XmlElement("Reproceso")]
        public string Reproceso { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detalle de respuesta CAE
    /// </summary>
    public class FECAEDetResponse
    {
        [XmlElement("Concepto")]
        public int Concepto { get; set; }

        [XmlElement("DocTipo")]
        public int DocTipo { get; set; }

        [XmlElement("DocNro")]
        public long DocNro { get; set; }

        [XmlElement("CbteDesde")]
        public long CbteDesde { get; set; }

        [XmlElement("CbteHasta")]
        public long CbteHasta { get; set; }

        [XmlElement("CbteFch")]
        public string CbteFch { get; set; } = string.Empty;

        [XmlElement("Resultado")]
        public string Resultado { get; set; } = string.Empty;

        /// <summary>
        /// Código de Autorización Electrónico
        /// </summary>
        [XmlElement("CAE")]
        public string CAE { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de vencimiento del CAE
        /// </summary>
        [XmlElement("CAEFchVto")]
        public string CAEFchVto { get; set; } = string.Empty;

        [XmlArray("Observaciones")]
        [XmlArrayItem("Obs")]
        public Obs[]? Observaciones { get; set; }
    }

    /// <summary>
    /// Request para obtener último comprobante
    /// </summary>
    public class FERecuperaLastCbteRequest
    {
        [XmlElement("Auth")]
        public FEAuthRequest? Auth { get; set; }

        [XmlElement("PtoVta")]
        public int PtoVta { get; set; }

        [XmlElement("CbteTipo")]
        public int CbteTipo { get; set; }
    }

    /// <summary>
    /// Response de último comprobante
    /// </summary>
    public class FERecuperaLastCbteResponse
    {
        [XmlElement("PtoVta")]
        public int PtoVta { get; set; }

        [XmlElement("CbteTipo")]
        public int CbteTipo { get; set; }

        [XmlElement("CbteNro")]
        public int CbteNro { get; set; }

        [XmlArray("Errors")]
        [XmlArrayItem("Err")]
        public Err[]? Errors { get; set; }

        [XmlArray("Events")]
        [XmlArrayItem("Evt")]
        public Evt[]? Events { get; set; }
    }

    /// <summary>
    /// Response de tipos de comprobante
    /// </summary>
    public class CbteTipoResponse
    {
        [XmlArray("ResultGet")]
        [XmlArrayItem("CbteTipo")]
        public CbteTipo[]? ResultGet { get; set; }

        [XmlArray("Errors")]
        [XmlArrayItem("Err")]
        public Err[]? Errors { get; set; }

        [XmlArray("Events")]
        [XmlArrayItem("Evt")]
        public Evt[]? Events { get; set; }
    }

    /// <summary>
    /// Tipo de comprobante
    /// </summary>
    public class CbteTipo
    {
        /// <summary>
        /// ID del tipo de comprobante (11 = Factura C)
        /// </summary>
        [XmlElement("Id")]
        public int Id { get; set; }

        /// <summary>
        /// Descripción del tipo de comprobante
        /// </summary>
        [XmlElement("Desc")]
        public string Desc { get; set; } = string.Empty;

        /// <summary>
        /// Fecha desde que está vigente
        /// </summary>
        [XmlElement("FchDesde")]
        public string FchDesde { get; set; } = string.Empty;

        /// <summary>
        /// Fecha hasta que está vigente
        /// </summary>
        [XmlElement("FchHasta")]
        public string FchHasta { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response del método dummy
    /// </summary>
    public class DummyResponse
    {
        [XmlElement("AppServer")]
        public string AppServer { get; set; } = string.Empty;

        [XmlElement("DbServer")]
        public string DbServer { get; set; } = string.Empty;

        [XmlElement("AuthServer")]
        public string AuthServer { get; set; } = string.Empty;
    }

    /// <summary>
    /// Error de AFIP
    /// </summary>
    public class Err
    {
        [XmlElement("Code")]
        public int Code { get; set; }

        [XmlElement("Msg")]
        public string Msg { get; set; } = string.Empty;
    }

    /// <summary>
    /// Evento de AFIP
    /// </summary>
    public class Evt
    {
        [XmlElement("Code")]
        public int Code { get; set; }

        [XmlElement("Msg")]
        public string Msg { get; set; } = string.Empty;
    }

    /// <summary>
    /// Observación de AFIP
    /// </summary>
    public class Obs
    {
        [XmlElement("Code")]
        public int Code { get; set; }

        [XmlElement("Msg")]
        public string Msg { get; set; } = string.Empty;
    }

    /// <summary>
    /// Constantes para tipos de comprobante más comunes
    /// </summary>
    public static class TiposComprobante
    {
        /// <summary>
        /// Factura A
        /// </summary>
        public const int FacturaA = 1;

        /// <summary>
        /// Nota de Débito A
        /// </summary>
        public const int NotaDebitoA = 2;

        /// <summary>
        /// Nota de Crédito A
        /// </summary>
        public const int NotaCreditoA = 3;

        /// <summary>
        /// Factura B
        /// </summary>
        public const int FacturaB = 6;

        /// <summary>
        /// Nota de Débito B
        /// </summary>
        public const int NotaDebitoB = 7;

        /// <summary>
        /// Nota de Crédito B
        /// </summary>
        public const int NotaCreditoB = 8;

        /// <summary>
        /// Factura C - Consumidor Final
        /// </summary>
        public const int FacturaC = 11;

        /// <summary>
        /// Nota de Débito C
        /// </summary>
        public const int NotaDebitoC = 12;

        /// <summary>
        /// Nota de Crédito C
        /// </summary>
        public const int NotaCreditoC = 13;
    }

    /// <summary>
    /// Constantes para tipos de documento
    /// </summary>
    public static class TiposDocumento
    {
        /// <summary>
        /// CUIT
        /// </summary>
        public const int CUIT = 80;

        /// <summary>
        /// CUIL
        /// </summary>
        public const int CUIL = 86;

        /// <summary>
        /// DNI
        /// </summary>
        public const int DNI = 96;

        /// <summary>
        /// Consumidor Final
        /// </summary>
        public const int ConsumidorFinal = 99;
    }

    /// <summary>
    /// Constantes para conceptos de comprobante
    /// </summary>
    public static class ConceptosComprobante
    {
        /// <summary>
        /// Productos
        /// </summary>
        public const int Productos = 1;

        /// <summary>
        /// Servicios
        /// </summary>
        public const int Servicios = 2;

        /// <summary>
        /// Productos y Servicios
        /// </summary>
        public const int ProductosYServicios = 3;
    }

    /// <summary>
    /// Constantes para alícuotas de IVA
    /// </summary>
    public static class AlicuotasIVA
    {
        /// <summary>
        /// 0%
        /// </summary>
        public const int Cero = 3;

        /// <summary>
        /// 10.5%
        /// </summary>
        public const int DiezCincoPorciento = 4;

        /// <summary>
        /// 21%
        /// </summary>
        public const int VeintiUnPorciento = 5;

        /// <summary>
        /// 27%
        /// </summary>
        public const int VeintisietePorciento = 6;

        /// <summary>
        /// 5%
        /// </summary>
        public const int CincoPorciento = 8;

        /// <summary>
        /// 2.5%
        /// </summary>
        public const int DosCincoPorciento = 9;
    }

    /// <summary>
    /// Response para obtener condiciones de IVA
    /// </summary>
    public class CondicionIvaResponse
    {
        [XmlArray("ResultGet")]
        [XmlArrayItem("CondicionIva")]
        public CondicionIvaItem[]? ResultGet { get; set; }

        [XmlArray("Errors")]
        [XmlArrayItem("Err")]
        public Err[]? Errors { get; set; }
    }

    /// <summary>
    /// Item de condición IVA
    /// </summary>
    public class CondicionIvaItem
    {
        [XmlElement("Id")]
        public int Id { get; set; }

        [XmlElement("Desc")]
        public string Desc { get; set; } = string.Empty;
    }

    /// <summary>
    /// Constantes para condiciones frente al IVA más comunes
    /// </summary>
    public static class CondicionesIVA
    {
        /// <summary>
        /// IVA Responsable Inscripto
        /// </summary>
        public const int ResponsableInscripto = 1;

        /// <summary>
        /// IVA Responsable no Inscripto
        /// </summary>
        public const int ResponsableNoInscripto = 2;

        /// <summary>
        /// IVA no Responsable
        /// </summary>
        public const int NoResponsable = 3;

        /// <summary>
        /// IVA Sujeto Exento
        /// </summary>
        public const int SujetoExento = 4;

        /// <summary>
        /// Consumidor Final
        /// </summary>
        public const int ConsumidorFinal = 5;

        /// <summary>
        /// Responsable Monotributo
        /// </summary>
        public const int ResponsableMonotributo = 6;

        /// <summary>
        /// Sujeto no Categorizado
        /// </summary>
        public const int SujetoNoCategorizado = 7;

        /// <summary>
        /// Proveedor del Exterior
        /// </summary>
        public const int ProveedorExterior = 8;

        /// <summary>
        /// Cliente del Exterior
        /// </summary>
        public const int ClienteExterior = 9;

        /// <summary>
        /// IVA Liberado - Ley Nº 19.640
        /// </summary>
        public const int IvaLiberado = 10;

        /// <summary>
        /// IVA Responsable Inscripto - Agente de Percepción
        /// </summary>
        public const int ResponsableInscriptoAgentePercepcion = 11;

        /// <summary>
        /// Pequeño Contribuyente Eventual
        /// </summary>
        public const int PequenioContribuyenteEventual = 12;

        /// <summary>
        /// Monotributista Social
        /// </summary>
        public const int MonotributistaSocial = 13;

        /// <summary>
        /// Pequeño Contribuyente Eventual Social
        /// </summary>
        public const int PequenioContribuyenteEventualSocial = 14;
    }
}
