using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// A conversation with someone: nodes of lines, each ending in answers the hero can give.
    /// Answers may need something (a condition) and may do something (effects). Conditions and
    /// effects are short strings, so the content reads like a script:
    /// <list type="bullet">
    /// <item>conditions: "flag:x", "!flag:x", "rep:bai>=20", "rep:xiong&lt;0", "item:jade_shard>=3", "stones>=50",
    /// "step>=4", "rank>=2", "quest:id" (taken, not done), "done:id", "!quest:id", "chapter>=3", "night", "day",
    /// "gu:id" (owns one), "skill>=2" (refining), "visited:lantern_id"</item>
    /// <item>effects: "flag:x", "give:item[:n]", "take:item[:n]", "stones:+n" / "stones:-n", "rep:clan:+n",
    /// "quest:id" (start it), "shop", "gamble", "board", "learn:pill:id", "learn:fusion:id", "insight:n",
    /// "gu:id" (a refined Gu), "codex:id", "heal", "fight:encounter_id", "sleep:hours"</item>
    /// </list>
    /// </summary>
    public class DialogueDef
    {
        public string Id;
        /// <summary>The first node (its id: "start").</summary>
        public DialogueNode[] Nodes = new DialogueNode[0];

        Dictionary<string, DialogueNode> byId;

        public DialogueNode Node(string id)
        {
            if (byId == null)
            {
                byId = new Dictionary<string, DialogueNode>();

                foreach (var node in Nodes)
                {
                    byId[node.Id] = node;
                }
            }

            return id != null && byId.TryGetValue(id, out var found) ? found : null;
        }
    }

    public class DialogueNode
    {
        public string Id;
        public Line[] Lines = new Line[0];
        /// <summary>What happens when the node is reached.</summary>
        public string[] Effects = new string[0];
        public DialogueOption[] Options = new DialogueOption[0];
    }

    public class DialogueOption
    {
        public string Text;
        /// <summary>The node it leads to; "" ends the conversation.</summary>
        public string Next = "";
        /// <summary>All must hold for the answer to be offered.</summary>
        public string[] Needs = new string[0];
        public string[] Effects = new string[0];
        /// <summary>Offered only once in the whole game (remembered as a flag).</summary>
        public bool Once;
        /// <summary>Shown greyed with its reason when a need fails, instead of hidden.</summary>
        public bool ShowLocked;
    }

    /// <summary>Short builders for writing conversations.</summary>
    public static class Converse
    {
        public static DialogueDef D(string id, params DialogueNode[] nodes) => new DialogueDef { Id = id, Nodes = nodes };

        public static DialogueNode N(string id, Line[] lines, params DialogueOption[] options) => new DialogueNode { Id = id, Lines = lines, Options = options };

        public static DialogueNode N(string id, Line[] lines, string[] effects, params DialogueOption[] options) => new DialogueNode { Id = id, Lines = lines, Effects = effects, Options = options };

        public static Line[] Say(params (string who, string text)[] lines)
        {
            var all = new Line[lines.Length];

            for (var i = 0; i < lines.Length; i++)
            {
                all[i] = new Line(lines[i].who, lines[i].text);
            }

            return all;
        }

        /// <summary>An answer: its text, the node it leads to, then conditions (prefixed "?") and effects mixed.</summary>
        public static DialogueOption O(string text, string next = "", params string[] rules)
        {
            var needs = new List<string>();
            var effects = new List<string>();
            var once = false;
            var locked = false;

            foreach (var rule in rules)
            {
                if (rule == "once")
                {
                    once = true;
                }
                else if (rule == "locked")
                {
                    locked = true;
                }
                else if (rule.StartsWith("?"))
                {
                    needs.Add(rule.Substring(1));
                }
                else
                {
                    effects.Add(rule);
                }
            }

            return new DialogueOption { Text = text, Next = next, Needs = needs.ToArray(), Effects = effects.ToArray(), Once = once, ShowLocked = locked };
        }

        /// <summary>The usual way out.</summary>
        public static DialogueOption Bye(string text = "Мне пора.") => new DialogueOption { Text = text };
    }
}
