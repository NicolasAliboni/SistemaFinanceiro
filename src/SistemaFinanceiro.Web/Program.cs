using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SistemaFinanceiro.Web;
using SistemaFinanceiro.Web.Configuration;
using SistemaFinanceiro.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using SistemaFinanceiro.Web.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["Api:BaseUrl"];

if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri)
    || (apiUri.Scheme != Uri.UriSchemeHttps
        && apiUri.Scheme != Uri.UriSchemeHttp))
{
    throw new InvalidOperationException(
        "Configure um endereço HTTP ou HTTPS válido em Api:BaseUrl.");
}

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = apiUri
});

var supabaseUrl = builder.Configuration["Supabase:Url"];
var publishableKey = builder.Configuration["Supabase:PublishableKey"];

if (!Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var supabaseUri)
    || supabaseUri.Scheme != Uri.UriSchemeHttps)
{
    throw new InvalidOperationException(
        "Configure um endereço HTTPS válido em Supabase:Url.");
}

if (string.IsNullOrWhiteSpace(publishableKey)
    || !publishableKey.StartsWith(
        "sb_publishable_", StringComparison.Ordinal))
{
    throw new InvalidOperationException(
        "Configure uma Publishable key válida em Supabase:PublishableKey.");
}

builder.Services.AddSingleton(new SupabaseSettings
{
    Url = supabaseUri,
    PublishableKey = publishableKey
});

builder.Services.AddScoped<AuthService>();

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<
    AuthenticationStateProvider,
    SupabaseAuthenticationStateProvider>();

builder.Services.AddScoped<ApiService>();

await builder.Build().RunAsync();