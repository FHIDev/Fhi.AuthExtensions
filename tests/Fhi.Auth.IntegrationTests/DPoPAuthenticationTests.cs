using Fhi.Auth.IntegrationTests.Setup;
using Fhi.Authentication.JwtDPoP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text.RegularExpressions;

namespace Fhi.Auth.IntegrationTests
{
    /// <summary>
    /// Krav til bruk av www-authenticate header ved 401 respons:
    /// https://datatracker.ietf.org/doc/html/rfc6750#section-3.1
    /// https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/WWW-Authenticate#syntax
    ///
    /// DPoP: validering krav:
    /// - https://www.rfc-editor.org/rfc/rfc9449.html#name-protected-resource-access
    /// - https://www.rfc-editor.org/rfc/rfc9449.html#checking
    /// </summary>
    public class DPoPAuthenticationTests
    {
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_valid_DPoP_token_and_proof_THEN_returns_200()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "http://authority",
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token);
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        #region Authentication scheme
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_no_token_THEN_returns_401_with_DPoP_challenge()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop())
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP");
        }

        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_invalid_authentication_scheme_THEN_returns_401_with_DPoP_challenge()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop())
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token);
            client.AddBearerAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP");
        }

        [Test]
        public async Task GIVEN_accessing_Bearer_protected_endpoint_WHEN_using_DPoP_valid_token_THEN_returns_401_with_Bearer_challenge()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth
                    .AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = "http://authority",
                        ValidAudience = "api_audience",
                        IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                    })
                    .AddJwtBearer())
                .AppPipeline(app =>
                {
                    app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful DPoP access");
                    app.MapGet("/api/bearerEndpoint", [Authorize(AuthenticationSchemes = "Bearer")] () => "Successful Bearer access");
                })
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token);
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var dpopResponse = await client.GetAsync("/api/dpopEndpoint");
            Assert.That(dpopResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var bearerResponse = await client.GetAsync("/api/bearerEndpoint");
            Assert.That(bearerResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            bearerResponse.AssertWWWAuthenticate("Bearer");
        }

        #endregion

        #region Validate DPoPProof token

        /// <summary>
        /// 1. There is not more than one DPoP HTTP request header field.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_no_DPoP_proof_header_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop())
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            client.AddDPoPAuthorizationHeader(token);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP error=\"invalid_request\", error_description=\"Missing DPOP header\"");
        }

        /// <summary>
        /// 1. There is not more than one DPoP HTTP request header field.
        /// </summary>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_multiple_DPoP_headers_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "http://authority",
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token);
            client.AddDPoPAuthorizationHeader(token)
                .AddDPoPHeader(proof)
                .AddDPoPHeader(proof); // second DPoP header

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP error=\"invalid_request\"");
        }

        /// <summary>
        /// 2. The DPoP HTTP request header field value is a single and well-formed JWT.
        /// </summary>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_DPoP_proof_is_malformed_JWT_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "http://authority",
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            client.AddDPoPAuthorizationHeader(token);
            client.DefaultRequestHeaders.Add("DPoP", "not.a.valid.jwt");

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("error=\"invalid_dpop_proof\", error_description=\"DPoP proof is malformed\"");
        }

        /// <summary>
        /// 3. All required claims (jti, htm, htu, iat, ath) are present.
        /// </summary>
        [TestCase("jti")]
        [TestCase("htm")]
        [TestCase("htu")]
        [TestCase("iat")]
        [TestCase("ath")]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_proof_missing_required_claim_THEN_returns_401(string missingClaim)
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "http://authority",
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = missingClaim switch
            {
                "jti" => FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token, jti: null),
                "htm" => FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token, htm: null),
                "htu" => FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token, htu: null),
                "iat" => FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token, iat: null),
                "ath" => FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token, ath: null),
                _ => throw new ArgumentException($"Unknown claim: {missingClaim}"),
            };
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate($"DPoP error=\"invalid_dpop_proof\", error_description=\"Missing required claim {missingClaim}\"");
        }

        /// <summary>
        /// 4. The typ JOSE Header Parameter has the value dpop+jwt
        /// </summary>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_dpopProof_has_wrong_typ_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
               .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
               {
                   ValidIssuer = "http://authority",
                   ValidAudience = "api_audience",
                   IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
               }))
               .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
               .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token, typ: "JWT");
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP error=\"invalid_dpop_proof\", error_description=\"typ header must be dpop+jwt\"");
        }

        /// <summary>
        /// 5. The alg JOSE Header Parameter indicates a registered asymmetric digital signature algorithm
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_invalid_dpop_jwk_algorithm_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options =>
                {
                    options.DPoPProotTokenValidationParameters.ValidAlgorithms = new[]
                    {
                        SecurityAlgorithms.RsaSha512
                    };
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = "http://authority",
                        ValidAudience = "api_audience",
                        IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey
                    };
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token, alg: SecurityAlgorithms.RsaSha256);
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("error=\"invalid_dpop_proof\", error_description=\"Disallowed algorithm\"");
        }

        /// <summary>
        /// 6. The JWT signature verifies with the public key contained in the jwk JOSE Header Parameter
        /// </summary>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_DPoPProof_invalid_signature_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "http://authority",
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProofWithInvalidSignature("http://localhost/api/dpopEndpoint", "GET", token);
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP error=\"invalid_dpop_proof\", error_description=\"Invalid DPoP proof signature.\"");
        }


        /// <summary>
        /// 7. The jwk JOSE Header Parameter does not contain a private key
        /// </summary>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_dpopProof_contains_private_key_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "http://authority",
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProofWithPrivateKey("http://localhost/api/dpopEndpoint", "GET", token);
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP error=\"invalid_dpop_proof\"");
        }


        /// <summary>
        /// 8. The htm claim matches the HTTP method of the current request.
        /// </summary>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_proof_htm_does_not_match_request_method_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "http://authority",
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "POST", token); // wrong method
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP error=\"invalid_dpop_proof\"");
        }


        /// <summary>
        /// 9. The htu claim matches the HTTP URI value for the HTTP request in which the JWT was received, ignoring any query and fragment parts.
        /// </summary>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_htu_host_does_not_match_request_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "http://authority",
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://wrong-host/api/dpopEndpoint", "GET", token);
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP error=\"invalid_dpop_proof\", error_description=\"htu claim does not match the request URI\"");
        }

        /// <summary>
        /// 10. If the server provided a nonce value to the client, the nonce claim matches the server-provided nonce value
        /// </summary>
        [Ignore("implementeres mulig senere")]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_require_nonce_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options =>
                {
                    //options.DPoPProotTokenValidationParameters.ProofTokenLifetimeValidationType = ProofLifetimeValidationType.Nonce;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = "http://authority",
                        ValidAudience = "api_audience",
                        IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                    };
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token, iat: DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds());
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP error=\"invalid_dpop_proof\"");
        }


        /// <summary>
        /// 11. The JWT was created within an acceptable window of time (iat is not expired).
        /// </summary>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_proof_iat_is_expired_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options =>
                {
                    options.DPoPProotTokenValidationParameters.ProofTokenLifetimeValidationType = ProofLifetimeValidationType.IssuedAt;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = "http://authority",
                        ValidAudience = "api_audience",
                        IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                    };
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token, iat: DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds());
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP error=\"invalid_dpop_proof\"");
        }

        /// <summary>
        /// 12. The ath claim matches the hash of the access token.
        /// </summary>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_proof_ath_does_not_match_access_token_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "http://authority",
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", "non-valid-accesstoken");
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP error=\"invalid_dpop_proof\"");
        }

        /// <summary>
        /// 12. ensure that the value of the ath claim equals the hash of that access token
        /// </summary>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_ath_claim_not_mathing_access_token_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "http://authority",
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", "invalid_token");
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("error=\"invalid_dpop_proof\", error_description=\"ath claim does not match the access token hash");
        }

        /// <summary>
        /// https://www.rfc-editor.org/rfc/rfc9449.html#name-public-key-confirmation
        /// 
        /// 12. Validate access token cnf value (jkt). The public key from the DPoP proof matches the one bound to the access token.
        /// cnf jkt does not match proof key thumbprint. The public key in the access token cnf claim matches the public key in the DPoP proof.
        /// Case: access token has no jkt in cnf claim
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_invalid_token_binding_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://any-issuer", "api_audience", jkt: "invalid_proof_binding");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token);
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP error=\"invalid_dpop_proof\", error_description=\"invalid token binding\"");
        }




        #endregion

        #region Validate Access Token (DPoP bound token)


        [TestCase("http://invalid_issuer", "api_audience")]
        [TestCase("http://authority", "invalid_audience")]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_invalid_audience_or_issuer_THEN_returns_401(string issuer, string audience)
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "http://authority",
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken(issuer, audience);
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token);
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            response.AssertWWWAuthenticate("DPoP error=\"invalid_token\"");
        }



        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_issuer_validation_off_THEN_returns_200()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://any-issuer", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token);
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var response = await client.GetAsync("/api/dpopEndpoint");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        #endregion

        /// <summary>
        /// https://www.rfc-editor.org/rfc/rfc9449.html#name-dpop-proof-replay
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task GIVEN_accessing_DPoPprotected_endpoint_WHEN_jti_replayed_THEN_returns_401()
        {
            var client = new DPoPTestServerBuilder()
                .AddServiceConfiguration(auth => auth.AddJwtDpop(configure: options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "http://authority",
                    ValidAudience = "api_audience",
                    IssuerSigningKey = FakeDPoPTokenBuilder.SecurityKey,
                }))
                .AppPipeline(app => app.MapGet("/api/dpopEndpoint", [Authorize(AuthenticationSchemes = "DPoP")] () => "Successful access"))
                .Start();

            var token = FakeDPoPTokenBuilder.CreateDPoPToken("http://authority", "api_audience");
            var proof = FakeDPoPTokenBuilder.CreateDPoPProof("http://localhost/api/dpopEndpoint", "GET", token, jti: "fixed-jti");
            client.AddDPoPAuthorizationHeader(token).AddDPoPHeader(proof);

            var firstResponse = await client.GetAsync("/api/dpopEndpoint");
            Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            client.DefaultRequestHeaders.Remove("DPoP");
            client.DefaultRequestHeaders.Add("DPoP", proof);

            var replayResponse = await client.GetAsync("/api/dpopEndpoint");
            Assert.That(replayResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            replayResponse.AssertWWWAuthenticate("DPoP error=\"invalid_dpop_proof\", error_description=\"Detected DPoP proof token replay");
        }


    }

    internal static class AssertExtensions
    {
        internal static void AssertWWWAuthenticate(this HttpResponseMessage response, string pattern)
        {
            if (response.Headers.TryGetValues("WWW-Authenticate", out var values))
            {
                var wwwAuthenticate = values.FirstOrDefault();
                Assert.That(wwwAuthenticate, Does.Match(Regex.Escape(pattern)));
            }
            else
            {
                Assert.Fail("WWW-Authenticate header was not present in the response");
            }
        }
    }
}
