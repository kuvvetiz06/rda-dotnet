// PATH: MyApp.Infrastructure/DependencyInjection.cs
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Application.Documents.Services;
using MyApp.Infrastructure.Documents.Llm;
using MyApp.Infrastructure.Documents.Ocr;

namespace MyApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IOcrService, TesseractOcrService>();
        services.AddHttpClient<ILlmExtractionService, OllamaLlmExtractionService>(client =>
        {
            var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        });

        return services;
    }
}
