using Duende.IdentityModel.Client;

namespace Fhi.Authentication.ClientCredentials
{
    /// <summary>
    /// Provides a builder for configuring client credential parameters used when configure ClientCredentials with Duende AccessTokenManagement.
    /// The parameters is used in <see cref="ClientCredentialsAssertionService"/> .
    /// </summary>
    /// <remarks>Use this class to incrementally add parameters to Duende.IdentityModel.Client.Paramters such as issuer and private JWK when
    /// configuring client credentials. The builder pattern allows chaining of parameter additions before producing a
    /// finalized set of parameters with the Build method.</remarks>
    public class ClientCredentialParametersBuilder
    {
        private readonly List<KeyValuePair<string, string>> _parameters = new();

        /// <summary>
        /// Adds an issuer parameter to the client credential parameters collection.
        /// </summary>
        /// <param name="issuer">The issuer value to associate with the client credentials. If <paramref name="issuer"/> is <see
        /// langword="null"/>, an empty string is used.</param>
        /// <returns>The current <see cref="ClientCredentialParametersBuilder"/> instance with the issuer parameter added.</returns>
        public ClientCredentialParametersBuilder AddIssuer(string? issuer)
        {
            _parameters.Add(new KeyValuePair<string, string>(ClientCredentialParameter.Issuer, issuer ?? string.Empty));
            return this;
        }

        /// <summary>
        /// Adds a private JSON Web Key (JWK) to the client credential parameters.
        /// </summary>
        /// <param name="jwk">The private JWK to be included in the parameters. If <paramref name="jwk"/> is null, an empty string is
        /// used.</param>
        /// <returns>The current <see cref="ClientCredentialParametersBuilder"/> instance to allow method chaining.</returns>
        public ClientCredentialParametersBuilder AddPrivateJwk(string jwk)
        {
            _parameters.Add(new KeyValuePair<string, string>(ClientCredentialParameter.PrivateJwk, jwk ?? string.Empty));
            return this;
        }

        /// <summary>
        /// Creates a new <see cref="Parameters"/> instance containing the current set of parameters.
        /// </summary>
        /// <returns>A <see cref="Parameters"/> object that includes all parameters configured in this builder at the time of the
        /// call.</returns>
        public Parameters Build()
        {
            return [.. _parameters];
        }
    }
}
