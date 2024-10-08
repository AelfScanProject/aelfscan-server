using System;
using System.Collections.Generic;
using System.Globalization;

namespace AElfScanServer.Common.Helper;

public static class IdGeneratorHelper
{
    public static string GenerateId(params object[] ids) => ids.JoinAsString("-");


    public static string GetId(params object[] inputs)
    {
        return inputs.JoinAsString("-");
    }

    public static string GetNftInfoId(string chainId, string symbol)
    {
        return GetId(chainId, symbol);
    }
}

public static class DateTimeHelper
{
    public static long GetTotalSeconds(DateTime dateTime)
    {
        return (long)dateTime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
    }

    public static long GetTotalMilliseconds(DateTime dateTime)
    {
        return (long)dateTime.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }


    public static long GetPreviousDayMilliseconds(long time, int dateInterval)
    {
        long msInDay = 24 * 60 * 60 * 1000;
        long msInDays = (dateInterval - 1) * msInDay;

        return time - msInDays;
    }


    public static string GetDateTimeString(long milliseconds)
    {
        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(milliseconds)
            .ToUtc8String("yyyy-MM-dd");
    }


    public static long GetIntDateTimeToTimestamp(long date)
    {
        string dateString = date.ToString();

        DateTime dateTime = DateTime.ParseExact(dateString, "yyyyMMdd", null);

        long timestamp = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();

        return timestamp;
    }


    public static string FormatDateStr(string date)
    {
        DateTime dateTime = DateTime.Parse(date, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        string formattedDateTime = dateTime.ToString("yyyy-MM-dd");
        return formattedDateTime;
    }


    public static DateTime GetDateTimeFromYYMMDD(string dateString)
    {
        string dateFormat = "yyyy-MM-dd";
        DateTime dateTime;
        DateTime.TryParseExact(dateString, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
            out dateTime);
        return dateTime;
    }

    public static long ConvertYYMMDD(string dateString)
    {
        string dateFormat = "yyyy-MM-dd";

        DateTime dateTime;
        if (DateTime.TryParseExact(dateString, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out dateTime))
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime);

            TimeSpan timeSinceEpoch =
                dateTimeOffset.UtcDateTime - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

            long timestamp = (long)timeSinceEpoch.TotalMilliseconds;

            return timestamp;
        }

        return 0;
    }

    public static int GetNowMonth()
    {
        DateTime now = DateTime.Now;
        string formattedDate = now.ToString("yyyyMM", CultureInfo.InvariantCulture);

        return int.Parse(formattedDate);
    }

    public static int GetBeforeYYMMDD(int dateMonth)
    {
        string inputDate = dateMonth.ToString();
        int year = int.Parse(inputDate.Substring(0, 4));
        int month = int.Parse(inputDate.Substring(4, 2));

        DateTime date = new DateTime(year, month, 1);

        DateTime previousMonthDate = date.AddMonths(-1);

        string previousMonthFormatted = previousMonthDate.ToString("yyyyMM");

        return int.Parse(previousMonthFormatted);
    }


    public static int GetNextYYMMDD(int dateMonth)
    {
        string inputDate = dateMonth.ToString();
        int year = int.Parse(inputDate.Substring(0, 4));
        int month = int.Parse(inputDate.Substring(4, 2));

        DateTime date = new DateTime(year, month, 1);

        DateTime previousMonthDate = date.AddMonths(+1);

        string previousMonthFormatted = previousMonthDate.ToString("yyyyMM");

        return int.Parse(previousMonthFormatted);
    }


    public static int ConvertToYYYYMM(long timestamp)
    {
        DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
        string formattedDate = dateTime.ToString("yyyyMM");

        return int.Parse(formattedDate);
    }

    public static string GetNextDayDate(string dateString)
    {
        string dateFormat = "yyyy-MM-dd";

        DateTime dateTime;
        DateTime.TryParseExact(dateString, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
            out dateTime);

        return dateTime.AddDays(1).ToUtc8String(dateFormat);
    }


    public static string GetBeforeDayDate(string dateString)
    {
        string dateFormat = "yyyy-MM-dd";

        DateTime dateTime;
        DateTime.TryParseExact(dateString, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
            out dateTime);

        return dateTime.AddDays(-1).ToUtc8String(dateFormat);
    }


    public static string GetAfterDayDate(string dateString)
    {
        string dateFormat = "yyyy-MM-dd";

        DateTime dateTime;
        DateTime.TryParseExact(dateString, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
            out dateTime);

        return dateTime.AddDays(1).ToUtc8String(dateFormat);
    }

    public static string GetDateStr(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, DateTimeKind.Utc).ToString(
            "yyyy-MM-dd");
    }


    public static long ParseDateToLong(string dateString)
    {
        DateTime date = DateTime.ParseExact(dateString, "yyyy-MM-dd", null);
        long longDate = long.Parse(date.ToString("yyyyMMdd"));

        return longDate;
    }


    public static List<long> GetRangeDayList(long start, long end)
    {
        DateTime startTime = DateTimeOffset.FromUnixTimeMilliseconds(start).UtcDateTime;
        DateTime endTime = DateTimeOffset.FromUnixTimeMilliseconds(end).UtcDateTime;
        List<long> dailyTimestamps = new List<long>();

        TimeSpan timeSpan = endTime - startTime;

        for (int i = 0; i <= timeSpan.Days; i++)
        {
            DateTime dailyStart = startTime.AddDays(i);
            long dailyTimestamp = ((DateTimeOffset)dailyStart).ToUnixTimeMilliseconds();
            dailyTimestamps.Add(dailyTimestamp);
        }

        return dailyTimestamps;
    }


    public static List<long> GetDayHourList(long milliseconds)
    {
        var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).DateTime;


        List<long> hourTimestamps = new List<long>();

        for (int hour = 0; hour < 24; hour++)
        {
            DateTime hourDateTime = dateTime.AddHours(hour);

            TimeSpan timeSpan = hourDateTime - new DateTime(1970, 1, 1);

            long timestamp = (long)timeSpan.TotalMilliseconds;

            hourTimestamps.Add(timestamp);
        }

        return hourTimestamps;
    }

    public static List<long> GetDayHourList(string dateStr)
    {
        var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(ConvertYYMMDD(dateStr)).DateTime;


        List<long> hourTimestamps = new List<long>();

        for (int hour = 0; hour < 24; hour++)
        {
            DateTime hourDateTime = dateTime.AddHours(hour);

            TimeSpan timeSpan = hourDateTime - new DateTime(1970, 1, 1);

            long timestamp = (long)timeSpan.TotalMilliseconds;

            hourTimestamps.Add(timestamp);
        }

        return hourTimestamps;
    }

    public static List<DateTime> GetDateTimeHourList(string dateStr)
    {
        DateTimeStyles style = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
        DateTime dateTime = DateTime.ParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, style);

        DateTime dateTimeUtc = dateTime.ToUniversalTime();


        List<DateTime> hourTimestamps = new List<DateTime>();

        for (int hour = 0; hour <= 24; hour++)
        {
            DateTime hourDateTime = dateTimeUtc.AddHours(hour);

            hourTimestamps.Add(hourDateTime);
        }

        return hourTimestamps;
    }


    public static long GetDateTimeLong(long milliseconds)
    {
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        DateTime dateTime = dateTimeOffset.DateTime;

        var dateTotalMilliseconds = GetDateTotalMilliseconds(dateTime);
        return dateTotalMilliseconds;
    }


    public static long GetDateTotalMilliseconds(DateTime dateTime)
    {
        DateTime currentDate = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
        return (long)currentDate.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
    }


    public static long GetTotalMinutes(DateTime dateTime)
    {
        return (long)dateTime.Subtract(new DateTime(1970, 1, 1)).TotalMinutes;
    }

    public static long GetTodayTotalSeconds()
    {
        var now = DateTime.Now;
        DateTime currentDate = new DateTime(now.Year, now.Month, now.Day);
        return GetTotalSeconds(currentDate);
    }

    public static long GetTodayTotalMilliSeconds()
    {
        var now = DateTime.Now;
        DateTime currentDate = new DateTime(now.Year, now.Month, now.Day);
        return GetTotalSeconds(currentDate);
    }


    public static long GetTomorrowTotalSeconds()
    {
        var now = DateTime.Now;
        DateTime currentDate = new DateTime(now.Year, now.Month, now.Day + 1);
        return GetTotalSeconds(currentDate);
    }

    public static long GetAfterDayTotalSeconds(long t)
    {
        DateTime currentDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(t);
        DateTime nextDate = currentDate.AddDays(1);
        return (long)(nextDate - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }


    public static long GetNowMilliSeconds()
    {
        return GetTotalMilliseconds(DateTime.Now);
    }

    public static long GetBeforeHoursMilliSeconds(int hours)
    {
        return GetTotalMilliseconds(DateTime.Now.AddHours(-hours));
    }

    public static long GetBeforeMinutesMilliSeconds(int minutes)
    {
        return GetTotalMilliseconds(DateTime.Now.AddMinutes(-minutes));
    }


    public static long GetBeforeDayMilliSeconds(int day)
    {
        return GetTotalMilliseconds(DateTime.Now.AddDays(-day));
    }

    public static long GetBeforeDayMilliSeconds(long milliseconds)
    {
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        DateTime dateTime = dateTimeOffset.DateTime;
        var time = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
        return GetTotalMilliseconds(time.AddDays(-1));
    }
}