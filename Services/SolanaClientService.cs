using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using volatility_service.Data;

namespace volatility_service.Services;

public class SolanaClientService
{
    private const uint BASIS_POINTS_MULTIPLIER = 10000;
    private readonly IConfiguration _config;
    private readonly string _programId; 
    
    public SolanaClientService(IConfiguration configuration)
    {
        _config = configuration;   
        _programId = _config["OnchainProgramId"];
    }

    public async Task PushVolatilityOnChain(ushort marketIx, Dictionary<Window, decimal> volData)
    {
        try
        {
            byte[] keypair = JsonConvert.DeserializeObject<byte[]>(_config["SolanaLocalPK"]);

            byte[] privateKey = new byte[64];
            Array.Copy(keypair, privateKey, 64);
            
            byte[] publicKey = new byte[32];
            Array.Copy(keypair, 32, publicKey, 0, 32);
            
            var wallet = new Account(privateKey, publicKey);
        
            // var payer = wallet.Account;   

            string clusterUrl = _config["SolanaCluster"] ?? throw new ExecutionEngineException();
            var cluster = ClientFactory.GetClient(clusterUrl);

            var marketIxBytes = BitConverter.GetBytes(marketIx);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(marketIxBytes);

            bool success = PublicKey.TryFindProgramAddress(
                new[] { Encoding.ASCII.GetBytes("market"), marketIxBytes },
                new PublicKey(_programId),
                out PublicKey marketPDA,
                out byte bump
            );

            if (!success) throw new Exception("Failed to derive market PDA");

            // var ss = await cluster.GetAccountInfoAsync(marketPDA);
            // Console.WriteLine($"Fetched market {ss.Result.Value}");

            // var base64Data = ss.Result.Value.Data[0];
            // byte[] rawBytes = Convert.FromBase64String(base64Data);

            // Market market = ParseMarket(rawBytes);
            // Console.WriteLine($"Market Name: {market.Name}, Fee: {market.FeeBps} bps");
            

            var instruction = new TransactionInstruction()
            {
                ProgramId = new PublicKey(_programId),
                Keys = new List<AccountMeta>
                {
                    AccountMeta.ReadOnly(wallet.PublicKey, true),
                    AccountMeta.Writable(marketPDA, false),
                    AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                },
                Data = BuildUpdateMarketVolInstructionData(
                    marketIx,
                    (uint)Math.Round(volData[Window.Hour1] * BASIS_POINTS_MULTIPLIER, MidpointRounding.AwayFromZero),
                    (uint)Math.Round(volData[Window.Hour4] * BASIS_POINTS_MULTIPLIER, MidpointRounding.AwayFromZero),
                    (uint)Math.Round(volData[Window.Day1] * BASIS_POINTS_MULTIPLIER, MidpointRounding.AwayFromZero),
                    (uint)Math.Round(volData[Window.Day3] * BASIS_POINTS_MULTIPLIER, MidpointRounding.AwayFromZero),
                    (uint)Math.Round(volData[Window.Day7] * BASIS_POINTS_MULTIPLIER, MidpointRounding.AwayFromZero)
                )
            };

            var blockHash = await cluster.GetLatestBlockHashAsync();
            var tx = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(wallet)
                .AddInstruction(instruction)
                .Build(wallet);

            var res = await cluster.SendTransactionAsync(tx);
            

            if (res.WasSuccessful)
            {
                Console.WriteLine($"Tx sent: {res.Result}");
            }
            else
            {
                Console.WriteLine($"Tx failed: {res.Reason}");
            }
        } catch (Exception e)
        {

        }
    }

    private byte[] BuildUpdateMarketVolInstructionData(ushort marketIx, uint hour1, uint hour4, uint day1, uint day3, uint week) 
    {
        var buffer = new List<byte>();

        buffer.AddRange(GetDiscriminator("update_market_vol"));
        buffer.AddRange(BitConverter.GetBytes(marketIx));
        buffer.AddRange(BitConverter.GetBytes(hour1));
        buffer.AddRange(BitConverter.GetBytes(hour4));
        buffer.AddRange(BitConverter.GetBytes(day1));
        buffer.AddRange(BitConverter.GetBytes(day3));
        buffer.AddRange(BitConverter.GetBytes(week));

        return buffer.ToArray();
    }

    private static byte[] GetDiscriminator(string name)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes("global:" + name));
        return hash[..8]; // Take first 8 bytes
    }

    public static Market ParseMarket(byte[] data)
{
    var reader = new BinaryReader(new MemoryStream(data));

    var market = new Market
    {
        Id = reader.ReadUInt16(),
        Name = ReadBorshString(reader),
        AssetMint = reader.ReadBytes(32),
        FeeBps = reader.ReadUInt64(),
        Bump = reader.ReadByte(),
        ReserveSupply = reader.ReadUInt64(),
        CommittedReserve = reader.ReadUInt64(),
        Premiums = reader.ReadUInt64(),
        LpMinted = reader.ReadUInt64(),
        PriceFeed = "", //ReadBorshString(reader),
        AssetDecimals = reader.ReadByte(),
        Hour1VolatilityBps = reader.ReadUInt32(),
        Hour4VolatilityBps = reader.ReadUInt32(),
        Day1VolatilityBps = reader.ReadUInt32(),
        Day3VolatilityBps = reader.ReadUInt32(),
        WeekVolatilityBps = reader.ReadUInt32(),
        VolLastUpdated = reader.ReadInt64()
    };

    return market;
}

private static string ReadBorshString(BinaryReader reader)
{
    var length = reader.ReadInt32(); // little-endian u32
    var strBytes = reader.ReadBytes(length);
    return Encoding.UTF8.GetString(strBytes);
}
}