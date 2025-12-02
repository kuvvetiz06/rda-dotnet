// PATH: RDA.Infrastructure/Documents/Llm/OllamaLlmExtractionService.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RDA.Application.Documents.Services;
using RDA.Domain.Documents;

namespace RDA.Infrastructure.Documents.Llm;

public class OllamaLlmExtractionService : ILlmExtractionService
{
    private const int MaxRetries = 3;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaLlmExtractionService> _logger;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private readonly string _model;

    public OllamaLlmExtractionService(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaLlmExtractionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _model = configuration["Ollama:Model"] ?? "llama3.1";
    }

    public async Task<DocumentExtractionResult> ExtractFieldsAsync(string ocrText, string instructionPrompt, CancellationToken cancellationToken = default)
    {
        var userMessage = $"Prompt: {instructionPrompt}\n\nMetin:\n<<<{ocrText}>>>\n\nLütfen sadece geçerli JSON döndür.";

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var response = await SendChatRequestAsync(userMessage, cancellationToken);

                if (response.StatusCode == HttpStatusCode.InternalServerError || response.StatusCode == HttpStatusCode.BadGateway || response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    await BackoffAsync(attempt, cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var failureMessage = $"LLM request failed with status code {response.StatusCode}.";
                    _logger.LogError(failureMessage);
                    return new DocumentExtractionResult
                    {
                        Success = false,
                        ErrorMessage = failureMessage
                    };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var llmPayload = JsonSerializer.Deserialize<OllamaChatResponse>(content, _serializerOptions);
                var messageContent = llmPayload?.Message?.Content;

                if (string.IsNullOrWhiteSpace(messageContent))
                {
                    var missingContentMessage = "LLM response did not contain content.";
                    _logger.LogError(missingContentMessage);
                    return new DocumentExtractionResult
                    {
                        Success = false,
                        ErrorMessage = missingContentMessage
                    };
                }

                if (TryDeserializeExtraction(messageContent, out var extraction, out var parseError))
                {
                    return extraction;
                }

                _logger.LogError("LLM JSON parse error on attempt {Attempt}: {Error}.", attempt, parseError);
                if (attempt >= MaxRetries)
                {
                    return new DocumentExtractionResult
                    {
                        Success = false,
                        ErrorMessage = "LLM JSON parse error"
                    };
                }

                await BackoffAsync(attempt, cancellationToken);
            }
            catch (HttpRequestException httpEx) when (IsTransient(httpEx))
            {
                _logger.LogWarning(httpEx, "Transient LLM call failure on attempt {Attempt}.", attempt);
                if (attempt >= MaxRetries)
                {
                    return new DocumentExtractionResult
                    {
                        Success = false,
                        ErrorMessage = "LLM request failed after retries"
                    };
                }

                await BackoffAsync(attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during LLM extraction.");
                return new DocumentExtractionResult
                {
                    Success = false,
                    ErrorMessage = "Unexpected error during LLM extraction"
                };
            }
        }

        return new DocumentExtractionResult
        {
            Success = false,
            ErrorMessage = "LLM extraction failed after retries"
        };
    }

    private async Task<HttpResponseMessage> SendChatRequestAsync(string userMessage, CancellationToken cancellationToken)
    {
        var payload = new OllamaChatRequest
        {
            Model = _model,
            Stream = false,
            Messages = new List<OllamaMessage>
            {
                new() { Role = "system", Content = "Sen bir alan çıkarma asistanısın. Girdi olarak uzun bir sözleşme metni alacaksın. Çıktı olarak her zaman sadece geçerli JSON döneceksin. JSON dışına herhangi bir metin yazma." },
                new() { Role = "user", Content = userMessage }
            }
        };

        return await _httpClient.PostAsJsonAsync("/api/chat", payload, _serializerOptions, cancellationToken);
    }

    private static bool IsTransient(HttpRequestException httpEx)
    {
        return httpEx.StatusCode == null || (int)httpEx.StatusCode >= 500;
    }

    private static async Task BackoffAsync(int attempt, CancellationToken cancellationToken)
    {
        var delaySeconds = Math.Pow(2, attempt - 1);
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
    }

    private bool TryDeserializeExtraction(string llmContent, out DocumentExtractionResult? extraction, out string? parseError)
    {
        parseError = null;

        foreach (var candidate in GetJsonCandidates(llmContent))
        {
            try
            {
                var result = JsonSerializer.Deserialize<DocumentExtractionResult>(candidate, _serializerOptions);
                if (result != null)
                {
                    result.RawText ??= llmContent;
                    extraction = result;
                    return true;
                }
            }
            catch (JsonException jsonEx)
            {
                parseError = jsonEx.Message;
            }
        }

        parseError ??= "No JSON content found in LLM response.";
        extraction = null;
        return false;
    }

    private static IEnumerable<string> GetJsonCandidates(string llmContent)
    {
        var trimmed = llmContent.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            yield return trimmed;
        }

        var fenced = ExtractFromCodeFence(trimmed);
        if (!string.IsNullOrEmpty(fenced) && !string.Equals(fenced, trimmed, StringComparison.Ordinal))
        {
            yield return fenced;
        }

        var bracketed = ExtractFromBraces(trimmed);
        if (!string.IsNullOrEmpty(bracketed) && !string.Equals(bracketed, trimmed, StringComparison.Ordinal))
        {
            yield return bracketed;
        }
    }

    private static string? ExtractFromCodeFence(string content)
    {
        var fenceMatch = Regex.Match(content, "```(json)?\\s*([\\s\\S]*?)```", RegexOptions.IgnoreCase);
        if (!fenceMatch.Success)
        {
            return null;
        }

        var inner = fenceMatch.Groups.Count >= 3 ? fenceMatch.Groups[2].Value : string.Empty;
        return inner.Trim();
    }

    private static string? ExtractFromBraces(string content)
    {
        var startIndex = content.IndexOf('{');
        var endIndex = content.LastIndexOf('}');

        if (startIndex < 0 || endIndex <= startIndex)
        {
            return null;
        }

        return content[startIndex..(endIndex + 1)];
    }

    private class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OllamaMessage> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaResponseMessage? Message { get; set; }
    }

    private class OllamaResponseMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
