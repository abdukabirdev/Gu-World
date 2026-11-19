using System;
using System.Collections.Generic;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuWorld.Actors
{
    /// <summary>
    /// The hero's controls, by action, rebindable in the settings and kept between runs (ROADMAP 2.7.2).
    /// Each action has two inputs, a main one and a second one; an input is a key, a mouse button or the
    /// wheel turned one way (a notch is a press). Every action has its defaults (the ones the help shows);
    /// an input another action held is swapped to it, so no two actions share one.
    /// </summary>
    public static class Bindings
    {
        /// <summary>The inputs that are not keys: numbers past any Key's.</summary>
        public const int MouseLeft = 1001, MouseRight = 1002, MouseMiddle = 1003, MouseBack = 1004, MouseForward = 1005, WheelUp = 1006, WheelDown = 1007;

        public sealed class Action
        {
            public string Id;
            public string Name;
            /// <summary>The defaults and the inputs now: [0] the main one, [1] the second (0: none).</summary>
            public int[] Defaults;
            public int[] Inputs;

            /// <summary>The main input as a key (Key.None if it is a mouse button, the wheel or nothing).</summary>
            public Key Key => Inputs[0] > 0 && Inputs[0] < 1000 ? (Key)Inputs[0] : Key.None;
            public bool Changed => Inputs[0] != Defaults[0] || Inputs[1] != Defaults[1];
        }

        public static readonly Action[] All =
        {
            A("strike", "Удар ладонью", MouseLeft),
            A("slot1", "Гу в слоте 1", (int)Key.Q), A("slot2", "Гу в слоте 2", (int)Key.E), A("slot3", "Гу в слоте 3", (int)Key.R),
            A("slot4", "Гу в слоте 4", (int)Key.F), A("slot5", "Гу в слоте 5", (int)Key.Z), A("slot6", "Гу в слоте 6", (int)Key.C),
            A("move", "Убийственный приём", (int)Key.V), A("next_move", "Следующий приём", (int)Key.X),
            A("guard", "Блок", (int)Key.LeftCtrl), A("sprint", "Бежать быстрее", (int)Key.LeftShift), A("roll", "Перекат", (int)Key.LeftAlt, MouseRight),
            A("jump", "Прыжок", (int)Key.Space), A("target", "Держать цель", (int)Key.Tab, MouseMiddle), A("heal", "Пилюля заживления", (int)Key.H),
            A("absorb", "Впитать камень", (int)Key.G), A("use", "Говорить, взять, открыть", (int)Key.T),
            A("meditate", "Медитация: сесть · встать", (int)Key.Y),
            // C and Z hold the Gu of slots 6 and 5: crouching and crawling are on keys of their own (rebound in the settings).
            A("crouch", "Присесть · встать", (int)Key.CapsLock), A("crawl", "Ползти · встать", (int)Key.N),
            A("zoom_in", "Камера ближе", WheelUp), A("zoom_out", "Камера дальше", WheelDown),
        };

        static Action A(string id, string name, int input, int second = 0) => new Action { Id = id, Name = name, Defaults = new[] { input, second }, Inputs = new[] { input, second } };

        static readonly Dictionary<string, Action> byId = new Dictionary<string, Action>();
        static bool loaded;

        static Action Get(string id)
        {
            Load();

            return byId.TryGetValue(id, out var action) ? action : null;
        }

        /// <summary>An action's main key (Key.None if its main input is the mouse).</summary>
        public static Key KeyOf(string id) => Get(id)?.Key ?? Key.None;

        public static int InputOf(string id, int slot = 0) => Get(id)?.Inputs[Mathf.Clamp(slot, 0, 1)] ?? 0;

        // ------------------------------------------------------------------ Reading the inputs

        static int wheelFrame = -1;
        static float wheel;

        /// <summary>The wheel's turn this frame (read once a frame, so every action sees the same notch).</summary>
        static float Wheel
        {
            get
            {
                if (wheelFrame != Time.frameCount)
                {
                    wheelFrame = Time.frameCount;
                    wheel = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0;
                }

                return wheel;
            }
        }

        static UnityEngine.InputSystem.Controls.ButtonControl Button(int input)
        {
            var mouse = Mouse.current;

            if (mouse == null)
            {
                return null;
            }

            switch (input)
            {
                case MouseLeft: return mouse.leftButton;
                case MouseRight: return mouse.rightButton;
                case MouseMiddle: return mouse.middleButton;
                case MouseBack: return mouse.backButton;
                case MouseForward: return mouse.forwardButton;
                default: return null;
            }
        }

        /// <summary>Whether an input went down this frame (a wheel notch counts).</summary>
        public static bool InputDown(int input)
        {
            if (input <= 0)
            {
                return false;
            }

            if (input < 1000)
            {
                return Keys.Down((Key)input);
            }

            if (input == WheelUp)
            {
                return Wheel > 0.01f;
            }

            if (input == WheelDown)
            {
                return Wheel < -0.01f;
            }

            var button = Button(input);

            return button != null && button.wasPressedThisFrame;
        }

        /// <summary>Whether an input is held now (the wheel only in the frame it turns).</summary>
        public static bool InputHeld(int input)
        {
            if (input <= 0)
            {
                return false;
            }

            if (input < 1000)
            {
                return Keys.Held((Key)input);
            }

            if (input == WheelUp || input == WheelDown)
            {
                return InputDown(input);
            }

            var button = Button(input);

            return button != null && button.isPressed;
        }

        public static bool Down(string id)
        {
            var action = Get(id);

            return action != null && (InputDown(action.Inputs[0]) || InputDown(action.Inputs[1]));
        }

        public static bool Held(string id)
        {
            var action = Get(id);

            return action != null && (InputHeld(action.Inputs[0]) || InputHeld(action.Inputs[1]));
        }

        // ------------------------------------------------------------------ Names

        /// <summary>The key's name for the interface ("Q", "Ctrl", "Пробел").</summary>
        public static string Label(Key key)
        {
            switch (key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl: return "Ctrl";
                case Key.LeftShift:
                case Key.RightShift: return "Shift";
                case Key.LeftAlt:
                case Key.RightAlt: return "Alt";
                case Key.Space: return "Пробел";
                case Key.Tab: return "Tab";
                case Key.CapsLock: return "Caps";
                case Key.None: return "—";
                default:
                    var name = key.ToString();

                    return name.StartsWith("Digit") ? name.Substring(5) : name;
            }
        }

        /// <summary>An input's name: a key's, or the mouse's ("ЛКМ", "Колесо ↑").</summary>
        public static string Label(int input)
        {
            switch (input)
            {
                case 0: return "—";
                case MouseLeft: return "ЛКМ";
                case MouseRight: return "ПКМ";
                case MouseMiddle: return "СКМ";
                case MouseBack: return "Мышь 4";
                case MouseForward: return "Мышь 5";
                case WheelUp: return "Колесо ↑";
                case WheelDown: return "Колесо ↓";
                default: return input > 0 && input < 1000 ? Label((Key)input) : "—";
            }
        }

        /// <summary>An action's main input by name (what the HUD and the hints show).</summary>
        public static string LabelOf(string id) => Label(InputOf(id));

        /// <summary>Both of an action's inputs: "Alt · ПКМ" (or the one).</summary>
        public static string LabelsOf(string id)
        {
            var action = Get(id);

            if (action == null)
            {
                return "—";
            }

            return action.Inputs[1] > 0 ? $"{Label(action.Inputs[0])} · {Label(action.Inputs[1])}" : Label(action.Inputs[0]);
        }

        /// <summary>A text with keys named by action ({key:guard}, {key:slot1}…) told as they are bound now.</summary>
        public static string Fill(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("{key:", StringComparison.Ordinal) < 0)
            {
                return text;
            }

            foreach (var action in All)
            {
                text = text.Replace("{key:" + action.Id + "}", Label(action.Inputs[0]));
            }

            return text;
        }

        /// <summary>
        /// Keys the game uses that are not rebound here (walking, the pages, the function keys): an action bound
        /// to one of them clashes — both happen at once. The settings mark such a key (ROADMAP 2.7).
        /// </summary>
        public static readonly Dictionary<Key, string> Fixed = new Dictionary<Key, string>
        {
            [Key.W] = "идти", [Key.A] = "идти", [Key.S] = "идти", [Key.D] = "идти",
            [Key.K] = "апертура", [Key.U] = "приёмы", [Key.P] = "пути", [Key.I] = "сумка", [Key.J] = "журнал",
            [Key.M] = "карта", [Key.B] = "кодекс", [Key.L] = "кланы", [Key.O] = "статус",
            [Key.F3] = "отладка", [Key.F5] = "вид", [Key.F7] = "хитбоксы", [Key.F10] = "панель",
            [Key.Slash] = "консоль", [Key.Backquote] = "консоль", [Key.Escape] = "меню",
        };

        /// <summary>What else a key does, if it is one of the fixed ones ("" if it clashes with nothing).</summary>
        public static string ClashOf(Key key) => Fixed.TryGetValue(key, out var what) ? what : "";

        public static string ClashOf(int input) => input > 0 && input < 1000 ? ClashOf((Key)input) : "";

        // ------------------------------------------------------------------ Changing them

        /// <summary>Every action's two places, for Core.Rebind (the same arrays the actions hold).</summary>
        static int[][] Places()
        {
            Load();
            var places = new int[All.Length][];

            for (var i = 0; i < All.Length; i++)
            {
                places[i] = All[i].Inputs;
            }

            return places;
        }

        /// <summary>
        /// Puts an input in one of an action's two places. Whoever had it takes what was there before (another
        /// action, in the same place it had it; or the action's own other place, the two swapped) — Core.Rebind.
        /// </summary>
        public static void Bind(string id, int slot, int input)
        {
            var at = Array.FindIndex(All, a => a.Id == id);

            if (at < 0 || input <= 0 || input == (int)Key.Escape)
            {
                return;
            }

            Rebind.Bind(Places(), at, slot, input);
            Save();
        }

        /// <summary>The old one-input form (a key in the main place).</summary>
        public static void Bind(string id, Key key) => Bind(id, 0, (int)key);

        /// <summary>Empties an action's place (a main one takes the second input then).</summary>
        public static void Clear(string id, int slot)
        {
            var at = Array.FindIndex(All, a => a.Id == id);

            if (at < 0)
            {
                return;
            }

            Rebind.Clear(Places(), at, slot);
            Save();
        }

        public static void Reset()
        {
            Load();

            foreach (var action in All)
            {
                action.Inputs[0] = action.Defaults[0];
                action.Inputs[1] = action.Defaults[1];
            }

            Save();
        }

        // ------------------------------------------------------------------ Kept between runs

        /// <summary>An input as kept: a key's name, or "Mouse:1001"; "" for none.</summary>
        static string Word(int input) => input <= 0 ? "" : input < 1000 ? ((Key)input).ToString() : "Mouse:" + input;

        /// <summary>An input from its word (unknown: `fallback`).</summary>
        static int Parse(string word, int fallback)
        {
            if (word == null)
            {
                return fallback;
            }

            if (word == "-")
            {
                return 0;
            }

            if (word.StartsWith("Mouse:", StringComparison.Ordinal) && int.TryParse(word.Substring(6), out var mouse) && mouse >= MouseLeft && mouse <= WheelDown)
            {
                return mouse;
            }

            return word != "" && Enum.TryParse(word, out Key key) && key != Key.None ? (int)key : fallback;
        }

        static void Load()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;

            foreach (var action in All)
            {
                byId[action.Id] = action;
                // The main input (a key's name before the mouse could be bound), then the second ("-": none on purpose).
                action.Inputs[0] = Parse(PlayerPrefs.GetString("gw2.key." + action.Id, ""), action.Defaults[0]);
                action.Inputs[1] = Parse(PlayerPrefs.GetString("gw2.key2." + action.Id, ""), action.Defaults[1]);
            }

            // Two actions sharing an input (an older run's keys against new defaults): the later one gives its place up.
            var places = new int[All.Length][];

            for (var i = 0; i < All.Length; i++)
            {
                places[i] = All[i].Inputs;
            }

            Rebind.Untangle(places);
        }

        static void Save()
        {
            foreach (var action in All)
            {
                PlayerPrefs.SetString("gw2.key." + action.Id, Word(action.Inputs[0]));
                PlayerPrefs.SetString("gw2.key2." + action.Id, action.Inputs[1] == 0 ? "-" : Word(action.Inputs[1]));
            }

            PlayerPrefs.Save();
        }

        /// <summary>The key pressed this frame (for rebinding), or None.</summary>
        public static Key Pressed()
        {
            if (Keyboard.current == null)
            {
                return Key.None;
            }

            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (key == Key.None)
                {
                    continue;
                }

                try
                {
                    if (Keyboard.current[key].wasPressedThisFrame)
                    {
                        return key;
                    }
                }
                catch (ArgumentException)
                {
                    // Keys the keyboard has no control for (IME and the like).
                }
            }

            return Key.None;
        }

        /// <summary>The input pressed this frame — a key, a mouse button or a wheel notch — for rebinding; 0 if none.</summary>
        public static int PressedInput()
        {
            var key = Pressed();

            if (key != Key.None)
            {
                return (int)key;
            }

            for (var input = MouseLeft; input <= WheelDown; input++)
            {
                if (InputDown(input))
                {
                    return input;
                }
            }

            return 0;
        }
    }
}
