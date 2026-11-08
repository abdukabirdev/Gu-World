using System.Collections.Generic;
using GuWorld.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The chat (T, or / to start a command), as Minecraft has it: the log of what happened
    /// over a line to type in. A line beginning with / is a command (Commands); as one types,
    /// what fits is listed above the line with how the command is written, Tab puts the chosen
    /// one in, ↑ ↓ go through the list (or, with none, through the lines sent before), Enter
    /// sends, Esc closes. The world goes on meanwhile.
    /// </summary>
    public partial class Menus
    {
        const int ChatLines = 12;
        const int ShownSuggestions = 8;

        InputField chatField;
        Text chatUsage;
        RectTransform suggestionBox;
        readonly List<Text> suggestionRows = new List<Text>();
        List<string> suggestions = new List<string>();
        int suggestionChosen;
        string suggestedFor;
        /// <summary>Lines sent before (newest last), and how far back ↑ has gone (−1: not at all).</summary>
        readonly List<string> sent = new List<string>();
        int sentBack = -1;
        bool focusChat;
        string chatStart = "";

        /// <summary>Opens the chat; `start` is put in the line ("/" for a command).</summary>
        public void OpenChat(string start)
        {
            chatStart = start ?? "";
            Show(Page.Chat);
        }

        void ChatPage()
        {
            var stage = UiKit.Stage(pageRoot);
            var shade = UiKit.Image("Shade", stage, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.75f), UiKit.SoftEdge);
            shade.rectTransform.Place(Vector2.zero, new Vector2(0.55f, 1));
            var panel = UiKit.Rect("Chat", stage);
            panel.BottomLeft(32, 32, 680, 520);

            // The line to type in: a gold prompt, the keys at its right.
            var line = UiKit.Image("Line", panel, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.92f), UiKit.Plain);
            line.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -46);
            UiKit.Frame(line.transform, UiKit.GoldLit.A(0.5f));
            UiKit.Glow(line.transform, UiKit.Gold.A(0.15f), 20);
            UiKit.Label(">", line.transform, 14, UiKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(Vector2.zero, new Vector2(0, 1), 14, 0, -30, 0);
            var field = UiKit.Field(line.transform, chatStart, "Сообщение или /команда", 160, OnChatTyped);
            field.GetComponent<LayoutElement>().ignoreLayout = true;
            field.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            field.transform.Find("Frame").gameObject.SetActive(false);
            ((RectTransform)field.transform).Place(Vector2.zero, Vector2.one, 20, 0, 330, 0);
            field.textComponent.supportRichText = false;
            field.textComponent.fontSize = 14;
            field.caretWidth = 2;
            chatField = field;
            var keys = UiKit.Rect("Keys", line.transform);
            keys.Place(new Vector2(1, 0), Vector2.one, -330, 0, 14, 0);
            var keyRow = keys.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.MiddleRight);
            keyRow.childForceExpandWidth = false;
            UiKit.Hint(keys, "Tab", "дописать", 11);
            UiKit.Hint(keys, "Enter", "отправить", 11);
            UiKit.Hint(keys, "Esc", "закрыть", 11);

            // How the command is written, over the line.
            chatUsage = UiKit.Label("", panel, 12, UiKit.Muted).Shadowed();
            chatUsage.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 2, 50, 0, -70);

            // The log: a column fading in from the top, a gold line at its left.
            var logBox = UiKit.Image("Log", panel, Color.white, UiKit.Plain);
            logBox.rectTransform.Place(Vector2.zero, Vector2.one, 0, 76, 0, 0);
            logBox.Gradient(new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.25f), new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.7f));
            var edge = UiKit.Image("Edge", logBox.transform, UiKit.Gold.A(0.3f), UiKit.Plain);
            edge.rectTransform.Place(Vector2.zero, new Vector2(0, 1), 0, 0, -1, 0);
            var lines = UiKit.Rect("Lines", logBox.transform).Place(Vector2.zero, Vector2.one, 16, 14, 16, 14);
            lines.Stack<VerticalLayoutGroup>(2, null, TextAnchor.LowerLeft);
            var first = Mathf.Max(0, log.Count - ChatLines);

            for (var i = first; i < log.Count; i++)
            {
                var (text, _) = log[i];
                var row = UiKit.Label(ChatLine(text), lines, 14, UiKit.Text2).Shadowed();
                row.Size(22);
            }

            // What fits what is typed: a list over the line, the chosen one lit with a gold diamond.
            suggestionBox = UiKit.Image("Suggestions", panel, new Color(5 / 255f, 7 / 255f, 12 / 255f, 0.94f), UiKit.Plain).rectTransform;
            UiKit.Frame(suggestionBox, new Color(200 / 255f, 230 / 255f, 240 / 255f, 0.12f));
            suggestionBox.anchorMin = suggestionBox.anchorMax = new Vector2(0, 0);
            suggestionBox.pivot = new Vector2(0, 0);
            suggestionBox.anchoredPosition = new Vector2(0, 74);
            suggestionRows.Clear();
            suggestionMarks.Clear();

            for (var i = 0; i < ShownSuggestions; i++)
            {
                var wash = UiKit.Image("Wash", suggestionBox, UiKit.Gold.A(0), UiKit.SoftEdge);
                wash.rectTransform.anchorMin = new Vector2(0, 0);
                wash.rectTransform.anchorMax = new Vector2(1, 0);
                wash.rectTransform.pivot = new Vector2(0, 0);
                wash.rectTransform.offsetMin = new Vector2(0, 6 + i * 30);
                wash.rectTransform.offsetMax = new Vector2(0, 6 + i * 30 + 30);
                var gem = UiKit.Image("Gem", wash.transform, UiKit.Gold, UiKit.Diamond);
                gem.rectTransform.At(new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(8, 8));
                var row = UiKit.Label("", suggestionBox, 13, UiKit.Text2);
                row.rectTransform.anchorMin = row.rectTransform.anchorMax = new Vector2(0, 0);
                row.rectTransform.pivot = new Vector2(0, 0);
                row.rectTransform.sizeDelta = new Vector2(600, 30);
                row.rectTransform.anchoredPosition = new Vector2(24, 6 + i * 30);
                suggestionRows.Add(row);
                suggestionMarks.Add(wash);
            }

            // The world does not wait.
            var note = UiKit.Rect("Note", stage);
            note.TopRight(32, 28, 300, 20);
            var noteRow = note.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.MiddleRight);
            noteRow.childForceExpandWidth = false;
            UiKit.Image("Dot", note, UiKit.Ok, UiKit.Disc).Size(6, 6, 0);
            UiKit.Lbl("Мир не ждёт", note, 11, UiKit.Muted, 0.2f).Size(20);

            suggestedFor = null;
            sentBack = -1;
            focusChat = true;
            RefreshSuggestions();
        }

        readonly List<Image> suggestionMarks = new List<Image>();

        /// <summary>A log line as the chat shows it: a command sent in the moon's blue, a refusal in coral.</summary>
        static string ChatLine(string text) =>
            text.StartsWith("> ") ? $"<color=#A0D2FF>{text}</color>" : text.StartsWith("!") ? $"<color=#F08A74>{text.Substring(1)}</color>" : text;

        void OnChatTyped(string text)
        {
            if (text != suggestedFor)
            {
                RefreshSuggestions();
            }
        }

        void RefreshSuggestions()
        {
            if (chatField == null)
            {
                return;
            }

            var text = chatField.text ?? "";
            suggestedFor = text;
            suggestions.Clear();
            string usage = null;

            if (Commands.Normalize(text).StartsWith("/") && host.Commands != null)
            {
                suggestions = host.Commands.Suggest(text, out usage);
            }

            suggestionChosen = 0;
            chatUsage.text = usage ?? (text.Length == 0 ? "/help — все команды · ↑ ↓ — прежние строки" : "");
            ShowSuggestions();
        }

        void ShowSuggestions()
        {
            var shown = Mathf.Min(ShownSuggestions, suggestions.Count);
            suggestionBox.gameObject.SetActive(shown > 0);
            // The chosen one stays in the window as one goes down a long list.
            var start = Mathf.Clamp(suggestionChosen - shown + 1, 0, Mathf.Max(0, suggestions.Count - shown));

            for (var i = 0; i < ShownSuggestions; i++)
            {
                // The first at the foot, next to the line.
                var row = suggestionRows[i];
                var index = start + i;

                if (i >= shown)
                {
                    row.text = "";
                    suggestionMarks[i].gameObject.SetActive(false);

                    continue;
                }

                var chosen = index == suggestionChosen;
                row.text = suggestions[index];
                row.color = chosen ? UiKit.GoldLit : UiKit.Text2;
                suggestionMarks[i].gameObject.SetActive(chosen);
                suggestionMarks[i].color = UiKit.Gold.A(0.14f);
            }

            suggestionBox.sizeDelta = new Vector2(680, shown * 30 + 12);
        }

        /// <summary>The chat's keys each frame: Tab, ↑ ↓, Enter, Esc.</summary>
        void UpdateChat()
        {
            var keyboard = Keyboard.current;

            if (chatField == null || keyboard == null)
            {
                return;
            }

            // Focused a frame after opening, so the key that opened it is not typed in.
            if (focusChat)
            {
                focusChat = false;
                chatField.ActivateInputField();
                chatField.text = chatStart;
                chatField.caretPosition = chatField.text.Length;
                RefreshSuggestions();

                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                host.CloseChat();

                return;
            }

            if (keyboard.tabKey.wasPressedThisFrame && suggestions.Count > 0)
            {
                chatField.text = Commands.Complete(chatField.text, suggestions[suggestionChosen]);
                chatField.caretPosition = chatField.text.Length;
                RefreshSuggestions();
            }
            else if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                var up = keyboard.upArrowKey.wasPressedThisFrame;

                if (suggestions.Count > 0)
                {
                    suggestionChosen = (suggestionChosen + (up ? 1 : -1) + suggestions.Count) % suggestions.Count;
                    ShowSuggestions();
                }
                else if (sent.Count > 0)
                {
                    sentBack = Mathf.Clamp(sentBack + (up ? 1 : -1), -1, sent.Count - 1);
                    chatField.text = sentBack < 0 ? "" : sent[sent.Count - 1 - sentBack];
                    chatField.caretPosition = chatField.text.Length;
                    suggestedFor = chatField.text;
                }
            }
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                SendChat(chatField.text);
            }

            // The field keeps the focus (a click elsewhere would take it).
            if (!chatField.isFocused)
            {
                chatField.ActivateInputField();
            }
        }

        void SendChat(string text)
        {
            text = (text ?? "").Trim();

            if (text.Length > 0)
            {
                sent.Add(text);

                if (Commands.Normalize(text).StartsWith("/"))
                {
                    Notify("> " + text);
                    var said = host.Commands?.Run(text);

                    if (!string.IsNullOrEmpty(said))
                    {
                        Notify(said);
                    }
                }
                else
                {
                    Notify($"<color=#E8C98A>{host.Look.FullName}:</color> {text}");
                }
            }

            host.CloseChat();
        }
    }
}
