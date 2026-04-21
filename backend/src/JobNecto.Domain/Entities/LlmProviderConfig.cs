using JobNecto.Domain.Enums;

namespace JobNecto.Domain.Entities;

public class LlmProviderConfig
{
    public LlmProvider LlmProvider;
    public string? ApiKey;
    public string? BaseUrl;
    public string? Model;
    public double? Temperature;
}
