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
    /// Fetch up-to-date library documentation from Context7 for a resolved library ID.
    /// </summary>
    [McpForUnityTool("context7_query_docs",
        Description = "Fetch up-to-date code snippets and documentation for a Context7 library ID " +
                      "(obtained from context7_resolve_library). Returns examples and explanations " +
                      "for the given query.",
        StructuredOutput = true)]
    public static class Context7QueryDocsTool
    {
        public class Parameters
        {
            [ToolParameter("Context7 library ID (e.g. '/unitycom/docs'). Get this from context7_resolve_library.", Required = true)]
            public string LibraryId { get; set; }

            [ToolParameter("Specific concept, API, or question to fetch documentation for.", Required = true)]
            public string Query { get; set; }

            [ToolParameter("Prefer documentation in this language (e.g. 'C#', 'JavaScript').")]
            public string Language { get; set; }
        }

        public static async Task<object> HandleCommand(JObject @params)
        {
            if (@params == null) return new ErrorResponse("Parameters cannot be null.");
            var p = new ToolParams(@params);

            var libraryId = p.GetRequired("library_id");
            if (!libraryId.IsSuccess) return new ErrorResponse(libraryId.ErrorMessage);

            var query = p.GetRequired("query");
            if (!query.IsSuccess) return new ErrorResponse(query.ErrorMessage);

            if (!ExaContext7Keys.TryGetContext7Key(out var apiKey))
                return new ErrorResponse(
                    "Context7 API key not configured. Set the CONTEXT7_API_KEY environment variable " +
                    "or add a key in Window/MCP for EXA & Context7.");

            var url = "https://context7.com/api/v2/context" +
                      $"?libraryId={Uri.EscapeDataString(libraryId.Value.Trim())}" +
                      $"&query={Uri.EscapeDataString(query.Value.Trim())}" +
                      "&type=json";

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
                    ["infoSnippets"] = parsed["infoSnippets"] as JArray ?? new JArray(),
                    ["codeSnippets"] = parsed["codeSnippets"] as JArray ?? new JArray()
                };
                return new SuccessResponse("Documentation fetched.", data);
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Context7 query-docs failed: {SecretRedactor.Scrub(e.Message)}");
            }
        }
    }
}
