
namespace Fhi.Authentication.ClientCredentials
{
    /// <summary>
    /// Provides constant parameter names used for client credential authentication.
    /// </summary>
    /// <remarks>Use these constants when constructing authentication requests that require specific parameter names
    /// for client credentials, such as private JSON Web Keys (JWK) or issuer identifiers. This class is intended to promote
    /// consistency and reduce errors when referencing parameter names in authentication flows.</remarks>
    public static class ClientCredentialParameter
    {
        /// <summary>
        /// Gets the key name used to identify a private JSON Web Key (JWK) in configuration or data stores.
        /// </summary>
        public static string PrivateJwk => "PrivateJwk";

        /// <summary>
        /// Gets the claim type identifier for the issuer claim, as defined by the default claim type schema.
        /// </summary>
        public static string Issuer => "Issuer";
    }
}
