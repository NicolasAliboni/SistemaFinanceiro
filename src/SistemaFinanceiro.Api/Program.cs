using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configurações do Supabase.
var supabaseUrl = builder.Configuration["Supabase:Url"];
var ownerUserId = builder.Configuration["Supabase:OwnerUserId"];

if (!Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var supabaseUri)
    || supabaseUri.Scheme != Uri.UriSchemeHttps)
{
    throw new InvalidOperationException(
        "Configure um endereço HTTPS válido em Supabase:Url.");
}

if (!Guid.TryParse(ownerUserId, out var ownerId))
{
    throw new InvalidOperationException(
        "Configure o UID do proprietário em Supabase:OwnerUserId.");
}

var issuer = $"{supabaseUri.AbsoluteUri.TrimEnd('/')}/auth/v1";

// Autenticação: verifica se o token é válido.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = issuer;
        options.Audience = "authenticated";
        options.RequireHttpsMetadata = true;

        // Preserva nomes de claims como "sub" e "role".
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = "authenticated",

            ValidateLifetime = true,
            RequireExpirationTime = true,

            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,

            ValidAlgorithms = new[]
            {
                SecurityAlgorithms.EcdsaSha256
            },

            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// Autorização: permite somente o proprietário.
builder.Services.AddAuthorization(options =>
{
    var ownerPolicy = new AuthorizationPolicyBuilder(
        JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .RequireClaim("role", "authenticated")
        .RequireAssertion(context =>
            Guid.TryParse(
                context.User.FindFirst("sub")?.Value,
                out var userId)
            && userId == ownerId)
        .Build();

    options.AddPolicy("OwnerOnly", ownerPolicy);

    // Protege também endpoints que não tenham uma política explícita.
    options.FallbackPolicy = ownerPolicy;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorWeb", policy =>
    {
        policy
            .WithOrigins(
                "https://localhost:7197",
                "http://localhost:5199")
            .WithMethods("GET")
            .WithHeaders("Authorization");
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors("BlazorWeb");

app.UseAuthentication();
app.UseAuthorization();

// Endpoint público para verificar se a API está funcionando.
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "SistemaFinanceiro.Api"
}))
.AllowAnonymous();

// Endpoint protegido para testar a autenticação e a autorização.
app.MapGet("/api/me", () => Results.Ok(new
{
    message = "Acesso autorizado à API."
}))
.RequireAuthorization("OwnerOnly");

app.Run();