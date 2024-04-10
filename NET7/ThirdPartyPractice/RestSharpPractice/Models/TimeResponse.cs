namespace RestSharpPractice.Models;
/*
 {
  "year": 2023,
  "month": 5,
  "day": 28,
  "hour": 22,
  "minute": 1,
  "seconds": 5,
  "milliSeconds": 90,
  "dateTime": "2023-05-28T22:01:05.0902613",
  "date": "05/28/2023",
  "time": "22:01",
  "timeZone": "Asia/Seoul",
  "dayOfWeek": "Sunday",
  "dstActive": false
}
 */
public class TimeResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int Seconds { get; set; }
    public int MilliSeconds { get; set; }
    public DateTime DateTime { get; set; }
    public string Date { get; set; }
    public string Time { get; set; }
    public string TimeZone { get; set; }
    public string DayOfWeek { get; set; }
    public bool DstActive { get; set; }
}