using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JarvisPocketBridge
{
    public class PocketBridge
    {
        private readonly HttpClient _http;
        private readonly string _apiEndpoint;

        public PocketBridge(string apiEndpoint = null)
        {
            _http = new HttpClient();
            _apiEndpoint = apiEndpoint ?? "http://localhost:6000";
        }

        public bool IsAvailable()
        {
            try
            {
                var task = _http.GetAsync($"{_apiEndpoint}/health");
                task.Wait(1000);
                return task.Result.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<string[]> ListSnippetsAsync(string path = "")
        {
            if (!IsAvailable()) return Array.Empty<string>();
            var r = await _http.GetAsync($"{_apiEndpoint}/files?path={Uri.EscapeDataString(path)}");
            if (!r.IsSuccessStatusCode) return Array.Empty<string>();
            var json = await r.Content.ReadAsStringAsync();
            // Return raw JSON as a placeholder; will be parsed to a structured model later.
            return new string[] { json };
        }

        public async Task<string> ReadSnippetAsync(string filePath)
        {
            if (!IsAvailable()) return null;
            var r = await _http.GetAsync($"{_apiEndpoint}/read?path={Uri.EscapeDataString(filePath)}");
            if (!r.IsSuccessStatusCode) return null;
            return await r.Content.ReadAsStringAsync();
        }

        public async Task<string> ImportSnippetAsync(string snippet)
        {
            var payload = new
            {
                targetPath = "PocketImport/snippet.txt",
                changeType = "FileCreate",
                content = snippet,
                reason = "Imported from PocketBridge"
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var res = await _http.PostAsync($"{_apiEndpoint}/pending-change", content);
            if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadAsStringAsync();
            }
            return null;
        }
    }
}
