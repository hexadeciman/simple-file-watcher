using FileStreamer.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FileStreamer.Auth;

internal static class SupabaseJwtAuthenticationExtensions
{
    public static IServiceCollection AddSupabaseJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(SupabaseAuthOptions.SectionName)
            .Get<SupabaseAuthOptions>() ?? new SupabaseAuthOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtOptions =>
            {
                var supabaseUrl = options.Url.TrimEnd('/');

                jwtOptions.Authority = $"{supabaseUrl}/auth/v1";
                jwtOptions.MetadataAddress = $"{supabaseUrl}/auth/v1/.well-known/openid-configuration";
                jwtOptions.MapInboundClaims = false;
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"{supabaseUrl}/auth/v1",
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.HttpContext.Request.Path == JsonWebSocketRoutes.Path)
                        {
                            context.Token = context.HttpContext.Request.Query["token"];
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(authorizationOptions =>
        {
            authorizationOptions.AddPolicy(AuthPolicies.AllowedUser, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("sub", options.AllowedUserId);
            });
        });

        return services;
    }
}

internal static class AuthPolicies
{
    public const string AllowedUser = "AllowedUser";
}

internal static class JsonWebSocketRoutes
{
    public const string Path = "/ws";
}
