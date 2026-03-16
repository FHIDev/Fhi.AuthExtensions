using Fhi.Samples.EndUser.Angular.BFFApi.Services.DPoP;

namespace Fhi.Samples.EndUser.Angular.BFFApi.HttpHandler
{
    public class DPoPHttpMessageHandler(
        IDPoPKeyStore keyStore,
        IDPoPProofGenerator proofGen) : DelegatingHandler
    {
        private readonly IDPoPKeyStore _keyStore = keyStore;
        private readonly IDPoPProofGenerator _proofGen = proofGen;

        /// <summary>
        /// Overrides the normal HTTP request to attach a new DPoP proof for every request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Get the DPoP key pair
            var keyPair = await _keyStore.GetKeyPairAsync();

            // Build the full URL for the htu claim
            var url = request.RequestUri!.ToString();

            // Generate a fresh DPoP proof for this request
            var proof = await _proofGen.CreateProofAsync(
                request.Method.Method,
                url,
                keyPair);

            // Attach the DPoP header
            request.Headers.Remove("DPoP");
            request.Headers.Add("DPoP", proof);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}