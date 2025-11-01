using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
       .WriteTo.Console(
           outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
           theme: AnsiConsoleTheme.Code)
       .MinimumLevel.Information()
       .MinimumLevel.Override("Duende", LogEventLevel.Verbose)
       .Enrich.FromLogContext());

builder
    .ConfigureServices();


var app = builder.Build();
app.ConfigurePipeline();
app.Run();

