using Fhi.Authentication.Tokens;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Fhi.Authentication.OpenIdConnect
{
    /// <summary>
    /// Extensions for OpenIdConnect events.
    /// </summary>
    public static class OpenIdConnectEventsExtensions
    {
        private const string ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
        /// <summary>
        /// This method is used to create a client assertion for the authorization code flow.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="privateJwk"></param>
        /// <returns></returns>
        public static async Task AuthorizationCodeReceivedWithClientAssertionAsync(this AuthorizationCodeReceivedContext context, string privateJwk)
        {
            await AuthorizationCodeReceivedWithClientAssertionAsync(context, context.Options.ClientId!, privateJwk);
        }

        /// <summary>
        /// This method is used to create a client assertion for the authorization code flow.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="clientId"></param>
        /// <param name="privateJwk"></param>
        /// <returns></returns>
        public static async Task AuthorizationCodeReceivedWithClientAssertionAsync(this AuthorizationCodeReceivedContext context, string clientId, string privateJwk)
        {
            if (context.Options.ConfigurationManager is not null)
            {
                var discovery = await context.Options.ConfigurationManager.GetConfigurationAsync(default);

                context.TokenEndpointRequest!.ClientAssertionType = ClientAssertionType;
                context.TokenEndpointRequest.ClientAssertion = ClientAssertionTokenHandler.CreateJwtToken(discovery.Issuer, clientId, privateJwk);
            }
        }

#if NET9_0

        /// <summary>
        /// This method is used to create a client assertion for the authorization code flow.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="privateJwk"></param>
        /// <returns></returns>
        public static async Task PushAuthorizationWithClientAssertion(this PushedAuthorizationContext context, string privateJwk)
        {
            await PushAuthorizationWithClientAssertion(context, context.Options.ClientId!, privateJwk);
        }
        /// <summary>
        /// This method is used to create a client assertion for the authorization code flow.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="clientId"></param>
        /// <param name="jwk"></param>
        /// <returns></returns>
        public static async Task PushAuthorizationWithClientAssertion(this PushedAuthorizationContext context, string clientId, string jwk)
        {
            if (context.Options.ConfigurationManager is not null)
            {
                var discovery = await context.Options.ConfigurationManager.GetConfigurationAsync(default);

                context.ProtocolMessage.ClientAssertionType = ClientAssertionType;
                context.ProtocolMessage.ClientAssertion = ClientAssertionTokenHandler.CreateJwtToken(discovery.Issuer, clientId, jwk);
            }
        }
#endif
    }
}
