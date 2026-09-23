using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SistemaFinanceiro.Web.Configuration;
using SistemaFinanceiro.Web.Models;
using System.Net.Http.Headers;

namespace SistemaFinanceiro.Web.Services;

public sealed class AuthService : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _publishableKey;

    public AuthSession? Session { get; private set; }

    public DateTimeOffset? SessionExpiresAt { get; private set; }

    public event Action? SessionChanged;

    public bool HasValidSession =>
        Session is not null
        && SessionExpiresAt is DateTimeOffset expiresAt
        && expiresAt > DateTimeOffset.UtcNow;

    private void ClearSession()
    {
        Session = null;
        SessionExpiresAt = null;
        SessionChanged?.Invoke();
    }

    public AuthService(SupabaseSettings settings)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(settings.Url, "/auth/v1/"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _publishableKey = settings.PublishableKey;
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        ClearSession();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "token?grant_type=password");

        request.Headers.Add("apikey", _publishableKey);

        request.Content = JsonContent.Create(new
        {
            email = email.Trim(),
            password
        });

        try
        {
            using var response = await _http.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return "Muitas tentativas. Aguarde um pouco e tente novamente.";
            }

            if (!response.IsSuccessStatusCode)
            {
                return "Não foi possível entrar. Confira suas credenciais "
                    + "e a configuração de acesso no Supabase.";
            }

            var session =
                await response.Content.ReadFromJsonAsync<AuthSession>();

            if (session is null
                || string.IsNullOrWhiteSpace(session.AccessToken)
                || string.IsNullOrWhiteSpace(session.RefreshToken)
                || session.ExpiresIn <= 0)
            {
                return "O serviço de autenticação retornou uma sessão inválida.";
            }

            Session = session;

            SessionExpiresAt = DateTimeOffset.UtcNow
                .AddSeconds(Math.Max(0, session.ExpiresIn - 30));

            SessionChanged?.Invoke();

            return null;
        }
        catch (HttpRequestException)
        {
            return "Não foi possível conectar ao Supabase. Verifique sua conexão.";
        }
        catch (OperationCanceledException)
        {
            return "A conexão demorou demais. Tente novamente.";
        }
        catch (JsonException)
        {
            return "Não foi possível interpretar a resposta do Supabase.";
        }
    }
    public async Task<string?> LogoutAsync()
    {
        var accessToken = Session?.AccessToken;

        ClearSession();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "logout?scope=local");

        request.Headers.Add("apikey", _publishableKey);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await _http.SendAsync(request);

            return response.IsSuccessStatusCode
                ? null
                : "Você saiu deste aplicativo, mas não foi possível "
                    + "confirmar o encerramento da sessão no Supabase.";
        }
        catch (HttpRequestException)
        {
            return "Você saiu deste aplicativo, mas a conexão falhou "
                + "ao encerrar a sessão no Supabase.";
        }
        catch (OperationCanceledException)
        {
            return "Você saiu deste aplicativo, mas o Supabase "
                + "não respondeu a tempo.";
        }
    }
    public void Dispose()
    {
        _http.Dispose();
    }
}