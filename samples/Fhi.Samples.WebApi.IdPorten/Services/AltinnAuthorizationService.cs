using Fhi.Samples.WebApi.IdPorten.Endpoints.v1.Dtos;
using Fhi.Samples.WebApi.IdPorten.Hosting;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;

namespace Fhi.Samples.WebApi.IdPorten.Services;

/// <summary>
/// Calls the Altinn Authorization API to check whether the authenticated user (identified by their
/// Norwegian personal identification number from the ID-Porten token) has access to a configured resource.
///
/// Flow:
///   1. Exchange the user's ID-Porten access token for an Altinn-specific token.
///   2. Send an XACML 3.0 authorization decision request to Altinn's PDP endpoint.
/// </summary>
public class AltinnAuthorizationService
{
    private readonly HttpClient _httpClient;
    private readonly AltinnSettings _settings;

    public AltinnAuthorizationService(HttpClient httpClient, AltinnSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    /// <summary>
    /// Checks whether the user identified by <paramref name="pid"/> (Norwegian personal id number)
    /// is authorized to perform the configured action on the configured resource.
    /// </summary>
    /// <param name="idPortenAccessToken">The raw bearer token received from ID-Porten.</param>
    /// <param name="pid">The user's Norwegian personal identification number (from the "pid" claim).</param>
    public async Task<AltinnDecision> CheckAuthorizationAsync(string idPortenAccessToken, string pid)
    {
        /************************************************************************************
         * Step 1: Exchange the ID-Porten token for an Altinn token.
         * POST {BaseUrl}/authentication/api/v1/exchange/id-porten
         * Authorization: Bearer {idPortenToken}
         * Returns: a JWT string representing the Altinn session token.
         ************************************************************************************/
        var altinnToken = await ExchangeTokenAsync(idPortenAccessToken);
        if (altinnToken is null)
            return new AltinnDecision("Error", "Failed to exchange ID-Porten token for Altinn token.");

        /************************************************************************************
         * Step 2: Send an XACML 3.0 authorization decision request.
         * POST {BaseUrl}/authorization/api/v1/decision
         * Authorization: Bearer {altinnToken}
         * Content-Type: application/xml
         * Body: XACML 3.0 request with subject (pid), resource, and action.
         ************************************************************************************/
        return await GetDecisionAsync(altinnToken, pid);
    }

    private async Task<string?> ExchangeTokenAsync(string idPortenToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_settings.BaseUrl}/authentication/api/v1/exchange/id-porten");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idPortenToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync();
        // Response is a plain JWT string (possibly JSON-quoted)
        return content.Trim('"');
    }

    private async Task<AltinnDecision> GetDecisionAsync(string altinnToken, string pid)
    {
        var xacmlRequest = BuildXacmlRequest(pid, _settings.ResourceId, _settings.Action);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_settings.BaseUrl}/authorization/api/v1/decision");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", altinnToken);
        request.Content = new StringContent(xacmlRequest, Encoding.UTF8, "application/xml");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new AltinnDecision("Error", $"Altinn returned HTTP {(int)response.StatusCode}.");

        var xml = await response.Content.ReadAsStringAsync();
        return ParseXacmlResponse(xml);
    }

    private static string BuildXacmlRequest(string pid, string resourceId, string action)
    {
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Request xmlns="urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"
                     xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                     ReturnPolicyIdList="false">
              <Attributes Category="urn:oasis:names:tc:xacml:1.0:subject-category:access-subject">
                <Attribute AttributeId="urn:altinn:person:identifier-no" IncludeInResult="false">
                  <AttributeValue DataType="http://www.w3.org/2001/XMLSchema#string">{pid}</AttributeValue>
                </Attribute>
              </Attributes>
              <Attributes Category="urn:oasis:names:tc:xacml:3.0:attribute-category:resource">
                <Attribute AttributeId="urn:altinn:resource" IncludeInResult="false">
                  <AttributeValue DataType="http://www.w3.org/2001/XMLSchema#string">{resourceId}</AttributeValue>
                </Attribute>
              </Attributes>
              <Attributes Category="urn:oasis:names:tc:xacml:3.0:attribute-category:action">
                <Attribute AttributeId="urn:oasis:names:tc:xacml:1.0:action:action-id" IncludeInResult="false">
                  <AttributeValue DataType="http://www.w3.org/2001/XMLSchema#string">{action}</AttributeValue>
                </Attribute>
              </Attributes>
            </Request>
            """;
    }

    private static AltinnDecision ParseXacmlResponse(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("xacml", "urn:oasis:names:tc:xacml:3.0:core:schema:wd-17");

            var decisionNode = doc.SelectSingleNode("//xacml:Decision", ns);
            var decision = decisionNode?.InnerText?.Trim() ?? "Indeterminate";
            return new AltinnDecision(decision);
        }
        catch (Exception ex)
        {
            return new AltinnDecision("Error", $"Failed to parse Altinn response: {ex.Message}");
        }
    }
}
