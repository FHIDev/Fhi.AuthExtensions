using System.ComponentModel.DataAnnotations;

namespace Fhi.Authentication.Extensions.UnitTests;

[TestFixture]
public class RefitClientCredentialsOptionsTests
{
    [Test]    
    public void SectionName_ShouldHaveCorrectValue()
    {
        var sectionName = RefitClientCredentialsOptions.SectionName;

        Assert.That(sectionName, Is.EqualTo("RefitClientCredentials"));
    }

    [Test]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        var options = new RefitClientCredentialsOptions();

        Assert.That(options.ClientName, Is.EqualTo("default"));
        Assert.That(options.TokenEndpoint, Is.EqualTo(string.Empty));
        Assert.That(options.ClientId, Is.EqualTo(string.Empty));
        Assert.That(options.ClientSecret, Is.Null);
        Assert.That(options.Scope, Is.Null);
        Assert.That(options.ApiBaseUrl, Is.Null);
    }

    [Test]
    public void DataAnnotations_ShouldValidateRequiredFields()
    {
        var options = new RefitClientCredentialsOptions();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results, Has.Count.EqualTo(2)); // TokenEndpoint and ClientId are required
        Assert.That(results.Any(r => r.MemberNames.Contains("TokenEndpoint")), Is.True);
        Assert.That(results.Any(r => r.MemberNames.Contains("ClientId")), Is.True);
    }

    [Test]
    public void DataAnnotations_ShouldPassWithValidData()
    {
        var options = new RefitClientCredentialsOptions
        {
            TokenEndpoint = "https://auth.example.com/token",
            ClientId = "test-client-id"
        };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();       
        var isValid = Validator.TryValidateObject(options, context, results, true);

        Assert.That(isValid, Is.True);
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void UrlValidation_ShouldValidateApiBaseUrl()
    {
        var options = new RefitClientCredentialsOptions
        {
            TokenEndpoint = "https://auth.example.com/token",
            ClientId = "test-client-id",
            ApiBaseUrl = "not-a-valid-url"
        };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains("ApiBaseUrl")), Is.True);
    }
}
