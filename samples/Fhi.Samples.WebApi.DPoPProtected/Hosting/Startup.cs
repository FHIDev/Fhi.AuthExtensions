using Microsoft.AspNetCore.Authorization;

namespace Fhi.Samples.WebApi.DPoPProtected.Hosting;

public static class Startup
{
    private const string HelseIdDPoP = "HelseIdDPoP";

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>()
            ?? new AuthenticationSettings();

        /********************************************************************************************************
         * Authentication — DPoP only
         ********************************************************************************************************/
        builder.Services.AddAuthentication()
            .AddJwtDpop(HelseIdDPoP, options =>
            {
                options.Authority = authSettings.Authority;
                options.Audience = authSettings.Audience;
            });

        /********************************************************************************************************
         * Authorization
         ********************************************************************************************************/
        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

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
