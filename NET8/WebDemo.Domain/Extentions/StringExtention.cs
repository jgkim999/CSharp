using System.Globalization;

namespace WebDemo.Domain.Extentions;

public static class StringExtention
{
    /// <summary>
    /// yyyyMMddHHmmss
    /// </summary>
    /// <param name="dtFmt"></param>
    /// <param name="timeZone"></param>
    /// <returns></returns>
    public static DateTime ToDateTime(this string dtFmt, string timeZone = "Korea Standard Time")
    {
        var dt = DateTime.ParseExact(dtFmt, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        TimeZoneInfo krZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        //var localDt = DateTime.SpecifyKind(dt, DateTimeKind.Local);
        var ctDt = TimeZoneInfo.ConvertTime(dt, krZone, TimeZoneInfo.Local);
        return ctDt;
    }

    public static DateTime ToTimeZone(this DateTime dateTime, string timeZoneId = "Korea Standard Time")
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(dateTime.ToUniversalTime(), timeZone);
    }
}
