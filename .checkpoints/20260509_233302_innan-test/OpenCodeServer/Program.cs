using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
  options.ListenAnyIP(6000);
});
builder.Services.AddEndpointsApiExplorer();
// Swagger generation is optional for local development. If Swagger is needed, enable the package
// Swashbuckle.AspNetCore and uncomment the next line.
// builder.Services.AddSwaggerGen();
var app = builder.Build();

var sampleFiles = new Dictionary<string, string> {
  { "docs/PROJECT_INDEX.md", "# Project Index\n\nThis is a sample." },
  { "docs/README.md", "# README\n\nJarvis OpenCode Server." },
  { "docs/SESSION_LOG.md", "## Session log sample" }
};

app.MapGet("/health", () => Results.Ok(new { status = "ok", version = "0.1.0" }));

app.MapGet("/files", (string path) => {
  if (string.IsNullOrWhiteSpace(path)) path = "";
  var list = sampleFiles.Keys
    .Where(k => k.StartsWith(path, System.StringComparison.OrdinalIgnoreCase))
    .ToList();
  return Results.Ok(list);
});

app.MapGet("/read", (string path) => {
  if (path != null && sampleFiles.TryGetValue(path, out var content)) return Results.Ok(content);
  return Results.NotFound();
});

app.MapPost("/pending-change", async (System.Text.Json.JsonElement payload) => {
  var json = payload.GetRawText();
  return Results.Ok(new { status = "pending", payload = json });
});

// Optional token gate for remote access; set OPENCODE_TOKEN in env to enable
// If not set, server is open on localhost for local testing.
var token = Environment.GetEnvironmentVariable("OPENCODE_TOKEN");
if (!string.IsNullOrEmpty(token))
{
  app.Use(async (ctx, next) => {
    // allow health check unauthenticated
    if (ctx.Request.Path.StartsWithSegments("/health")) { await next(); return; }
    if (!ctx.Request.Headers.ContainsKey("Authorization")) { ctx.Response.StatusCode = 401; await ctx.Response.WriteAsync("Unauthorized"); return; }
    var auth = ctx.Request.Headers["Authorization"].ToString();
    if (!auth.StartsWith("Bearer ")) { ctx.Response.StatusCode = 401; await ctx.Response.WriteAsync("Unauthorized"); return; }
    var provided = auth.Substring("Bearer ".Length);
    if (provided != token) { ctx.Response.StatusCode = 401; await ctx.Response.WriteAsync("Unauthorized"); return; }
    await next();
  });
}

app.Run("http://0.0.0.0:6000");
