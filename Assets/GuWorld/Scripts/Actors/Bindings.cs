using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuWorld.Actors
{
    /// <summary>
    /// The hero's keys, by action, rebindable in the settings and kept between runs. Every action has
    /// its default (the keys the help shows); a rebound key that another action held is swapped to it,
    /// so no two actions share a key.
    /// </summary>
    public static class Bindings
    {
        public sealed class Action
        {
            public string Id;
            public string Name;
            public Key Default;
            public Key Key;
        }

        public static readonly Action[] All =
        {
            A("slot1", "Гу в слоте 1", Key.Q), A("slot2", "Гу в слоте 2", Key.E), A("slot3", "Гу в слоте 3", Key.R),
            A("slot4", "Гу в слоте 4", Key.F), A("slot5", "Гу в слоте 5", Key.Z), A("slot6", "Гу в слоте 6", Key.C),
            A("move", "Убийственный приём", Key.V), A("next_move", "Следующий приём", Key.X),
            A("guard", "Блок", Key.LeftCtrl), A("sprint", "Бежать быстрее", Key.LeftShift), A("roll", "Перекат", Key.LeftAlt),
            A("jump", "Прыжок", Key.Space), A("target", "Держать цель", Key.Tab), A("heal", "Пилюля заживления", Key.H),
            A("absorb", "Впитать камень", Key.G), A("use", "Говорить, взять, открыть", Key.T),
        };

        static Action A(string id, string name, Key key) => new Action { Id = id, Name = name, Default = key, Key = key };

        static readonly Dictionary<string, Action> byId = new Dictionary<string, Action>();
        static bool loaded;

        static Action Get(string id)
        {
            Load();

            return byId.TryGetValue(id, out var action) ? action : null;
        }

        public static Key KeyOf(string id) => Get(id)?.Key ?? Key.None;

        public static bool Down(string id)
        {
            var key = KeyOf(id);

            return key != Key.None && Keys.Down(key);
        }

        public static bool Held(string id)
        {
            var key = KeyOf(id);

            return key != Key.None && Keys.Held(key);
        }

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
                case Key.None: return "—";
                default:
                    var name = key.ToString();

                    return name.StartsWith("Digit") ? name.Substring(5) : name;
            }
        }

        public static string LabelOf(string id) => Label(KeyOf(id));

        /// <summary>Binds an action to a key; an action that had the key takes the old one.</summary>
        public static void Bind(string id, Key key)
        {
            var action = Get(id);

            if (action == null || key == Key.None || key == Key.Escape)
            {
                return;
            }

            foreach (var other in All)
            {
                if (other != action && other.Key == key)
                {
                    other.Key = action.Key;
                }
            }

            action.Key = key;
            Save();
        }

        public static void Reset()
        {
            foreach (var action in All)
            {
                action.Key = action.Default;
            }

            Save();
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
                var saved = PlayerPrefs.GetString("gw2.key." + action.Id, "");

                if (saved != "" && Enum.TryParse(saved, out Key key))
                {
                    action.Key = key;
                }
            }
        }

        static void Save()
        {
            foreach (var action in All)
            {
                PlayerPrefs.SetString("gw2.key." + action.Id, action.Key.ToString());
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
    }
}
