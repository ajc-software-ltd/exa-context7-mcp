using System;
using MCPForUnity.Editor.Security;

namespace MCP.ExaContext7.Editor
{
    /// <summary>
    /// Resolves Exa and Context7 API keys.
    ///
    /// Resolution order per provider:
    ///   1. The standard environment variable (EXA_API_KEY / CONTEXT7_API_KEY).
    ///   2. MCP for Unity's OS secure store via <see cref="SecureKeyStore"/>, which itself
    ///      honours the MCPFORUNITY_EXA_API_KEY / MCPFORUNITY_CONTEXT7_API_KEY env overrides
    ///      before falling back to Keychain / Credential Manager / libsecret.
    ///
    /// Key values are never logged and never returned across the MCP bridge.
    /// </summary>
    public static class ExaContext7Keys
    {
        public const string ExaProviderId = "exa";
        public const string Context7ProviderId = "context7";

        public static bool TryGetExaKey(out string apiKey)
        {
            apiKey = Environment.GetEnvironmentVariable("EXA_API_KEY");
            if (!string.IsNullOrWhiteSpace(apiKey)) return true;

            try { return SecureKeyStore.Current.TryGet(ExaProviderId, out apiKey); }
            catch { apiKey = null; return false; }
        }

        public static bool TryGetContext7Key(out string apiKey)
        {
            apiKey = Environment.GetEnvironmentVariable("CONTEXT7_API_KEY");
            if (!string.IsNullOrWhiteSpace(apiKey)) return true;

            try { return SecureKeyStore.Current.TryGet(Context7ProviderId, out apiKey); }
            catch { apiKey = null; return false; }
        }
    }
}
