using System;
using MCPForUnity.Editor.Security;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MCP.ExaContext7.Editor.Settings
{
    /// <summary>
    /// Editor window for Exa & Context7 MCP tool configuration, opened from
    /// Window/MCP for EXA & Context7. API keys use the same interaction pattern
    /// as MCP for Unity's AssetGen key management: masked password field +
    /// Save / Clear buttons writing to the OS secure store, status label showing
    /// presence only (the value is never read back into this window).
    /// </summary>
    public class ExaContext7Window : EditorWindow
    {
        [MenuItem("Window/MCP for EXA & Context7", priority = 2100)]
        public static void ShowWindow()
        {
            var window = GetWindow<ExaContext7Window>("MCP for EXA & Context7");
            window.minSize = new Vector2(420, 260);
            window.Show();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            root.Add(scroll);

            var content = new VisualElement();
            content.style.paddingLeft = 12;
            content.style.paddingRight = 12;
            content.style.paddingTop = 12;
            content.style.paddingBottom = 12;
            scroll.Add(content);

            var title = new Label("Exa & Context7 MCP Tools");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14;
            title.style.marginBottom = 4;
            content.Add(title);

            var subtitle = new Label(
                "Neural web search (Exa) and up-to-date library documentation (Context7) " +
                "for your AI assistant via MCP for Unity.");
            subtitle.style.whiteSpace = WhiteSpace.Normal;
            subtitle.style.opacity = 0.7f;
            subtitle.style.marginBottom = 16;
            content.Add(subtitle);

            var keyHelp = new Label(
                "API keys are stored in your OS secure store (Keychain / Credential Manager / " +
                "libsecret) and are never read back into this window. Alternatively set the " +
                "EXA_API_KEY and CONTEXT7_API_KEY environment variables before launching Unity.");
            keyHelp.style.whiteSpace = WhiteSpace.Normal;
            keyHelp.style.opacity = 0.6f;
            keyHelp.style.marginBottom = 16;
            content.Add(keyHelp);

            content.Add(CreateProviderRow(
                "Exa",
                ExaContext7Keys.ExaProviderId,
                "Get your key at https://dashboard.exa.ai"));

            content.Add(CreateProviderRow(
                "Context7",
                ExaContext7Keys.Context7ProviderId,
                "Get your key at https://context7.com/dashboard"));

            var toolsHeader = new Label("Registered Tools");
            toolsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            toolsHeader.style.fontSize = 12;
            toolsHeader.style.marginTop = 8;
            toolsHeader.style.marginBottom = 4;
            content.Add(toolsHeader);

            var toolsList = new Label(
                "exa_search · exa_find_similar · exa_get_contents\n" +
                "context7_resolve_library · context7_query_docs · context7_search");
            toolsList.style.whiteSpace = WhiteSpace.Normal;
            toolsList.style.opacity = 0.7f;
            toolsList.style.marginBottom = 8;
            content.Add(toolsList);

            var reconnectNote = new Label(
                "After setting keys, reconnect your AI client to the MCP server " +
                "(or restart Unity) so the tools are registered.");
            reconnectNote.style.whiteSpace = WhiteSpace.Normal;
            reconnectNote.style.opacity = 0.6f;
            content.Add(reconnectNote);
        }

        VisualElement CreateProviderRow(string displayName, string providerId, string keyHint)
        {
            var row = new VisualElement();
            row.style.marginBottom = 12;
            row.style.paddingBottom = 8;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

            var statusLabel = new Label();

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 2;

            var nameLabel = new Label(displayName);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.flexShrink = 0;
            header.Add(nameLabel);

            statusLabel.style.flexGrow = 1;
            statusLabel.style.marginLeft = 8;
            header.Add(statusLabel);
            row.Add(header);

            var hintLabel = new Label(keyHint);
            hintLabel.style.opacity = 0.6f;
            hintLabel.style.fontSize = 10;
            hintLabel.style.marginBottom = 4;
            row.Add(hintLabel);

            var fieldRow = new VisualElement();
            fieldRow.style.flexDirection = FlexDirection.Row;
            fieldRow.style.alignItems = Align.Center;

            var keyField = new TextField();
            keyField.isPasswordField = true;
            keyField.maskChar = '*';
            keyField.tooltip =
                $"Paste your {displayName} API key, then press Save (or click away). " +
                "The key is stored in your OS secure store and is never read back into this field.";
            keyField.style.flexGrow = 1;
            keyField.style.flexShrink = 1;
            keyField.style.marginRight = 4;
            fieldRow.Add(keyField);

            var saveButton = new Button { text = "Save" };
            fieldRow.Add(saveButton);

            var clearButton = new Button { text = "Clear" };
            fieldRow.Add(clearButton);

            var testButton = new Button { text = "Test" };
            fieldRow.Add(testButton);

            row.Add(fieldRow);

            void SetStatus(string text, bool ok)
            {
                statusLabel.text = text;
                statusLabel.style.color = ok ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.4f, 0.3f);
            }

            void SaveKeyFromField()
            {
                string text = keyField.text?.Trim();
                if (string.IsNullOrEmpty(text)) return;

                try
                {
                    SecureKeyStore.Current.Set(providerId, text);
                    keyField.SetValueWithoutNotify(string.Empty);
                    SetStatus("saved ✓", true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ExaContext7] Failed to store {providerId} key: {ex.Message}");
                    SetStatus("save failed", false);
                }
            }

            keyField.RegisterCallback<FocusOutEvent>(_ => SaveKeyFromField());
            saveButton.clicked += SaveKeyFromField;

            clearButton.clicked += () =>
            {
                try { SecureKeyStore.Current.Delete(providerId); }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ExaContext7] Failed to delete {providerId} key: {ex.Message}");
                }
                keyField.SetValueWithoutNotify(string.Empty);
                SetStatus("not set", false);
            };

            testButton.clicked += () =>
            {
                bool present = HasKey(providerId);
                SetStatus(present ? "key present ✓" : "no key set", present);
            };

            bool present = HasKey(providerId);
            SetStatus(present ? "saved ✓" : "not set", present);

            return row;
        }

        static bool HasKey(string providerId)
        {
            try { return SecureKeyStore.Current.Has(providerId); }
            catch { return false; }
        }
    }
}
