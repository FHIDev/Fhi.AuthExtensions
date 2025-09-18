namespace WebApi
{
    internal class AuthenticationSettings
    {
        public string? Audience { get; set; }

        public string? Authority { get; set; }
    }

    internal class AuthenticationSchemes
    {
        public const string HelseIdBearer = "HelseIdBearer";
        public const string HelseIdDPoP = "HelseIdDPoP";
        public const string Duende = "Duende";
        public const string MaskinPorten = "MaskinPorten";
        public const string IdPorten = "ID-Porten";
    }

    internal class Policies
    {
        public const string EndUserPolicy = "EndUserPolicy";
        public const string IntegrationPolicy = "IntegrationPolicy";
    }
}