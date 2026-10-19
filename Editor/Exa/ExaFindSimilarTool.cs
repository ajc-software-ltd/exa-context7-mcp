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
    /// Find pages semantically similar to a given URL via the Exa API.
    /// </summary>
    [McpForUnityTool("exa_find_similar",
        Description = "Find web pages semantically similar to a given URL via Exa. " +
                      "Useful for discovering related articles, competing tools, or follow-up sources.",
        StructuredOutput = true)]
    public static class ExaFindSimilarTool
    {
        public class Parameters
        {
            [ToolParameter("The source URL to find similar pages for.", Required = true)]
            public string Url { get; set; }

            [ToolParameter("Number of similar results to return (1-100). Default 10.")]
            public int NumResults { get; set; } = 10;

            [ToolParameter("Include full page text in results. Default false.")]
            public bool IncludeText { get; set; }
        }

        public static async Task<object> HandleCommand(JObject @params)
        {
            if (@params == null) return new ErrorResponse("Parameters cannot be null.");
            var p = new ToolParams(@params);

            var url = p.GetRequired("url");
            if (!url.IsSuccess) return new ErrorResponse(url.ErrorMessage);

            if (!ExaContext7Keys.TryGetExaKey(out var apiKey))
                return new ErrorResponse(
                    "Exa API key not configured. Set the EXA_API_KEY environment variable " +
                    "or add a key in Window/MCP for EXA & Context7.");

            var body = new JObject
            {
                ["url"] = url.Value.Trim(),
                ["numResults"] = Math.Clamp(p.GetInt("num_results", 10) ?? 10, 1, 100)
            };

            if (p.GetBool("include_text"))
                body["contents"] = new JObject { ["text"] = true, ["maxCharacters"] = 4000 };

            try
            {
                var result = await ExaContext7Http.PostJsonAsync(
                    "https://api.exa.ai/findSimilar", "x-api-key", apiKey, body.ToString(Formatting.None));

                if (!result.IsSuccess)
                    return new ErrorResponse(
                        $"Exa API error (HTTP {result.StatusCode}): {SecretRedactor.Scrub(result.Error ?? result.Text ?? "unknown")}");

                return new SuccessResponse("Similar pages found.", ExaSearchTool.ShapeResults(result.Text));
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Exa find-similar failed: {SecretRedactor.Scrub(e.Message)}");
            }
        }
    }
}
