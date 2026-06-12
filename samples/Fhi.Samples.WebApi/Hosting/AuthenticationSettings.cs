namespace Api.WebApi.Hosting
{
    internal class AuthenticationSettings
    {
        public string? Audience { get; set; }

        public string Authority { get; set; } = string.Empty;
    }

    internal class AuthenticationSchemes
    {
        public const string HelseIdDPoP = "HelseIdDPoP";
        public const string Duende = "Duende";
        public const string MaskinPorten = "MaskinPorten";
    }

    internal class Policies
    {
        public const string EndUserPolicy = "EndUserPolicy";
        public const string IntegrationPolicy = "IntegrationPolicy";
    }
}