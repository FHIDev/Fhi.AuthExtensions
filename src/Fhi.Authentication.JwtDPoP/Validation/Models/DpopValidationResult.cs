namespace Fhi.Authentication.JwtDPoP.Validation.Models
{
    internal record DPoPValidationResult(
        bool IsError = false,
        string? Error = null,
        string? ErrorDescription = null);
}
