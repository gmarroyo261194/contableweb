using Volo.Abp.Auditing;

namespace ContableWeb.Entities.Clientes;

public enum TipoCondIva
{
    ResponsableInscripto = 1,
    ResponsableNoInscripto = 3,
    Exento = 4,
    ConsumidorFinal = 5,
    Monotributista = 6,
    NoCategorizado = 7,
    ProveedorExterior = 8,
    ClienteExterior = 9,
    IvaLiberado = 10
}