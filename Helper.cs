namespace volatility_service;

public static class Helper 
{
    public static DateTime ToDateTime(this string value) 
    {
        return DateTimeOffset
            .FromUnixTimeSeconds(long.Parse(value))
            .UtcDateTime;
    }
}