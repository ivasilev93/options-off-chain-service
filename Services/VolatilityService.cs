using Newtonsoft.Json;
using volatility_service.ApiModels;
using volatility_service.Data;

namespace volatility_service.Services;

public class VolatilityService 
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public VolatilityService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _config = configuration;   
        _httpClient = httpClientFactory.CreateClient();     
    }

    public async Task<Dictionary<Window, decimal>> ComputeVolatility(string tokenAddr)
    {
        //Get time series for 1HR
        var timeSeriesHour = await FetchHistoricPrices(
            tokenAddr, "3m", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
        await Task.Delay(1000); 

        //Get time series for 4HR
        var timeSeriesHour4 = await FetchHistoricPrices(
            tokenAddr, "5m", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow);
        await Task.Delay(1000); 

        //Get time series for day - 30min 1week ago
        var timeSeriesDay = await FetchHistoricPrices(
            tokenAddr, "30m", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        await Task.Delay(1000); 

        //Get time series for 3 day - 1h 2week ago
        var timeSeriesDay3 = await FetchHistoricPrices(
            tokenAddr, "1H", DateTime.UtcNow.AddDays(-14), DateTime.UtcNow);
        await Task.Delay(1000); 

        //Get time series for 7 day - 2h 30 days ago
        var timeSeriesDay7 = await FetchHistoricPrices(
            tokenAddr, "2H", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        await Task.Delay(1000); 
        
        var results = new Dictionary<Window, decimal>
        {
            { Window.Hour1, Calculations.CalculateHistoricalVolatility(timeSeriesHour, 3 * 60) },
            { Window.Hour4, Calculations.CalculateHistoricalVolatility(timeSeriesHour4, 5 * 60) },
            { Window.Day1, Calculations.CalculateHistoricalVolatility(timeSeriesDay, 30 * 60) },
            { Window.Day3, Calculations.CalculateHistoricalVolatility(timeSeriesDay3, 60 * 60) },
            { Window.Day7, Calculations.CalculateHistoricalVolatility(timeSeriesDay7, 2 * 60 * 60) },
        };

        return results;
    }

    private async Task<decimal[]> FetchHistoricPrices(
        string tokenAddr, string interval, DateTime from, DateTime to, int maxRetries = 3)
    {
        long unixFrom = ((DateTimeOffset)from).ToUnixTimeSeconds();
        long unixTo = ((DateTimeOffset)to).ToUnixTimeSeconds();
        string birdeyeKey = _config["Birdeye:ApiKey"];        

       for (int attempt = 0; attempt < maxRetries; attempt++)
       {
            try 
            {
                var reqMessage = new HttpRequestMessage();
                reqMessage.Method = HttpMethod.Get;
                reqMessage.Headers.Add("X-API-KEY", birdeyeKey);
                reqMessage.Headers.Add("accept", "application/json");
                reqMessage.Headers.Add("x-chain", "solana");
                reqMessage.RequestUri = new Uri($"https://public-api.birdeye.so/defi/history_price?address={tokenAddr}&address_type=token&type={interval}&time_from={unixFrom}&time_to={unixTo}");

                var resp = await  _httpClient.SendAsync(reqMessage);
                if (resp.IsSuccessStatusCode) 
                {
                    string content = await resp.Content.ReadAsStringAsync();
                    ApiResponse timeSeries = JsonConvert.DeserializeObject<ApiResponse>(content);
                    if (timeSeries.Success) 
                    {
                        var results = timeSeries.Data.Items.Select(x => x.Value).ToArray();
                        return results;
                    }
                }
            }    
            catch (Exception e)
            {
                if (attempt == maxRetries - 1) {
                    throw new Exception($"Failed to fetch price - from {from}, to {to}, interval: {interval}");
                }

                await Task.Delay(1000 * (attempt + 1));
            }
       }

       throw new Exception($"Failed to fetch price - from {from}, to {to}, interval: {interval}");
    }
}