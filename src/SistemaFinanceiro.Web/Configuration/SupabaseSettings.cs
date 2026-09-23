namespace SistemaFinanceiro.Web.Configuration;

public sealed class SupabaseSettings
{
    public required Uri Url { get; init; }

    public required string PublishableKey { get; init; }
}