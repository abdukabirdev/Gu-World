using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuWorld.UI
{
    public static partial class UiKit
    {
        /// <summary>The colour of a Gu's rank, I–V (the design system's rank tokens).</summary>
        public static Color RankColour(int rank)
        {
            // Green copper, red steel, white silver, yellow gold, purple crystal.
            switch (rank)
            {
                case 1: return Hex(0x7FE0C8);
                case 2: return Hex(0xF08A74);
                case 3: return Hex(0xDDE2EA);
                case 4: return Hex(0xF0C868);
                default: return Hex(0xC3A0F0);
            }
        }

        public static Color Rgb(int rgb, float alpha = 1) => Hex(rgb, alpha);
    }

    /// <summary>
    /// A menu the keys can work: ↑ and ↓ choose an entry (the pointer chooses too), Enter
    /// opens it. The chosen entry is lit as the design's chosen row.
    /// </summary>
    public class MenuKeys : MonoBehaviour
    {
        readonly List<MenuEntry> entries = new List<MenuEntry>();
        int chosen;

        public void Add(MenuEntry entry)
        {
            var index = entries.Count;
            entries.Add(entry);
            entry.Keyed = true;
            entry.Entered = () => Choose(index);
            Choose(chosen);
        }

        void Choose(int index)
        {
            chosen = Mathf.Clamp(index, 0, entries.Count - 1);

            for (var i = 0; i < entries.Count; i++)
            {
                entries[i].Chosen = i == chosen;
            }
        }

        void Update()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null || entries.Count == 0)
            {
                return;
            }

            if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                Choose((chosen + 1) % entries.Count);
            }
            else if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                Choose((chosen + entries.Count - 1) % entries.Count);
            }
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                entries[chosen].Open();
            }
        }
    }

    /// <summary>Keys a page answers to while it is shown (each once per press).</summary>
    public class PageKeys : MonoBehaviour
    {
        readonly List<(Key key, Action act)> keys = new List<(Key, Action)>();
        int born;

        void Awake() => born = Time.frameCount;

        public void On(Key key, Action act) => keys.Add((key, act));

        void Update()
        {
            var keyboard = Keyboard.current;

            // Not the press that opened the page.
            if (keyboard == null || Time.frameCount == born)
            {
                return;
            }

            foreach (var (key, act) in keys)
            {
                if (keyboard[key].wasPressedThisFrame)
                {
                    act();

                    return;
                }
            }
        }
    }
}
