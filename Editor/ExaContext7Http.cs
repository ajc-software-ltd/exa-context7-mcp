using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace MCP.ExaContext7.Editor
{
    /// <summary>Result of an outbound HTTP call.</summary>
    public sealed class HttpResult
    {
        public long StatusCode;
        public string Text;
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
        public string Error;
    }

    /// <summary>
    /// Minimal async HTTP helper for the Exa and Context7 REST APIs.
    /// Wraps UnityWebRequest in a Task without ever blocking the editor main loop.
    /// </summary>
    public static class ExaContext7Http
    {
        const int DefaultTimeoutSeconds = 45;

        public static Task<HttpResult> PostJsonAsync(
            string url, string authHeaderName, string authValue, string jsonBody,
            int timeoutSeconds = DefaultTimeoutSeconds)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody ?? "{}"));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(authHeaderName) && !string.IsNullOrEmpty(authValue))
                request.SetRequestHeader(authHeaderName, authValue);
            request.timeout = timeoutSeconds;
            return Await(request);
        }

        public static Task<HttpResult> GetAsync(
            string url, string authHeaderName, string authValue,
            int timeoutSeconds = DefaultTimeoutSeconds)
        {
            var request = UnityWebRequest.Get(url);
            if (!string.IsNullOrEmpty(authHeaderName) && !string.IsNullOrEmpty(authValue))
                request.SetRequestHeader(authHeaderName, authValue);
            request.timeout = timeoutSeconds;
            return Await(request);
        }

        static Task<HttpResult> Await(UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<HttpResult>();
            var op = request.SendWebRequest();
            op.completed += _ =>
            {
                var result = new HttpResult
                {
                    StatusCode = request.responseCode,
                    Text = request.downloadHandler?.text
                };
                if (request.result != UnityWebRequest.Result.Success)
                    result.Error = request.error;
                request.Dispose();
                tcs.TrySetResult(result);
            };
            return tcs.Task;
        }
    }
}
