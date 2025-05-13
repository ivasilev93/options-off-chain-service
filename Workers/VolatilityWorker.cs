using volatility_service.Data;
using volatility_service.Services;
namespace volatility_service;

public class VolatilityWorker : BackgroundService
{    
    private readonly ILogger<VolatilityWorker> _logger; 
    private readonly IConfiguration _config;
    private readonly VolatilityService _volService;
    private readonly SolanaClientService _solanaService;
    private Dictionary<string, Dictionary<Window, decimal>> _volatilityCache;

    public VolatilityWorker(
        ILogger<VolatilityWorker> logger, 
        IConfiguration configuration,
        SolanaClientService solanaService,
        VolatilityService volService)
    {
        _volatilityCache = new Dictionary<string, Dictionary<Window, decimal>>();
        _logger = logger;
        _config = configuration;
        _volService = volService;
        _solanaService = solanaService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {        
        var tokens = _config.GetSection("Tokens").Get<List<TokenInfo>>();
        int jobInterval = int.Parse(_config["VolatilityCalcInterval"]);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }           

           //Going sync because rate limits of free plan :)
           foreach (var token in tokens)
           {
                 //Seed vol cache w historic values
                var volData = await _volService.ComputeVolatility(token.Address);
                _volatilityCache[token.Symbol] = volData;

                await _solanaService.PushVolatilityOnChain(ushort.
                    Parse(token.MarketIx),
                    volData
                );
           }

            await Task.Delay(jobInterval, stoppingToken); 
        }
    }     
}
