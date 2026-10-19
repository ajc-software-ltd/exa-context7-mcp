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
    /// Resolve a library name to a Context7-compatible library ID.
    /// </summary>
    [McpForUnityTool("context7_resolve_library",
        Description = "Resolve a library or package name to a Context7-compatible library ID " +
                      "(e.g. 'Unity' -> '/unitycom/docs'). Call this first, then pass the ID to " +
                      "context7_query_docs.",
        StructuredOutput = true)]
    public static class Context7ResolveLibraryTool
    {
        public class Parameters
        {
            [ToolParameter("Library or package name to resolve (e.g. 'Unity', 'Cinemachine', 'react').", Required = true)]
            public string LibraryName { get; set; }

            [ToolParameter("Task or question description used to rank relevant libraries.", Required = true)]
            public string Query { get; set; }
        }

        public static async Task<object> HandleCommand(JObject @params)
        {
            if (@params == null) return new ErrorResponse("Parameters cannot be null.");
            var p = new ToolParams(@params);

            var libraryName = p.GetRequired("library_name");
            if (!libraryName.IsSuccess) return new ErrorResponse(libraryName.ErrorMessage);

            var query = p.GetRequired("query");
            if (!query.IsSuccess) return new ErrorResponse(query.ErrorMessage);

            if (!ExaContext7Keys.TryGetContext7Key(out var apiKey))
                return new ErrorResponse(
                    "Context7 API key not configured. Set the CONTEXT7_API_KEY environment variable " +
                    "or add a key in Window/MCP for EXA & Context7.");

            var url = "https://context7.com/api/v2/libs/search" +
                      $"?libraryName={Uri.EscapeDataString(libraryName.Value.Trim())}" +
                      $"&query={Uri.EscapeDataString(query.Value.Trim())}";

            try
            {
                var result = await ExaContext7Http.GetAsync(url, "Authorization", $"Bearer {apiKey}");

                if (!result.IsSuccess)
                    return new ErrorResponse(
                        $"Context7 API error (HTTP {result.StatusCode}): {SecretRedactor.Scrub(result.Error ?? result.Text ?? "unknown")}");

                return new SuccessResponse("Library resolved.", ShapeResults(result.Text));
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Context7 resolve failed: {SecretRedactor.Scrub(e.Message)}");
            }
        }

        internal static JArray ShapeResults(string json)
        {
            var parsed = JToken.Parse(json);
            var searchResults = parsed["searchResults"] as JArray ?? new JArray();
            var shaped = new JArray();
            foreach (var r in searchResults)
            {
                var item = new JObject
                {
                    ["id"] = r["id"],
                    ["title"] = r["title"],
                    ["description"] = r["description"],
                    ["totalSnippets"] = r["totalSnippets"],
                    ["totalTokens"] = r["totalTokens"],
                    ["trustScore"] = r["trustScore"],
                    ["lastUpdated"] = r["lastUpdated"],
                    ["versions"] = r["versions"]
                };
                shaped.Add(item);
            }
            return shaped;
        }
    }
}
