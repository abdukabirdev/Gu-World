using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>An answer as the interface shows it: its words, whether it can be chosen, and why not.</summary>
    public class Answer
    {
        public DialogueOption Option;
        public string Text;
        public bool Enabled = true;
        public string Why = "";
    }

    /// <summary>
    /// A conversation under way: the node the hero is at, its lines, and the answers open to them.
    /// The interface reads Lines and Answers, then calls Choose; when Over, it closes.
    /// </summary>
    public class DialogueRun
    {
        public readonly DialogueDef Def;
        public readonly NpcDef Npc;
        readonly Session session;

        public DialogueNode Node { get; private set; }
        public bool Over { get; private set; }
        /// <summary>The nodes reached, in order (for the log and the tests).</summary>
        public readonly List<string> Path = new List<string>();

        public DialogueRun(Session session, DialogueDef def, NpcDef npc, string start = "start")
        {
            this.session = session;
            Def = def;
            Npc = npc;
            Go(start);
        }

        public Line[] Lines => Node != null ? Node.Lines : new Line[0];

        public List<Answer> Answers
        {
            get
            {
                var answers = new List<Answer>();

                if (Node == null)
                {
                    return answers;
                }

                foreach (var option in Node.Options)
                {
                    if (option.Once && session.Has(OnceFlag(option)))
                    {
                        continue;
                    }

                    var ok = session.MeetsAll(option.Needs);

                    if (ok)
                    {
                        answers.Add(new Answer { Option = option, Text = option.Text });
                    }
                    else if (option.ShowLocked)
                    {
                        var why = "";

                        foreach (var need in option.Needs)
                        {
                            why = session.Explain(need);

                            if (why != "")
                            {
                                break;
                            }
                        }

                        answers.Add(new Answer { Option = option, Text = option.Text, Enabled = false, Why = why });
                    }
                }

                // A conversation never leaves the hero without a way out.
                if (answers.TrueForAll(a => !a.Enabled))
                {
                    answers.Add(new Answer { Option = new DialogueOption { Text = "…" }, Text = "…" });
                }

                return answers;
            }
        }

        /// <summary>The flag that remembers a once-only answer (a hash of its words that is the same in every run, unlike string.GetHashCode).</summary>
        string OnceFlag(DialogueOption option)
        {
            var hash = 2166136261u;

            foreach (var c in option.Text)
            {
                hash = (hash ^ c) * 16777619u;
            }

            return $"said:{Def.Id}:{Node.Id}:{hash & 0xFFFFFF:x6}";
        }

        /// <summary>Picks an answer (by its place in Answers). False if it cannot be chosen.</summary>
        public bool Choose(int index)
        {
            var answers = Answers;

            if (Over || index < 0 || index >= answers.Count || !answers[index].Enabled)
            {
                return false;
            }

            var option = answers[index].Option;

            if (option.Once)
            {
                session.SetFlag(OnceFlag(option));
            }

            session.ApplyAll(option.Effects, Npc?.Id ?? "");
            Go(option.Next);

            return true;
        }

        void Go(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Over = true;
                Node = null;

                return;
            }

            Node = Def.Node(id);

            if (Node == null)
            {
                Over = true;

                return;
            }

            Path.Add(id);
            session.ApplyAll(Node.Effects, Npc?.Id ?? "");
        }
    }

    public partial class Session
    {
        /// <summary>Begins someone's conversation (null if they have none).</summary>
        public DialogueRun Converse(string npcId)
        {
            var npc = Atlas.Npc(npcId);

            if (npc == null || npc.Dialogue == "")
            {
                return null;
            }

            var def = Dialogues.Get(npc.Dialogue);

            if (def == null)
            {
                return null;
            }

            OpenCodex("person:" + npc.Id);

            return new DialogueRun(this, def, npc);
        }
    }
}
