using System.Net;
using System.Net.Http.Headers;

namespace SistemaFinanceiro.Web.Services;

public sealed class ApiService
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;

    public ApiService(HttpClient http, AuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    public async Task<string> TestarAcessoAsync()
    {
        if (!_auth.HasValidSession || _auth.Session is null)
        {
            return "Sua sessão expirou. Entre novamente.";
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/me");

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _auth.Session.AccessToken);

        try
        {
            using var response = await _http.SendAsync(request);

            return response.StatusCode switch
            {
                HttpStatusCode.OK =>
                    "HTTP 200: acesso autorizado pela API.",

                HttpStatusCode.Unauthorized =>
                    "HTTP 401: a API não aceitou o token. "
                    + "Saia e entre novamente.",

                HttpStatusCode.Forbidden =>
                    "HTTP 403: usuário autenticado, mas sem permissão. "
                    + "Confira o OwnerUserId configurado na API.",

                _ =>
                    $"A API retornou HTTP {(int)response.StatusCode}."
            };
        }
        catch (HttpRequestException)
        {
            return "Não foi possível acessar a API. "
                + "Confira se ela está rodando, o endereço HTTPS "
                + "e eventuais erros de CORS no navegador.";
        }
        catch (OperationCanceledException)
        {
            return "A API demorou para responder. Tente novamente.";
        }
    }
}