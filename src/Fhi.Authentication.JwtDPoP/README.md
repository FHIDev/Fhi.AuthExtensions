# Fhi.Authentication.JwtDPoP


## Documentation

For official documentation, please see the [documentation site](https://fhidev.github.io/Fhi.AuthExtensions/)

## Contributing

See [CONTRIBUTING.md](https://github.com/FHIDev/Fhi.AuthExtensions/blob/main/CONTRIBUTING.md)

## License
See [License.md](https://github.com/FHIDev/Fhi.AuthExtensions/blob/main/License.md)


## Usage



## JOSE‑header validators

JoseHeaderTypeValidator
Krever typ == "dpop+jwt".
Feilkode: InvalidTyp.

JoseHeaderAlgorithmPolicyValidator
Sjekker alg ∈ whitelist (ES256/RS256/PS256 osv.) – aldri none/MAC.
Feilkode: DisallowedAlg.


## 3) Signature validators

ProofSignatureValidator
Verifiserer signaturen med jwk fra header.
Feilkode: InvalidSignature.


## 4) Http request validators

HttptMethodMatchValidator
Sammenligner htm med faktisk HTTP‑metode.
Feilkode: HtmMismatch, MissingRequiredClaim.


HttpUriMatchValidator
Sammenligner htu med target URI uten query/fragment (normaliser skjema/port/trailing slash etter policy).
Feilkode: HtuMismatch, MissingRequiredClaim.


## 5) Lifetime validators

IatLifetimeValidator
Uten nonce: håndhev at iat er innen ProofLifetime ± AllowedClockSkew. Sjekker at iat ligger innenfor en definert lifetime (tidsvindu).
Feilkoder: IatTooOld, IatTooFarInFuture, MissingRequiredClaim.


## 6) Access‑token binding (ressursserver)


AthPresenceAndMatchValidator
Hvis access token er med: krev ath og verifiser SHA‑256(access‑token) == ath.
Feilkoder: MissingRequiredClaim, AthMismatch.


KeyBindingMatchValidator
Sjekk at tokenets key‑binding (cnf.jkt i access‑token eller via introspeksjon) matcher jwk i proof.
Feilkode: KeyBindingMismatch.


## 7) Replay‑beskyttelse (jti)


JtiLengthGuardValidator
Avvis urimelig lange jti (DoS‑vern).
Feilkode: JtiTooLong.


JtiSingleUseValidator
Enforce single‑use per htu i et kort tidsvindu (lagre hash(jti) i store).
Feilkode: JtiReplay.