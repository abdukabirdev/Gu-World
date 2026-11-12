using System;
using System.Collections.Generic;
using GuWorld.Core;
using GuWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The altar of the immortal aperture: the hero's step, Gu of the book on loan (by rank, at any
    /// quality), beasts called onto the terrace, and switches for endless essence and no death.
    /// </summary>
    public partial class Ui
    {
        int sandboxRank = 1;
        string sandboxGu;
        int sandboxQuality = 1;
        string sandboxEnemy = "wolf";
        int sandboxCount = 1;

        void SandboxPage()
        {
            var session = S;
            var state = session.State;
            var body = Frame("仙", "Апертура бессмертного", $"{Cultivation.Name(state.Step)} · здесь ничего не считается", false, null, null,
                new[] { ("Tab", "Ранг Гу"), ("Esc", "Закрыть") });
            body.gameObject.AddComponent<PageKeys>().On(Key.Tab, () => { sandboxRank = sandboxRank % Cultivation.MaxRank + 1; sandboxGu = null; Redraw(); });

            // ---------------------------------------------------------------- The hero

            var left = Column(body, 0, 0.27f, 6);
            UiKit.Lbl("Ступень", left, 11, UiKit.LabelGold).Size(18);
            var steps = UiKit.Rect("Steps", left);
            steps.Stack<HorizontalLayoutGroup>(8, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
            steps.Size(44);
            UiKit.HexButton("−", steps, () => { session.SandboxStep(state.Step - 1); Redraw(); }, UiKit.HexKind.Ghost, 16, 40).Size(40, 54, 0);
            UiKit.HexButton("+", steps, () => { session.SandboxStep(state.Step + 1); Redraw(); }, UiKit.HexKind.Ghost, 16, 40).Size(40, 54, 0);

            for (var rank = 1; rank <= Cultivation.MaxRank; rank++)
            {
                var r = rank;
                UiKit.HexButton(GuBook.RankNumerals[rank], steps, () => { session.SandboxStep((r - 1) * 4); Redraw(); }, session.Rank == rank ? UiKit.HexKind.Solid : UiKit.HexKind.Ghost, 13, 40).Size(40, 44, 0);
            }

            Fact(left, "Слоты", $"{session.Stats.ActiveSlots} активных · {session.Stats.PassiveSlots} пассивных");
            Fact(left, "Эссенция", $"{Mathf.RoundToInt(session.Stats.MaxEssence)}", UiKit.Essence);
            Fact(left, "Здоровье", $"{Mathf.RoundToInt(session.Stats.MaxHealth)}", UiKit.Danger);
            Gap(left, 8);
            UiKit.HexButton(session.SandboxEssence ? "Эссенция без конца: да" : "Эссенция без конца: нет", left, () => { session.SandboxEssence = !session.SandboxEssence; Redraw(); }, session.SandboxEssence ? UiKit.HexKind.Main : UiKit.HexKind.Ghost, 12, 40);
            UiKit.HexButton(session.SandboxUndying ? "Без смерти: да" : "Без смерти: нет", left, () => { session.SandboxUndying = !session.SandboxUndying; Redraw(); }, session.SandboxUndying ? UiKit.HexKind.Main : UiKit.HexKind.Ghost, 12, 40);
            UiKit.HexButton("Знать все приёмы", left, () => { session.SandboxAllMoves(); Redraw(); }, UiKit.HexKind.Ghost, 12, 40);
            UiKit.HexButton("Сбросить перезарядки", left, () => { session.SandboxReady(); Redraw(); }, UiKit.HexKind.Ghost, 12, 40);
            Gap(left, 8);
            UiKit.HexButton("Покинуть апертуру", left, () => { Close(); Aperture.Leave(); }, UiKit.HexKind.Ghost, 12, 40);

            // ---------------------------------------------------------------- Gu on loan

            var middle = Column(body, 0.3f, 0.66f, 6);
            var tabs = UiKit.Rect("Ranks", middle);
            tabs.Stack<HorizontalLayoutGroup>(14, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
            tabs.Size(36);

            for (var rank = 1; rank <= Cultivation.MaxRank; rank++)
            {
                var r = rank;
                UiKit.Tb($"Ранг {GuBook.RankNumerals[rank]}", tabs, sandboxRank == rank, () => { sandboxRank = r; sandboxGu = null; Redraw(); }, 11);
            }

            var list = Scroll(Rest(middle));
            var shown = new List<GuDef>();

            foreach (var def in GuBook.All)
            {
                if (def.Rank == sandboxRank)
                {
                    shown.Add(def);
                }
            }

            shown.Sort((a, b) => a.Path != b.Path ? a.Path.CompareTo(b.Path) : string.CompareOrdinal(a.Name, b.Name));

            if (sandboxGu == null && shown.Count > 0)
            {
                sandboxGu = shown[0].Id;
            }

            foreach (var def in shown)
            {
                var id = def.Id;
                var owned = state.Gu.Exists(g => g.Def == id);
                ListLine(list, def.Seal, def.Name, GuBook.PathName(def.Path) + (def.Kind == GuKind.Passive ? " · пасс." : ""), owned ? UiKit.Essence : UiKit.Text, id == sandboxGu, () => { sandboxGu = id; Redraw(); });
            }

            var picked = GuBook.Get(sandboxGu);
            var right = Column(body, 0.69f, 1, 6);

            if (picked != null)
            {
                UiKit.Zh(picked.Seal, right, 44, UiKit.Hex(picked.Colour)).Size(50);
                UiKit.Disp(picked.Name, right, 22, UiKit.Text).Size(28);
                Para(picked.Description, right, 13, UiKit.Text2);
                var qualities = UiKit.Rect("Quality", right);
                qualities.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
                qualities.Size(36);

                for (var q = 0; q <= GuBook.MaxQuality; q++)
                {
                    var quality = q;
                    UiKit.HexButton(GuBook.QualityNames[q], qualities, () => { sandboxQuality = quality; Redraw(); }, sandboxQuality == q ? UiKit.HexKind.Solid : UiKit.HexKind.Ghost, 10, 32).Size(32, 92, 0);
                }

                var take = picked.Id;
                var above = picked.Rank > session.Rank;
                UiKit.HexButton(above ? $"Взять (нужен ранг {GuBook.RankNumerals[picked.Rank]} для слота)" : "Взять и вставить", right, () => { S.SandboxGive(take, sandboxQuality); Redraw(); }, UiKit.HexKind.Main, 12, 42);
            }

            // ---------------------------------------------------------------- Beasts to call

            Gap(right, 10);
            UiKit.Lbl($"Противники · на террасе {Aperture.Instance?.Living ?? 0}", right, 11, UiKit.LabelGold).Size(18);
            var enemies = UiKit.Rect("Enemies", right);
            var grid = enemies.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(124, 30);
            grid.spacing = new Vector2(6, 6);
            enemies.Size(30 * 4 + 6 * 3);
            var foes = new List<EnemyDef>();

            foreach (var def in Bestiary.All)
            {
                if (def.Mind != EnemyMind.Still && (def.Boss || def.Level % 2 == 1 || def.Mind == EnemyMind.Caster) && foes.Count < 16)
                {
                    foes.Add(def);
                }
            }

            foreach (var def in foes)
            {
                var id = def.Id;
                UiKit.HexButton(def.Name, enemies, () => { sandboxEnemy = id; Redraw(); }, sandboxEnemy == id ? UiKit.HexKind.Solid : UiKit.HexKind.Ghost, 10, 30);
            }

            var row = UiKit.Rect("Call", right);
            row.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
            row.Size(40);

            foreach (var count in new[] { 1, 3, 6 })
            {
                var c = count;
                UiKit.HexButton($"×{count}", row, () => { sandboxCount = c; Redraw(); }, sandboxCount == count ? UiKit.HexKind.Solid : UiKit.HexKind.Ghost, 11, 36).Size(36, 48, 0);
            }

            UiKit.HexButton("Призвать", row, () => { Close(); Aperture.Instance?.Call(sandboxEnemy, sandboxCount); }, UiKit.HexKind.Main, 12, 36).Size(36, 120, 0);
            UiKit.HexButton("Манекены", row, () => { Aperture.Instance?.CallPosts(); Redraw(); }, UiKit.HexKind.Ghost, 11, 36).Size(36, 100, 0);
            UiKit.HexButton("Отпустить всех", right, () => { Aperture.Instance?.Dismiss(); Redraw(); }, UiKit.HexKind.Ghost, 11, 36);
        }
    }
}
