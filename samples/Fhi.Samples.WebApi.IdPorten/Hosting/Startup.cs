using Fhi.Samples.WebApi.IdPorten.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Fhi.Samples.WebApi.IdPorten.Hosting;

public static class Startup
{
    private const string IdPorten = "IdPorten";

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>()
            ?? new AuthenticationSettings();

        var altinnSettings = builder.Configuration.GetSection("Altinn").Get<AltinnSettings>()
            ?? new AltinnSettings();

        /********************************************************************************************************
         * Authentication — standard JWT bearer with ID-Porten
         ********************************************************************************************************/
        builder.Services.AddAuthentication()
            .AddJwtBearer(IdPorten, options =>
            {
                options.Authority = authSettings.Authority;
                options.Audience = authSettings.Audience;
                options.MapInboundClaims = false;
            });

        /********************************************************************************************************
         * Authorization
         ********************************************************************************************************/
        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(IdPorten)
                .Build());

        /********************************************************************************************************
         * Altinn authorization service
         ********************************************************************************************************/
        builder.Services.AddSingleton(altinnSettings);
        builder.Services.AddHttpClient<AltinnAuthorizationService>();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
