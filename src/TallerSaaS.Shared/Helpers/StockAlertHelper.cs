namespace TallerSaaS.Shared.Helpers;

public static class StockAlertHelper
{
    public static string GetNivel(int stock, int stockMinimo)
    {
        if (stock <= 0) return "Agotado";
        if (stock <= stockMinimo) return "Bajo";
        return "OK";
    }

    public static string GetClase(int stock, int stockMinimo)
    {
        if (stock <= 0) return "danger";
        if (stock <= stockMinimo) return "warning";
        return "success";
    }

    public static string GetIcon(int stock, int stockMinimo)
    {
        if (stock <= 0) return "bi-x-circle-fill";
        if (stock <= stockMinimo) return "bi-exclamation-triangle-fill";
        return "bi-check-circle-fill";
    }
}
