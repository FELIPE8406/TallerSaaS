namespace TallerSaaS.Domain.Enums;

/// <summary>
/// Clasifica si un producto del inventario es una Refacción (parte física) 
/// o un Servicio (mano de obra, disponibilidad infinita).
/// </summary>
public enum TipoItemProducto
{
    Refaccion = 0,
    Servicio  = 1
}
