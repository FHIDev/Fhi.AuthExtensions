using Fhi.Samples.EndUser.Angular.BFFApi.Services.DPoP;
using Fhi.Samples.EndUser.Angular.BFFApi.Services.Tokens;
using Fhi.Samples.EndUser.Angular.BFFApi.Services.IDPorten;
using Fhi.Samples.EndUser.Angular.BFFApi.HttpHandler;
using Fhi.Samples.EndUser.Angular.BFFApi.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAngularCors();
builder.Services.AddControllers();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// DPoP handler + HttpClient
builder.Services.AddTransient<DPoPHttpMessageHandler>();

builder.Services.AddHttpClient("dpop")
    .AddHttpMessageHandler<DPoPHttpMessageHandler>();

builder.Services.AddHttpClient();

// DPoP + Token services
builder.Services.AddSingleton<IDPoPKeyStore, InMemoryDPoPKeyStore>();
builder.Services.AddSingleton<ITokenStore, SessionTokenStore>();
builder.Services.AddSingleton<IDPoPProofGenerator, DPoPProofGenerator>();
builder.Services.AddScoped<IIDPortenService, IDPortenService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();