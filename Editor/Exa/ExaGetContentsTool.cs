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
    /// Fetch the full contents (text) of arbitrary URLs via the Exa API.
    /// </summary>
    [McpForUnityTool("exa_get_contents",
        Description = "Fetch the full contents (text) of one or more URLs via Exa. " +
                      "Use after a search to read pages in depth.",
        StructuredOutput = true)]
    public static class ExaGetContentsTool
    {
        public class Parameters
        {
            [ToolParameter("Array of URLs to fetch.", Required = true)]
            public string[] Urls { get; set; }

            [ToolParameter("Maximum characters of text to return per URL. Default 4000.")]
            public int MaxCharacters { get; set; } = 4000;
        }

        public static async Task<object> HandleCommand(JObject @params)
        {
            if (@params == null) return new ErrorResponse("Parameters cannot be null.");
            var p = new ToolParams(@params);

            var urls = p.GetStringArray("urls");
            if (urls is null || urls.Length == 0)
                return new ErrorResponse("'urls' parameter is required (array of URLs).");

            if (!ExaContext7Keys.TryGetExaKey(out var apiKey))
                return new ErrorResponse(
                    "Exa API key not configured. Set the EXA_API_KEY environment variable " +
                    "or add a key in Window/MCP for EXA & Context7.");

            var urlArray = new JArray();
            foreach (var u in urls)
                if (!string.IsNullOrWhiteSpace(u)) urlArray.Add(u.Trim());
            if (urlArray.Count == 0)
                return new ErrorResponse("'urls' contains no valid URLs.");

            var body = new JObject
            {
                ["urls"] = urlArray,
                ["text"] = true,
                ["maxCharacters"] = Math.Clamp(p.GetInt("max_characters", 4000) ?? 4000, 1, 100000)
            };

            try
            {
                var result = await ExaContext7Http.PostJsonAsync(
                    "https://api.exa.ai/contents", "x-api-key", apiKey, body.ToString(Formatting.None));

                if (!result.IsSuccess)
                    return new ErrorResponse(
                        $"Exa API error (HTTP {result.StatusCode}): {SecretRedactor.Scrub(result.Error ?? result.Text ?? "unknown")}");

                var parsed = JToken.Parse(result.Text ?? "{}");
                var results = parsed["results"] as JArray ?? new JArray();
                var shaped = new JArray();
                foreach (var r in results)
                {
                    var item = new JObject
                    {
                        ["id"] = r["id"],
                        ["title"] = r["title"],
                        ["url"] = r["url"],
                        ["text"] = r["text"]
                    };
                    shaped.Add(item);
                }
                return new SuccessResponse("Contents fetched.", shaped);
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Exa get-contents failed: {SecretRedactor.Scrub(e.Message)}");
            }
        }
    }
}
