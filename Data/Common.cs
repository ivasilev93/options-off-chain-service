namespace volatility_service.Data;

public class TokenInfo 
{
    public string Symbol { get; set; }
    public string MarketIx { get; set; }
    public string Address { get; set; }
}

public enum Window
{
    Hour1,
    Hour4,
    Day1,
    Day3,
    Day7
}


public class Market
{
    public ushort Id { get; set; }
    public string Name { get; set; }
    public string PriceFeed { get; set; }
    public byte[] AssetMint { get; set; } // 32 bytes pubkey
    public ulong FeeBps { get; set; }
    public byte Bump { get; set; }
    public ulong ReserveSupply { get; set; }
    public ulong CommittedReserve { get; set; }
    public ulong Premiums { get; set; }
    public ulong LpMinted { get; set; }
    public byte AssetDecimals { get; set; }
    public uint Hour1VolatilityBps { get; set; }
    public uint Hour4VolatilityBps { get; set; }
    public uint Day1VolatilityBps { get; set; }
    public uint Day3VolatilityBps { get; set; }
    public uint WeekVolatilityBps { get; set; }
    public long VolLastUpdated { get; set; }
}