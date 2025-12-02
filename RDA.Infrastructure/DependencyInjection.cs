// PATH: RDA.Infrastructure/DependencyInjection.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RDA.Application.Documents.Services;
using RDA.Infrastructure.Documents.Llm;
using RDA.Infrastructure.Documents.Ocr;

namespace RDA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IOcrService, TesseractOcrService>();

        services.AddHttpClient<ILlmExtractionService, OllamaLlmExtractionService>((sp, client) =>
        {
            var baseUrl = configuration["Ollama:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl);
            }
        });

        return services;
    }
}
