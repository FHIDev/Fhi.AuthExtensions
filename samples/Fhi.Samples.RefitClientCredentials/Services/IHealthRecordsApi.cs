using Refit;

namespace Fhi.Samples.RefitClientCredentials.Services;

/// <summary>
/// Refit interface for the Weather API.
/// This interface represents the WeatherForecast endpoint that works with client credentials authentication.
/// </summary>
public interface IHealthRecordsApi
{
    /// <summary>
    /// Gets weather forecast from the WebApi /WeatherForecast endpoint.
    /// This endpoint uses the "bearer.integration" authentication scheme and requires "fhi:weather/access" scope.
    /// </summary>
    /// <returns>A collection of weather forecasts.</returns>
    [Get("/WeatherForecast")]
    Task<IEnumerable<WeatherForecastDto>> GetWeatherForecastAsync();

    [Get("/V2/Datamin/Clients")]
    Task<ApiResponse<List<string>>> GetClients();

    [Post("/V2/Person/ById")]
    Task<ApiResponse<string>> GetPersonById([Body] PersonRequest requestData);
}

/// <summary>
/// Data Transfer Object representing a weather forecast from the WebApi WeatherForecast endpoint.
/// </summary>
public record WeatherForecastDto(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

/// <summary>
/// Data Transfer Object representing a health record from the WebApi user endpoint.
/// This matches the HealthRecordPersonDto from the WebApi /api/v1/me/health-records endpoint.
/// </summary>
public record HealthRecordPersonDto(string Pid, string Name, string Description, DateTime CreatedAt);

public class PersonRequest
{
    public string Fnr { get; set; } = default!;

    public string Id { get; set; } = default!;

    public string Key { get; set; } = default!;

    public bool InclHistory { get; set; } = default!;


    public InfoParts InfoParts { get; set; } = default!;

    public ConstructParts ConstructParts { get; set; } = default!;

    public bool InclRawdata { get; set; } = default!;
}

[Flags]
public enum InfoParts : long
{
    None = 0,
    AddressProtection = 1 << 0, // 1
    Birth = 1 << 1, // 2
    BirthInNorway = 1 << 2, // 4
    Citizenship = 1 << 3, // 8
    CitizenshipRetention = 1 << 4, // Expected unneccessary
    CommonContactRegisterInformation = 1 << 5, // 32
    Death = 1 << 6, // 64
    DeprivedLegalAuthority = 1 << 7, // Expected unneccessary
    EmigrationFromNorway = 1 << 8, // 256
    FamilyRelation = 1 << 9, // 512
    FalseIdentity = 1 << 10, // Not sure, can wait // 1024
    ForeignPersonIdentificationNumber = 1 << 11, // 2048
    ForeignPostalAddress = 1 << 12, // 4096
    Gender = 1 << 13, // 8192
    GuardianshipOrFuturePowerOfAttorney = 1 << 14, // 16384
    IdentificationDocument = 1 << 15, // Expected unneccessary
    IdentityVerification = 1 << 16, // Expected unneccessary
    ImmigrationAuthoritiesIdentificationNumber = 1 << 17, // 131072
    ImmigrationToNorway = 1 << 18, // 262144
    MaritalStatus = 1 << 19, // 524288
    Name = 1 << 20, // 1048576
    NorwegianIdentificationNumber = 1 << 21, // 2097152
    ParentalResponsibility = 1 << 22, // 4194304
    PreferredContactAddress = 1 << 23, // Deprecated
    PresentAddress = 1 << 24, // 16777216
    PostalAddress = 1 << 25, // 33554432
    ResiduaryEstateContactInformation = 1 << 26, // Expected unneccessary
    ResidencePermit = 1 << 27, // Expected unneccessary
    ResidentialAddress = 1 << 28, // 268435456
    SamiParliamentElectoralRegistryStatus = 1 << 29, // Expected unneccessary
    SharedResidence = 1 << 30, // 1073741824
    StayOnSvalbard = 1L << 31, // Expected unneccessary
    Status = 1L << 32, // 2147483648
    UseOfSamiLanguage = 1L << 33, // Expected unneccessary

    Alle = (1L << 34) - 1,
    //  Combined values

    // Addresses = AddressProtection | PostalAddress | ForeignPostalAddress | ResidentialAddress,  = 5695765359
    AllFhi = AddressProtection
             | Birth
             | BirthInNorway
             | Citizenship
             | CommonContactRegisterInformation
             | Death
             | EmigrationFromNorway
             | FamilyRelation
             | ForeignPersonIdentificationNumber
             | ForeignPostalAddress
             | Gender
             | GuardianshipOrFuturePowerOfAttorney
             | ImmigrationAuthoritiesIdentificationNumber
             | ImmigrationToNorway
             | MaritalStatus
             | Name
             | NorwegianIdentificationNumber
             | ParentalResponsibility
             | PresentAddress
             | PostalAddress
             | ResidentialAddress
             | SharedResidence
             | Status,

    Kjerneinfo = NorwegianIdentificationNumber
                 | AddressProtection
                 | Birth
                 | Death
                 | Name
                 | Gender
                 | Status
                 | ImmigrationAuthoritiesIdentificationNumber
}

[Flags]
public enum ConstructParts : long
{
    None = 0,
    Kjerneinfo = 1 << 0, // 1
    Bostedskommune = 1 << 1, // 2
    Familierelasjoner = 1 << 2, // 4,
    Bostedsadresse = 1 << 3, // 8
    DeltBosted = 1 << 4, // 16
    Oppholdsadresse = 1 << 5, // 32
    Postadresse = 1 << 6, // 64
    PostadresseIUtlandet = 1 << 7, // 128
    Fodselsdetaljer = 1 << 8, // 256
    InnOgUtflytting = 1 << 9, // 512
    BostedsadresseHistorikk = 1 << 10, // 1024
    Identifikasjonsnummerhistorikk = 1 << 11, // 2048
    KrrBasis = 1 << 12, // 4096
    KrrFull = 1 << 13, // 8192
    Foreldreansvar = 1 << 14, // 16384

    Alle = Kjerneinfo
           | Bostedskommune
           | Familierelasjoner
           | Bostedsadresse
           | DeltBosted
           | Oppholdsadresse
           | Postadresse
           | PostadresseIUtlandet
           | Fodselsdetaljer
           | InnOgUtflytting
           | BostedsadresseHistorikk
           | Identifikasjonsnummerhistorikk
           | KrrBasis
           | KrrFull
           | Foreldreansvar
}