using System;
using System.Threading.Tasks;
using MCP.ExaContext7.Editor;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Security;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCP.ExaContext7.Editor.Exa
{
    /// <summary>
    /// Neural web search via the Exa API. Returns results with optional full text
    /// and LLM-extracted highlights.
    /// </summary>
    [McpForUnityTool("exa_search",
        Description = "Neural web search via Exa. Returns search results with URLs, titles, " +
                      "and optional full text and highlights. Set type to 'deep' for the highest " +
                      "quality results (slower, more expensive).",
        StructuredOutput = true)]
    public static class ExaSearchTool
    {
        public class Parameters
        {
            [ToolParameter("Search query. Supports natural-language and semantic queries.", Required = true)]
            public string Query { get; set; }

            [ToolParameter("Number of results to return (1-100). Default 10.")]
            public int NumResults { get; set; } = 10;

            [ToolParameter("Search type: 'auto' (default), 'lite' (fast/cheap), or 'deep' (highest quality).")]
            public string Type { get; set; }

            [ToolParameter("Include full page text in results. Default false.")]
            public bool IncludeText { get; set; }

            [ToolParameter("Include LLM-extracted highlights (key snippets) in results. Default false.")]
            public bool IncludeHighlights { get; set; }

            [ToolParameter("Restrict results to these domains (e.g. ['unity.com', 'docs.unity3d.com']).")]
            public string[] IncludeDomains { get; set; }

            [ToolParameter("Exclude results from these domains.")]
            public string[] ExcludeDomains { get; set; }

            [ToolParameter("Only return results published on or after this date (ISO 8601, e.g. '2024-01-01').")]
            public string StartPublishedDate { get; set; }

            [ToolParameter("Only return results published on or before this date (ISO 8601).")]
            public string EndPublishedDate { get; set; }

            [ToolParameter("Category filter: 'news', 'company', 'research', 'github', 'pdf', etc.")]
            public string Category { get; set; }
        }

        public static async Task<object> HandleCommand(JObject @params)
        {
            if (@params == null) return new ErrorResponse("Parameters cannot be null.");
            var p = new ToolParams(@params);

            var query = p.GetRequired("query");
            if (!query.IsSuccess) return new ErrorResponse(query.ErrorMessage);

            if (!ExaContext7Keys.TryGetExaKey(out var apiKey))
                return new ErrorResponse(
                    "Exa API key not configured. Set the EXA_API_KEY environment variable " +
                    "or add a key in Window/MCP for EXA & Context7.");

            var body = new JObject
            {
                ["query"] = query.Value,
                ["numResults"] = Math.Clamp(p.GetInt("num_results", 10) ?? 10, 1, 100)
            };

            var type = p.Get("type");
            if (!string.IsNullOrWhiteSpace(type)) body["type"] = type.Trim();

            var includeDomains = p.GetStringArray("include_domains");
            if (includeDomains is { Length: > 0 }) body["includeDomains"] = new JArray(includeDomains);

            var excludeDomains = p.GetStringArray("exclude_domains");
            if (excludeDomains is { Length: > 0 }) body["excludeDomains"] = new JArray(excludeDomains);

            var start = p.Get("start_published_date");
            if (!string.IsNullOrWhiteSpace(start)) body["startPublishedDate"] = start.Trim();

            var end = p.Get("end_published_date");
            if (!string.IsNullOrWhiteSpace(end)) body["endPublishedDate"] = end.Trim();

            var category = p.Get("category");
            if (!string.IsNullOrWhiteSpace(category)) body["category"] = category.Trim();

            bool wantText = p.GetBool("include_text");
            bool wantHighlights = p.GetBool("include_highlights");
            if (wantText || wantHighlights)
            {
                var contents = new JObject { ["maxCharacters"] = p.GetInt("max_characters", 4000) ?? 4000 };
                if (wantText) contents["text"] = true;
                if (wantHighlights) contents["highlights"] = true;
                body["contents"] = contents;
            }

            try
            {
                var result = await ExaContext7Http.PostJsonAsync(
                    "https://api.exa.ai/search", "x-api-key", apiKey, body.ToString(Formatting.None));

                if (!result.IsSuccess)
                    return new ErrorResponse(
                        $"Exa API error (HTTP {result.StatusCode}): {SecretRedactor.Scrub(result.Error ?? result.Text ?? "unknown")}");

                return new SuccessResponse("Search completed.", ShapeResults(result.Text));
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Exa search failed: {SecretRedactor.Scrub(e.Message)}");
            }
        }

        internal static JArray ShapeResults(string json)
        {
            var parsed = JToken.Parse(json);
            var results = parsed["results"] as JArray ?? new JArray();
            var shaped = new JArray();
            foreach (var r in results)
            {
                var item = new JObject
                {
                    ["id"] = r["id"],
                    ["title"] = r["title"],
                    ["url"] = r["url"],
                    ["publishedDate"] = r["publishedDate"]
                };
                if (r["text"] != null) item["text"] = r["text"];
                if (r["highlights"] != null) item["highlights"] = r["highlights"];
                if (r["highlightScores"] != null) item["highlightScores"] = r["highlightScores"];
                shaped.Add(item);
            }
            return shaped;
        }
    }
}
