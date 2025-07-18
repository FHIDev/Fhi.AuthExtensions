using Duende.IdentityModel;
using Fhi.Authorization;
using Microsoft.AspNetCore.Authorization;
using WebApi.Authorization;
using WebApi.Services;

namespace WebApi
{
    public static class Startup
    {
        public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var authenticationSettingsSection = builder.Configuration.GetSection("Authentication");
            builder.Services.Configure<AuthenticationSettings>(authenticationSettingsSection);
            var authenticationSettings = authenticationSettingsSection.Get<AuthenticationSettings>();

            /********************************************************************************************************
            * Authentication
            ********************************************************************************************************/
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "bearer.me";
                options.DefaultChallengeScheme = "bearer.me";
            })
                .AddJwtBearer("bearer.me", options =>
                {
                    options.Audience = authenticationSettings?.Audience;
                    options.Authority = authenticationSettings?.Authority;
                })
                //Sample of having different handling for some endpoints with policies or setup trust with another OIDC provider
                .AddJwtBearer("bearer.integration", options =>
                {
                    options.Audience = authenticationSettings?.Audience;
                    options.Authority = authenticationSettings?.Authority;
                });

            builder.Services.AddTransient<IHealthRecordService, HealthRecordService>();

            /********************************************************************************************************
             * Authorization
             ********************************************************************************************************/
            builder.Services.AddSingleton<IAuthorizationHandler, ScopeHandler>();
            builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, DefaultAccessControlMiddleware>();

            builder.Services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                            .RequireAuthenticatedUser()
                            .Build())
                .AddPolicy("EndUserPolicy", policy =>
                {
                    policy.AuthenticationSchemes.Add("bearer.me");
                    policy.RequireClaim(JwtClaimTypes.Subject);
                    //Ensure the end-user "sub" claim is present
                    policy.RequireClaim(JwtClaimTypes.Subject);
                    policy.RequireAuthenticatedUser();
                })
                .AddPolicy("IntegrationPolicy", policy =>
                 {
                     policy.AuthenticationSchemes.Add("bearer.integration");
                     policy.RequireAuthenticatedUser();
                     policy.RequireAssertion(context =>
                     {
                         // Ensure the end-user "sub" claim is not present
                         return !context.User.HasClaim(c => c.Type == "sub");
                     });
                 });

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
}
