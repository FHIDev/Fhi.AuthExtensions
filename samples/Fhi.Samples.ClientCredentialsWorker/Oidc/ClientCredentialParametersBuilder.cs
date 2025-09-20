using Duende.IdentityModel.Client;

namespace Fhi.Samples.WorkerServiceMultipleClients.Oidc
{
    public static class ClientCredentialParameter
    {
        public static string PrivateJwk => "PrivateJwk";
        public static string Issuer => "Issuer";
    }
    public class ClientCredentialParametersBuilder
    {
        private readonly List<KeyValuePair<string, string>> _parameters = new();

        public ClientCredentialParametersBuilder AddIssuer(string? issuer)
        {
            _parameters.Add(new KeyValuePair<string, string>(ClientCredentialParameter.Issuer, issuer ?? string.Empty));
            return this;
        }

        public ClientCredentialParametersBuilder AddPrivateJwk(string jwk)
        {
            _parameters.Add(new KeyValuePair<string, string>(ClientCredentialParameter.PrivateJwk, jwk ?? string.Empty));
            return this;
        }

        public Parameters Build()
        {
            return [.. _parameters];
        }
    }
}
