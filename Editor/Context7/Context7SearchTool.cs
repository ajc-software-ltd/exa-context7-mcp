using System;
using System.Threading.Tasks;
using MCP.ExaContext7.Editor;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Security;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

namespace MCP.ExaContext7.Editor.Context7
{
    /// <summary>
    /// One-shot Context7 search (v3) that resolves and queries in a single call.
    /// </summary>
    [McpForUnityTool("context7_search",
        Description = "One-shot Context7 search: resolves libraries and returns relevant documentation " +
                      "snippets in a single call (no separate library ID needed). Alternative to the " +
                      "two-step context7_resolve_library + context7_query_docs flow.",
        StructuredOutput = true)]
    public static class Context7SearchTool
    {
        public class Parameters
        {
            [ToolParameter("Search query describing what you want to know.", Required = true)]
            public string Query { get; set; }

            [ToolParameter("Hint libraries to search (up to 4), e.g. ['unity', 'cinemachine'].")]
            public string[] Library { get; set; }

            [ToolParameter("Prefer documentation in this language (e.g. 'C#').")]
            public string Language { get; set; }
        }

        public static async Task<object> HandleCommand(JObject @params)
        {
            if (@params == null) return new ErrorResponse("Parameters cannot be null.");
            var p = new ToolParams(@params);

            var query = p.GetRequired("query");
            if (!query.IsSuccess) return new ErrorResponse(query.ErrorMessage);

            if (!ExaContext7Keys.TryGetContext7Key(out var apiKey))
                return new ErrorResponse(
                    "Context7 API key not configured. Set the CONTEXT7_API_KEY environment variable " +
                    "or add a key in Window/MCP for EXA & Context7.");

            var url = "https://context7.com/api/v3/search" +
                      $"?query={Uri.EscapeDataString(query.Value.Trim())}" +
                      "&type=json";

            var libraries = p.GetStringArray("library");
            if (libraries is { Length: > 0 })
            {
                var limited = libraries.Length > 4 ? 4 : libraries.Length;
                for (int i = 0; i < limited; i++)
                    url += $"&library={Uri.EscapeDataString(libraries[i].Trim())}";
            }

            var language = p.Get("language");
            if (!string.IsNullOrWhiteSpace(language))
                url += $"&language={Uri.EscapeDataString(language.Trim())}";

            try
            {
                var result = await ExaContext7Http.GetAsync(url, "Authorization", $"Bearer {apiKey}");

                if (!result.IsSuccess)
                    return new ErrorResponse(
                        $"Context7 API error (HTTP {result.StatusCode}): {SecretRedactor.Scrub(result.Error ?? result.Text ?? "unknown")}");

                var parsed = JToken.Parse(result.Text ?? "{}");
                var data = new JObject
                {
                    ["meta"] = parsed["meta"] ?? new JObject(),
                    ["searchResults"] = parsed["searchResults"] ?? new JObject(),
                    ["infoSnippets"] = parsed["infoSnippets"] as JArray ?? new JArray(),
                    ["codeSnippets"] = parsed["codeSnippets"] as JArray ?? new JArray()
                };
                return new SuccessResponse("Search completed.", data);
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Context7 search failed: {SecretRedactor.Scrub(e.Message)}");
            }
        }
    }
}
