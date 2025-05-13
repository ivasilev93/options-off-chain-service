namespace volatility_service;

public static class Calculations
{
    public static decimal CalculateHistoricalVolatility(decimal[] values, int sampleIntervalInSeconds)
    {
        var logReturns = new List<decimal>(values.Length - 1);
        for (int i = 1; i < values.Length; i++)
        {
            if (values[i-1] > 0 && values[i] > 0)
            {
                decimal logReturn = (decimal)Math.Log((double)(values[i] / values[i-1]));
                logReturns.Add(logReturn);
            }
        }

        if (logReturns.Count == 0)
            return 0;
        
        //Std dev
        decimal mean = logReturns.Average();
        decimal variance = logReturns.Sum(r => (r - mean) * (r - mean)) / logReturns.Count;
        decimal stdDev = (decimal)Math.Sqrt((double)variance);

        decimal annualizingFctr = (decimal)Math.Sqrt(365 * 24 * 60 * 60 / sampleIntervalInSeconds);
        return stdDev * annualizingFctr;
    }
}