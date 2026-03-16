using System.Text.Json;
using Fhi.Samples.EndUser.Angular.BFFApi.Services.Models;

namespace Fhi.Samples.EndUser.Angular.BFFApi.Services.Tokens
{
    /// <summary>
    /// Stores the user's OIDC token set inside the ASP.NET session.
    /// </summary>
    public class SessionTokenStore : ITokenStore
    {
        // The session key under which the serialized TokenSet is stored.
        private const string SessionKey = "OIDC_TOKEN_SET";

        /// <summary>
        /// Saves the token set to the user's session.
        /// The TokenSet is serialized to JSON and stored as a string.
        /// </summary>
        public Task SaveAsync(HttpContext context, TokenSet tokens)
        {
            var json = JsonSerializer.Serialize(tokens);
            context.Session.SetString(SessionKey, json);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Retrieves the token set from the user's session.
        /// Returns null if no tokens are stored (e.g., user not logged in or session expired).
        ///
        /// The JSON is deserialized back into a TokenSet object.
        /// </summary>
        public Task<TokenSet?> GetAsync(HttpContext context)
        {
            var json = context.Session.GetString(SessionKey);

            if (string.IsNullOrEmpty(json))
                return Task.FromResult<TokenSet?>(null);

            try
            {
                var tokens = JsonSerializer.Deserialize<TokenSet>(json);
                return Task.FromResult(tokens);
            }
            catch
            {
                // If deserialization fails, clear the corrupted session entry.
                context.Session.Remove(SessionKey);
                return Task.FromResult<TokenSet?>(null);
            }
        }

        /// <summary>
        /// Removes the token set from the user's session.
        /// Used during logout or when tokens are invalid/expired.
        /// </summary>
        public Task ClearAsync(HttpContext context)
        {
            context.Session.Remove(SessionKey);
            return Task.CompletedTask;
        }
    }
}