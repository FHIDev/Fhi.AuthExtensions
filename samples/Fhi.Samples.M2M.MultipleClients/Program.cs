public partial class Program
{
    public static void Main(string[] args)
    {
        var variant = Environment.GetEnvironmentVariable("PROGRAM_VARIANT") ?? "default";
        var builder = variant switch
        {
            "MultipleClientsNoExtensions" => CreateHostBuilderMultiHttpClients(args),
            "MultipleClientsUsingExtensions" => CreateHostBuilderUsingExtensions(args),
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
