using Lumina.Excel;

namespace HuntTrainAssistant.LuminaCache;

public static class LuminaCache
{
    public static ExcelSheet<T>? Get<T>() where T : ExcelRow
        => Svc.Data.GetExcelSheet<T>();

    public static bool TryGet<T>(out ExcelSheet<T>? sheet) where T : ExcelRow
    {
        sheet = Svc.Data.GetExcelSheet<T>();
        return sheet != null;
    }

    public static T? GetRow<T>(uint rowID) where T : ExcelRow
        => Svc.Data.GetExcelSheet<T>()?.GetRow(rowID);

    public static bool TryGetRow<T>(uint rowID, out T? item) where T : ExcelRow
    {
        item = Svc.Data.GetExcelSheet<T>()?.GetRow(rowID);
        return item != null;
    }
}