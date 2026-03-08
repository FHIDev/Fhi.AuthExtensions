namespace Fhi.Authentication.JwtDPoP.Validation.Models
{
    internal record DpopValidationResult(
        bool IsError = false,
        string? Error = null,
        string? ErrorDescription = null);
}
