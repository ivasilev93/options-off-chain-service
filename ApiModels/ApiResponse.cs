namespace volatility_service.ApiModels;

public class ApiResponse
{
    public bool Success { get; set; }
    public Data Data { get; set; }
}

public class Data
{
    public List<PricePoint> Items { get; set; }
}

public class PricePoint
{
    public string UnixTime { get; set; }
    public decimal Value { get; set; }
}