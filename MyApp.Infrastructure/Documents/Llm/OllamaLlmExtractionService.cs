// PATH: MyApp.Infrastructure/Documents/Llm/OllamaLlmExtractionService.cs
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyApp.Application.Documents.Services;
using MyApp.Domain.Documents;

namespace MyApp.Infrastructure.Documents.Llm;

public class OllamaLlmExtractionService : ILlmExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaLlmExtractionService> _logger;
    private readonly string _model;

    public OllamaLlmExtractionService(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaLlmExtractionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _model = configuration["Ollama:Model"] ?? "llama3.1";
    }

    public async Task<DocumentExtractionResult> ExtractFieldsAsync(string ocrText, string instructionPrompt, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = "Sen bir alan çıkarma asistanısın. Girdi olarak uzun bir sözleşme metni alacaksın. Çıktı olarak her zaman sadece geçerli JSON döneceksin. JSON dışına herhangi bir metin yazma." },
                new { role = "user", content = $"Prompt: {instructionPrompt}\n\nMetin:\n<<<{ocrText}>>>" }
            },
            stream = false
        };

        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync("api/chat", payload, cancellationToken);

                if ((int)response.StatusCode >= 500)
                {
                    throw new HttpRequestException($"Server error {response.StatusCode}");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync(cancellationToken);
                    return new DocumentExtractionResult
                    {
                        Success = false,
                        ErrorMessage = $"LLM extraction failed: {response.StatusCode} - {message}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var result = JsonSerializer.Deserialize<DocumentExtractionResult>(content, options);
                    if (result == null)
                    {
                        throw new JsonException("Deserialized result is null");
                    }

                    if (string.IsNullOrWhiteSpace(result.RawText))
                    {
                        result.RawText = ocrText;
                    }

                    return result;
                }
                catch (Exception ex) when (ex is JsonException || ex is NotSupportedException)
                {
                    _logger.LogError(ex, "LLM JSON parse error. Attempt {Attempt}. TODO: avoid logging full LLM output.", attempt);
                    if (attempt == maxAttempts)
                    {
                        return new DocumentExtractionResult
                        {
                            Success = false,
                            ErrorMessage = "LLM JSON parse error"
                        };
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "LLM HTTP error on attempt {Attempt}.", attempt);
                if (attempt == maxAttempts)
                {
                    return new DocumentExtractionResult
                    {
                        Success = false,
                        ErrorMessage = "LLM request failed"
                    };
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "LLM request timed out on attempt {Attempt}.", attempt);
                if (attempt == maxAttempts)
                {
                    return new DocumentExtractionResult
                    {
                        Success = false,
                        ErrorMessage = "LLM request timed out"
                    };
                }
            }

            var delaySeconds = Math.Pow(2, attempt - 1);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
        }

        return new DocumentExtractionResult
        {
            Success = false,
            ErrorMessage = "LLM extraction failed after retries"
        };
    }
}
