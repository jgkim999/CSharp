namespace WebDemo.Domain.Extentions;

public static class DateTimeExtention
{
    /// <summary>
    /// 20240506203909
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static string FormatyyyyMMddHHmmss(this DateTime dt)
    {
        return dt.ToString("yyyyMMddHHmmss");
    }
}
