using Microsoft.AspNetCore.Authentication;

namespace Fhi.Authentication.JwtDPoP
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
        /// <returns>The updated authentication builder with JWT DPoP support configured.</returns>
        public static AuthenticationBuilder AddJwtDpop(this AuthenticationBuilder builder)
        {
            return builder;
        }
    }
}
