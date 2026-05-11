using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JarvisCLI
{
  class Program
  {
    static async Task Main(string[] args)
    {
      using var client = new HttpClient();
      string baseUrl = "http://localhost:6000";
      Console.WriteLine("JarvisCLI: Connect to OpenCodeServer at " + baseUrl);

      while (true)
      {
        Console.Write("jarvis> ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) continue;
        var cmd = input.Trim();
        if (cmd.Equals("exit", StringComparison.OrdinalIgnoreCase) || cmd.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;

        if (cmd.StartsWith("read ", StringComparison.OrdinalIgnoreCase))
        {
          string path = input.Substring(5).Trim();
          var resp = await client.GetAsync(baseUrl + "/read?path=" + Uri.EscapeDataString(path));
          Console.WriteLine(await resp.Content.ReadAsStringAsync());
        }
        else if (cmd.StartsWith("list ", StringComparison.OrdinalIgnoreCase) || cmd.StartsWith("ls ", StringComparison.OrdinalIgnoreCase))
        {
          string path = input.Substring(input.IndexOf(' ')+1).Trim();
          var resp = await client.GetAsync(baseUrl + "/files?path=" + Uri.EscapeDataString(path));
          Console.WriteLine(await resp.Content.ReadAsStringAsync());
        }
        else if (cmd.StartsWith("pending-change", StringComparison.OrdinalIgnoreCase) || cmd.StartsWith("skriv fil", StringComparison.OrdinalIgnoreCase))
        {
          var payload = new
          {
            targetPath = "docs/mobile-test.md",
            changeType = "FileCreate",
            content = "# Mobil-test",
            reason = "JarvisCLI"
          };
          var json = System.Text.Json.JsonSerializer.Serialize(payload);
          var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
          var resp = await client.PostAsync(baseUrl + "/pending-change", content);
          Console.WriteLine(await resp.Content.ReadAsStringAsync());
        }
        else if (cmd.Equals("health", StringComparison.OrdinalIgnoreCase))
        {
          var resp = await client.GetAsync(baseUrl + "/health");
          Console.WriteLine(await resp.Content.ReadAsStringAsync());
        }
        else if (cmd == "help")
        {
          Console.WriteLine("Commands:");
          Console.WriteLine("  read <path>            – läs filens innehåll");
          Console.WriteLine("  list <path> / ls <path> – lista filer i mappen");
          Console.WriteLine("  pending-change         – skicka en skrivändring (förväntar godkännande)");
          Console.WriteLine("  health                 – kontrollera OpenCodeServer");
          Console.WriteLine("  exit                   – avsluta");
        }
        else
        {
          Console.WriteLine("Okänt kommando. Prova: read <path>, list <path>, pending-change, health, help, exit");
        }
      }
    }
  }
}
