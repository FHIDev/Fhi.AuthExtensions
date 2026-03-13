using Fhi.Samples.EndUser.Angular.BFFApi.Services.DPoP;
using Fhi.Samples.EndUser.Angular.BFFApi.Services.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
builder.Services.AddControllers();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// In-session memory for lagring av Dpop proofs og tokens
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IDPoPKeyStore, InMemoryDPoPKeyStore>();
builder.Services.AddSingleton<ITokenStore, SessionTokenStore>();
builder.Services.AddSingleton<IDPoPProofGenerator, DPoPProofGenerator>();


var app = builder.Build();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();