using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SistemaFinanceiro.Web.Services;

namespace SistemaFinanceiro.Web.Auth;

public sealed class SupabaseAuthenticationStateProvider
    : AuthenticationStateProvider, IDisposable
{
    private readonly AuthService _auth;
    private readonly Timer _timer;
    private bool _wasAuthenticated;

    public SupabaseAuthenticationStateProvider(AuthService auth)
    {
        _auth = auth;
        _wasAuthenticated = auth.HasValidSession;
        _auth.SessionChanged += OnSessionChanged;

        // Atualiza a interface quando a sessão expira.
        _timer = new Timer(
            _ => CheckExpiration(),
            null,
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(15));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = _auth.HasValidSession
            ? new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Name, "Usuário")
                },
                authenticationType: "Supabase")
            : new ClaimsIdentity();

        var user = new ClaimsPrincipal(identity);

        return Task.FromResult(new AuthenticationState(user));
    }

    private void OnSessionChanged()
    {
        _wasAuthenticated = _auth.HasValidSession;

        NotifyAuthenticationStateChanged(
            GetAuthenticationStateAsync());
    }

    private void CheckExpiration()
    {
        if (_wasAuthenticated != _auth.HasValidSession)
        {
            OnSessionChanged();
        }
    }

    public void Dispose()
    {
        _auth.SessionChanged -= OnSessionChanged;
        _timer.Dispose();
    }
}