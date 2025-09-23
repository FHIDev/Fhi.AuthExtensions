public partial class Program
{
    public static void Main(string[] args)
    {
        var variant = Environment.GetEnvironmentVariable("PROGRAM_VARIANT") ?? "default";
        var builder = variant switch
        {
            "MultipleClientVariant1" => CreateHostBuilderMultipleClientVariant1(args),
            "MultipleClientVariant2" => CreateHostBuilderMultiClientVariant2(args),
            "Refit" => CreateHostBuilderRefit(args),
            _ => CreateHostBuilderDefault(args)
        };

        builder.Build().Run();
    }

    public static IHostBuilder CreateHostBuilderDefault(string[] args) =>
       Host.CreateDefaultBuilder(args)
           .ConfigureServices((context, services) =>
           {
           });
}
