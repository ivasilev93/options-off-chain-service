using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using volatility_service;
using volatility_service.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<VolatilityWorker>();
builder.Services.AddHttpClient();
builder.Services.AddTransient<VolatilityService>();
builder.Services.AddTransient<SolanaClientService>();

if (builder.Environment.IsProduction())
{
    using var x509Store = new X509Store(StoreLocation.CurrentUser);

    x509Store.Open(OpenFlags.ReadOnly);

    var x509cert = x509Store.Certificates
        .Find(
            X509FindType.FindByThumbprint,
            builder.Configuration["AzureADCertThumbprint"],
            validOnly: false)
            .OfType<X509Certificate2>()
            .Single();

    builder.Configuration.AddAzureKeyVault(
        new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
        new ClientCertificateCredential(
            builder.Configuration["AzureADDirectoryId"],
            builder.Configuration["AzureADApplicationId"],
            x509cert));
}

var host = builder.Build();
host.Run();

