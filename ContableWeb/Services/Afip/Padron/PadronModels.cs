using System.Xml.Serialization;

namespace ContableWeb.Services.Afip.Padron;

/// <summary>
/// Modelos para el servicio de Padrón AFIP (PersonaServiceA5)
/// </summary>

#region Request Models

public class PadronAuthRequest
{
    public string Token { get; set; } = string.Empty;
    public string Sign { get; set; } = string.Empty;
    public long CuitRepresentada { get; set; }
}

#endregion

#region Response Models

public class PersonaReturn
{
    [XmlElement("metadata")]
    public Metadata? Metadata { get; set; }
    
    [XmlElement("persona")]
    public Persona? Persona { get; set; }
}

public class PersonaListReturn
{
    [XmlElement("metadata")]
    public Metadata? Metadata { get; set; }
    
    [XmlArray("persona")]
    [XmlArrayItem("persona")]
    public Persona[]? Personas { get; set; }
}

public class Metadata
{
    [XmlElement("fechaHora")]
    public DateTime FechaHora { get; set; }
    
    [XmlElement("servidor")]
    public string Servidor { get; set; } = string.Empty;
}

public class Persona
{
    [XmlElement("datosGenerales")]
    public DatosGenerales? DatosGenerales { get; set; }
    
    [XmlElement("datosMonotributo")]
    public DatosMonotributo? DatosMonotributo { get; set; }
    
    [XmlElement("datosRegimenGeneral")]
    public DatosRegimenGeneral? DatosRegimenGeneral { get; set; }
    
    [XmlElement("errorConstancia")]
    public ErrorConstancia? ErrorConstancia { get; set; }
    
    [XmlElement("errorMonotributo")]
    public ErrorMonotributo? ErrorMonotributo { get; set; }
    
    [XmlElement("errorRegimenGeneral")]
    public ErrorRegimenGeneral? ErrorRegimenGeneral { get; set; }
}

#endregion

#region Datos Generales

public class DatosGenerales
{
    [XmlElement("apellido")]
    public string? Apellido { get; set; }
    
    [XmlElement("domicilioFiscal")]
    public Domicilio? DomicilioFiscal { get; set; }
    
    [XmlElement("estadoClave")]
    public string? EstadoClave { get; set; }
    
    [XmlElement("idPersona")]
    public long IdPersona { get; set; }
    
    [XmlElement("mesCierre")]
    public int MesCierre { get; set; }
    
    [XmlElement("nombre")]
    public string? Nombre { get; set; }
    
    [XmlElement("razonSocial")]
    public string? RazonSocial { get; set; }
    
    [XmlElement("tipoPersona")]
    public string? TipoPersona { get; set; }
    
    [XmlElement("tipoClave")]
    public string? TipoClave { get; set; }
}

public class Domicilio
{
    [XmlElement("codPostal")]
    public string? CodigoPostal { get; set; }
    
    [XmlElement("descripcionProvincia")]
    public string? DescripcionProvincia { get; set; }
    
    [XmlElement("direccion")]
    public string? Direccion { get; set; }
    
    [XmlElement("idProvincia")]
    public int IdProvincia { get; set; }
    
    [XmlElement("localidad")]
    public string? Localidad { get; set; }
    
    [XmlElement("tipoDomicilio")]
    public string? TipoDomicilio { get; set; }
}

#endregion

#region Datos Monotributo

public class DatosMonotributo
{
    [XmlArray("actividad")]
    [XmlArrayItem("actividad")]
    public ActividadMonotributo[]? Actividades { get; set; }
    
    [XmlElement("categoriaMonotributo")]
    public CategoriaMonotributo? CategoriaMonotributo { get; set; }
    
    [XmlElement("componente")]
    public ComponenteMonotributo? Componente { get; set; }
    
    [XmlArray("impuesto")]
    [XmlArrayItem("impuesto")]
    public ImpuestoMonotributo[]? Impuestos { get; set; }
}

public class ActividadMonotributo
{
    [XmlElement("descripcionActividad")]
    public string? DescripcionActividad { get; set; }
    
    [XmlElement("idActividad")]
    public long IdActividad { get; set; }
    
    [XmlElement("nomenclador")]
    public int Nomenclador { get; set; }
    
    [XmlElement("orden")]
    public int Orden { get; set; }
    
    [XmlElement("periodo")]
    public int Periodo { get; set; }
}

public class CategoriaMonotributo
{
    [XmlElement("descripcionCategoria")]
    public string? DescripcionCategoria { get; set; }
    
    [XmlElement("idCategoria")]
    public int IdCategoria { get; set; }
    
    [XmlElement("idImpuesto")]
    public int IdImpuesto { get; set; }
    
    [XmlElement("periodo")]
    public int Periodo { get; set; }
}

public class ComponenteMonotributo
{
    [XmlElement("descripcionComponente")]
    public string? DescripcionComponente { get; set; }
    
    [XmlElement("idComponente")]
    public int IdComponente { get; set; }
}

public class ImpuestoMonotributo
{
    [XmlElement("descripcionImpuesto")]
    public string? DescripcionImpuesto { get; set; }
    
    [XmlElement("estadoImpuesto")]
    public string? EstadoImpuesto { get; set; }
    
    [XmlElement("idImpuesto")]
    public int IdImpuesto { get; set; }
    
    [XmlElement("motivo")]
    public string? Motivo { get; set; }
    
    [XmlElement("periodo")]
    public int Periodo { get; set; }
}

#endregion

#region Datos Regimen General

public class DatosRegimenGeneral
{
    [XmlArray("actividad")]
    [XmlArrayItem("actividad")]
    public Actividad[]? Actividades { get; set; }
    
    [XmlElement("categoriaAutonomo")]
    public Categoria? CategoriaAutonomo { get; set; }
    
    [XmlArray("impuesto")]
    [XmlArrayItem("impuesto")]
    public Impuesto[]? Impuestos { get; set; }
    
    [XmlArray("regimen")]
    [XmlArrayItem("regimen")]
    public Regimen[]? Regimenes { get; set; }
}

public class Actividad
{
    [XmlElement("descripcionActividad")]
    public string? DescripcionActividad { get; set; }
    
    [XmlElement("idActividad")]
    public long IdActividad { get; set; }
    
    [XmlElement("nomenclador")]
    public int Nomenclador { get; set; }
    
    [XmlElement("orden")]
    public int Orden { get; set; }
    
    [XmlElement("periodo")]
    public int Periodo { get; set; }
}

public class Categoria
{
    [XmlElement("descripcionCategoria")]
    public string? DescripcionCategoria { get; set; }
    
    [XmlElement("idCategoria")]
    public int IdCategoria { get; set; }
    
    [XmlElement("idImpuesto")]
    public int IdImpuesto { get; set; }
    
    [XmlElement("periodo")]
    public int Periodo { get; set; }
}

public class Impuesto
{
    [XmlElement("descripcionImpuesto")]
    public string? DescripcionImpuesto { get; set; }
    
    [XmlElement("estadoImpuesto")]
    public string? EstadoImpuesto { get; set; }
    
    [XmlElement("idImpuesto")]
    public int IdImpuesto { get; set; }
    
    [XmlElement("motivo")]
    public string? Motivo { get; set; }
    
    [XmlElement("periodo")]
    public int Periodo { get; set; }
}

public class Regimen
{
    [XmlElement("descripcionRegimen")]
    public string? DescripcionRegimen { get; set; }
    
    [XmlElement("idImpuesto")]
    public int IdImpuesto { get; set; }
    
    [XmlElement("idRegimen")]
    public int IdRegimen { get; set; }
    
    [XmlElement("periodo")]
    public int Periodo { get; set; }
    
    [XmlElement("tipoRegimen")]
    public string? TipoRegimen { get; set; }
}

#endregion

#region Error Models

public class ErrorConstancia
{
    [XmlElement("apellido")]
    public string? Apellido { get; set; }
    
    [XmlArray("error")]
    [XmlArrayItem("error")]
    public string[]? Errores { get; set; }
    
    [XmlElement("idPersona")]
    public long IdPersona { get; set; }
    
    [XmlElement("nombre")]
    public string? Nombre { get; set; }
}

public class ErrorMonotributo
{
    [XmlArray("error")]
    [XmlArrayItem("error")]
    public string[]? Errores { get; set; }
    
    [XmlElement("mensaje")]
    public string? Mensaje { get; set; }
}

public class ErrorRegimenGeneral
{
    [XmlArray("error")]
    [XmlArrayItem("error")]
    public string[]? Errores { get; set; }
    
    [XmlElement("mensaje")]
    public string? Mensaje { get; set; }
}

#endregion

#region Dummy

public class PadronDummyResponse
{
    [XmlElement("appserver")]
    public string AppServer { get; set; } = string.Empty;
    
    [XmlElement("authserver")]
    public string AuthServer { get; set; } = string.Empty;
    
    [XmlElement("dbserver")]
    public string DbServer { get; set; } = string.Empty;
}

#endregion

