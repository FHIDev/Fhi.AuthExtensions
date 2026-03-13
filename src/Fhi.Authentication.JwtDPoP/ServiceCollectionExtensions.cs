using Fhi.Authentication.JwtDPoP;
using Fhi.Authentication.JwtDPoP.Validation;
using Fhi.Authentication.JwtDPoP.Validation.DPoPProofValidators;
using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Claims;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds JWT DPoP (Demonstration Proof of Possession) authentication support to the specified authentication
        /// builder.
        /// </summary>
        /// <param name="builder">The authentication builder to which JWT DPoP support will be added.</param>
        /// <param name="authenticationScheme">The authentication scheme to use for JWT DPoP authentication.</param>
        /// <param name="configure">Optional configuration for DPoP token authentication options.</param>
        /// <returns>The updated authentication builder with JWT DPoP support configured.</returns>
        public static AuthenticationBuilder AddJwtDpop(
            this AuthenticationBuilder builder,
            string authenticationScheme,
            Action<JwtDPoPOptions>? configure = null)
        {
            builder.Services.AddDistributedMemoryCache();
            builder.Services.TryAddSingleton(TimeProvider.System);
            builder.Services.TryAddSingleton<IReplayCache, ReplayCache>();

            builder.Services.TryAddTransient<JwtSignatureValidator>();
            builder.Services.TryAddTransient<AthMatchValidator>();
            builder.Services.TryAddTransient<JoseHeaderAlgorithmPolicyValidator>();
            builder.Services.TryAddTransient<JoseHeaderJwkValidator>();
            builder.Services.TryAddTransient<HttpMethodMatchValidator>();
            builder.Services.TryAddTransient<HttpUriMatchValidator>();
            builder.Services.TryAddTransient<IatLifetimeValidator>();
            builder.Services.TryAddTransient<KeyBindingMatchValidator>();
            builder.Services.TryAddTransient<JtiLengthGuardValidator>();
            builder.Services.TryAddTransient<JtiReplayValidator>();
            builder.Services.TryAddTransient<DPoPProofCompositeValidator>();

            builder.Services.AddTransient<IDPoPProofHandler, DPoPHandler>();

            var dpopOptions = new JwtDPoPOptions();
            configure?.Invoke(dpopOptions);

            builder.AddJwtBearer(authenticationScheme, jwtOptions =>
            {
                if (dpopOptions.Authority != null)
                    jwtOptions.Authority = dpopOptions.Authority;
                if (dpopOptions.Audience != null)
                    jwtOptions.Audience = dpopOptions.Audience;
                if (dpopOptions.MetadataAddress != null)
                    jwtOptions.MetadataAddress = dpopOptions.MetadataAddress;
                jwtOptions.RequireHttpsMetadata = dpopOptions.RequireHttpsMetadata;
                jwtOptions.SaveToken = dpopOptions.SaveToken;
                jwtOptions.TokenValidationParameters = dpopOptions.TokenValidationParameters;
                jwtOptions.Challenge = DPoPConstants.Scheme;
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnMessageReceived = async context =>
                    {
                        var validator = context.HttpContext.RequestServices.GetRequiredService<IDPoPProofHandler>();
                        var result = await validator.ValidateRequest(new DPoPProofRequestValidationContext(context.Request, dpopOptions.DPoPProofTokenValidationParameters));
                        if (result.IsError)
                        {
                            context.Fail(result.ErrorDescription ?? result.Error ?? "DPoP validation failed");
                            context.HttpContext.Items[DPoPConstants.ItemPropertyName.DPoPFailureCode] = result.Error;
                            context.HttpContext.Items[DPoPConstants.ItemPropertyName.DPoPFailureDescription] = result.ErrorDescription;
                        }
                        context.Token = TryGetDPoPAccessToken(context.Request.Headers.Authorization.FirstOrDefault());
                    },
                    OnTokenValidated = async context =>
                    {
                        context.Request.Headers.TryGetValue(DPoPConstants.DPoPHeaderName, out var dpopHeader);
                        var validator = context.HttpContext.RequestServices.GetRequiredService<IDPoPProofHandler>();
                        var result = await validator.ValidateDPoPProof(new DPoPValidationContext
                        {
                            ProofToken = dpopHeader.FirstOrDefault() ?? string.Empty,
                            AccessToken = TryGetDPoPAccessToken(context.Request.Headers.Authorization.FirstOrDefault()),
                            AccessTokenClaims = context.Principal?.Claims ?? Array.Empty<Claim>(),
                            ExpectedMethod = context.HttpContext.Request.Method,
                            ExpectedUrl = $"{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}{context.HttpContext.Request.PathBase}{context.HttpContext.Request.Path}",
                            ValidationParameters = dpopOptions.DPoPProofTokenValidationParameters
                        });

                        if (result.IsError)
                        {
                            context.Fail(result.ErrorDescription ?? result.Error ?? "DPoP validation failed");
                            context.HttpContext.Items[DPoPConstants.ItemPropertyName.DPoPFailureCode] = result.Error;
                            context.HttpContext.Items[DPoPConstants.ItemPropertyName.DPoPFailureDescription] = result.ErrorDescription;
                        }
                    },
                    OnChallenge = context =>
                    {
                        context.Error = context.HttpContext.Items[DPoPConstants.ItemPropertyName.DPoPFailureCode] as string ?? "invalid_token";
                        var desc = context.HttpContext.Items[DPoPConstants.ItemPropertyName.DPoPFailureDescription] as string;
                        if (!string.IsNullOrEmpty(desc))
                            context.ErrorDescription = desc;
                        return Task.CompletedTask;
                    }
                };
            });

            return builder;
        }

        private static string TryGetDPoPAccessToken(string? header)
        {
            var dpopPrefix = $"{DPoPConstants.Scheme} ";
            if (header?.StartsWith(dpopPrefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                return header.Substring(dpopPrefix.Length).Trim();
            }

            return string.Empty;
        }
    }
}
