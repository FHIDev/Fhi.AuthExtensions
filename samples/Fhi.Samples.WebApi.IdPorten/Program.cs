using Fhi.Samples.WebApi.IdPorten.Hosting;

var builder = WebApplication.CreateBuilder(args);

var app = builder
    .ConfigureServices()
    .ConfigurePipeline();

app.Run();
