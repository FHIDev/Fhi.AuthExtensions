// HttpLogger.csx
using System.Net.Http;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;

public static class HttpLogger
{
    public static async Task LogRequest(HttpRequestMessage request)
    {
        Console.WriteLine("====== HTTP REQUEST ======");
        Console.WriteLine($"{request.Method} {request.RequestUri}");

        foreach (var h in request.Headers)
            Console.WriteLine($"Header: {h.Key} {string.Join(", ", h.Value)}");

        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            TryPrintJson(content);
        }

        Console.WriteLine("===========================");
    }

    public static async Task LogResponse(HttpResponseMessage response)
    {
        Console.WriteLine("====== HTTP RESPONSE ======");
        Console.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");

        foreach (var h in response.Headers)
            Console.WriteLine($"Header: {h.Key} {string.Join(", ", h.Value)}");

        if (response.Content != null)
        {
            var content = await response.Content.ReadAsStringAsync();
            TryPrintJson(content);
        }

        Console.WriteLine("============================");
    }

    private static void TryPrintJson(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var pretty = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("Content (JSON):");
            Console.WriteLine(pretty);
        }
        catch
        {
            Console.WriteLine($"Content: {content}");
        }
    }
}
