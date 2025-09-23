using Duende.IdentityModel;
using Fhi.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

            /********************************************************************************************************
            * Authentication
            ********************************************************************************************************/
            var authenticationBuilder = builder.Services.AddAuthentication();

            var helseIdSection = builder.Configuration.GetSection($"AuthenticationSchemes:{AuthenticationSchemes.HelseIdBearer}");
            var helseIdOptions = helseIdSection.Get<AuthenticationSettings>() ?? new AuthenticationSettings();
            authenticationBuilder
                .AddJwtBearer(AuthenticationSchemes.HelseIdBearer, options =>
                {
                    options.Audience = helseIdOptions.Audience;
                    options.Authority = helseIdOptions.Authority;
                    options.Events = new JwtBearerEvents()
                    {
                        OnChallenge = OnChallenge(AuthenticationSchemes.HelseIdBearer)
                    };

                });

            var helseIdDpopSection = builder.Configuration.GetSection($"AuthenticationSchemes:{AuthenticationSchemes.HelseIdDPoP}");
            var helseIdDpopOptions = helseIdDpopSection.Get<AuthenticationSettings>() ?? new AuthenticationSettings();
            authenticationBuilder
                .AddJwtBearer(AuthenticationSchemes.HelseIdDPoP, options =>
                {
                    options.Audience = helseIdDpopOptions.Audience;
                    options.Authority = helseIdDpopOptions.Authority;
                    options.Events = new JwtBearerEvents()
                    {
                        OnChallenge = OnChallenge(AuthenticationSchemes.HelseIdDPoP)
                    };
                });

            var duendeSection = builder.Configuration.GetSection($"AuthenticationSchemes:{AuthenticationSchemes.Duende}");
            var duendeOptions = duendeSection.Get<AuthenticationSettings>() ?? new AuthenticationSettings();
            authenticationBuilder
                .AddJwtBearer(AuthenticationSchemes.Duende, options =>
                {
                    options.Audience = duendeOptions.Audience;
                    options.Authority = duendeOptions.Authority;
                    options.Events = new JwtBearerEvents()
                    {
                        OnChallenge = OnChallenge(AuthenticationSchemes.Duende)
                    };
                });

            var maskinportenSection = builder.Configuration.GetSection($"AuthenticationSchemes:{AuthenticationSchemes.MaskinPorten}");
            var maskinportenOptions = maskinportenSection.Get<AuthenticationSettings>() ?? new AuthenticationSettings();
            authenticationBuilder
                .AddJwtBearer(AuthenticationSchemes.MaskinPorten, options =>
                {
                    options.Audience = maskinportenOptions.Audience;
                    options.Authority = maskinportenOptions.Authority;
                    options.Events = new JwtBearerEvents()
                    {
                        OnChallenge = OnChallenge(AuthenticationSchemes.MaskinPorten)
                    };
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
                .AddPolicy(Policies.EndUserPolicy, policy =>
                {
                    policy.RequireClaim(JwtClaimTypes.Subject);
                    //Ensure the end-user "sub" claim is present
                    policy.RequireClaim(JwtClaimTypes.Subject);
                    policy.RequireAuthenticatedUser();
                })
                .AddPolicy(Policies.IntegrationPolicy, policy =>
                 {
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

        /// <summary>
        /// This is to illustrate how WWW-Authenticate header can be used. 
        /// </summary>
        /// <param name="scheme"></param>
        /// <returns></returns>
        static Func<JwtBearerChallengeContext, Task> OnChallenge(string scheme)
        {
            return ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.Headers["WWW-Authenticate"] =
                    $"Bearer realm=\"{scheme}\", error=\"invalid_token\", error_description=\"{ctx.ErrorDescription}\"";
                return Task.CompletedTask;
            };
        }
    }
}
