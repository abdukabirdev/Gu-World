using System;
using GuWorld.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The status window (O, UI-Status.dc.html), the way a "system" shows one: a cold blue
    /// window with bright corners and a light scanning down it; at its left the hero's name,
    /// the body's level big and its title, health, stamina and the sea as bars, how the clans
    /// stand; at its right each trait of the body in a row — its name, its level, how far
    /// toward the next, what it gives now and what trains it.
    /// </summary>
    public partial class Menus
    {
        /// <summary>The system's own colours: a cold blue over the ink.</summary>
        static readonly Color SystemBlue = UiKit.Hex(0x6FB8FF);
        static readonly Color SystemPale = UiKit.Hex(0x8FC4FF);
        static readonly Color SystemText = UiKit.Hex(0xCFE6FF);

        void StatusPage()
        {
            var body = BookFrame("star", "Статус", Page.Status);
            var p = host.Physique;
            var gu = host.Cultivator;
            var a = gu.Aperture;

            // The window.
            var window = UiKit.Image("Window", body, Color.white, UiKit.Plain);
            window.rectTransform.Place(Vector2.zero, Vector2.one, 94, 0, 94, 2);
            window.Gradient(new Color(10 / 255f, 26 / 255f, 44 / 255f, 0.82f), new Color(4 / 255f, 10 / 255f, 20 / 255f, 0.9f));
            UiKit.Glow(window.transform, SystemBlue.A(0.2f), 40);
            UiKit.Frame(window.transform, SystemBlue.A(0.45f));
            var outerEdge = UiKit.Frame(window.transform, SystemBlue.A(0.12f));
            outerEdge.rectTransform.Fill(-4);

            foreach (var corner in new[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) })
            {
                var sx = corner.x < 0.5f ? 1 : -1;
                var sy = corner.y < 0.5f ? 1 : -1;
                var h = UiKit.Image("Corner", window.transform, UiKit.Hex(0x9FD2FF), UiKit.Plain);
                h.rectTransform.anchorMin = h.rectTransform.anchorMax = corner;
                h.rectTransform.pivot = corner;
                h.rectTransform.anchoredPosition = new Vector2(-2 * sx, -2 * sy);
                h.rectTransform.sizeDelta = new Vector2(22, 2);
                var v = UiKit.Image("Corner", window.transform, UiKit.Hex(0x9FD2FF), UiKit.Plain);
                v.rectTransform.anchorMin = v.rectTransform.anchorMax = corner;
                v.rectTransform.pivot = corner;
                v.rectTransform.anchoredPosition = new Vector2(-2 * sx, -2 * sy);
                v.rectTransform.sizeDelta = new Vector2(2, 22);
            }

            // The light scanning down.
            var clip = UiKit.Rect("Clip", window.transform).Fill();
            clip.gameObject.AddComponent<RectMask2D>();
            var scan = UiKit.Image("Scan", clip, Color.white, UiKit.Plain);
            scan.rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -80, 0, 0);
            scan.Gradient(SystemBlue.A(0), SystemBlue.A(0), false, SystemBlue.A(0.06f), 0.5f);
            var run = scan.gameObject.AddComponent<Drift>();
            run.Period = 7;
            run.Way = new Vector2(0, -800);

            // The title band.
            var band = UiKit.Image("Band", window.transform, Color.white, UiKit.FadeBoth);
            band.rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -56, 0, 0);
            band.color = SystemBlue.A(0.12f);
            UiKit.Image("Rule", window.transform, SystemBlue.A(0.25f), UiKit.Plain).rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -57, 0, 56);
            var title = UiKit.Rect("Title", window.transform);
            title.Place(new Vector2(0, 1), Vector2.one, 0, -56, 0, 0);
            var titleRow = title.Stack<HorizontalLayoutGroup>(14, null, TextAnchor.MiddleCenter);
            titleRow.childForceExpandWidth = false;
            SystemGem(title);
            var word = UiKit.Lbl("Статус", title, 13, SystemText, 0.5f);
            word.Shadowed().GetComponent<Shadow>().effectColor = SystemBlue.A(0.6f);
            word.Size(56);
            SystemGem(title);

            // The hero.
            var who = UiKit.Rect("Who", window.transform);
            who.Place(new Vector2(0, 0), new Vector2(0, 1), 44, 30, -344, 84);
            who.Stack<VerticalLayoutGroup>(14);
            SystemLabel(who, "Имя");
            UiKit.Disp(host.Look.FullName, who, 32).Size(34);
            var level = UiKit.Rect("Level", who);
            var levelRow = level.Stack<HorizontalLayoutGroup>(16, null, TextAnchor.LowerLeft);
            levelRow.childForceExpandWidth = false;
            level.Size(100);
            var levelBox = UiKit.Rect("Body", level);
            levelBox.Stack<VerticalLayoutGroup>(0, null, TextAnchor.LowerLeft);
            levelBox.Size(100, 110, 0);
            SystemLabel(levelBox, "Уровень тела");
            var number = UiKit.Disp($"{p.Overall}", levelBox, 84, UiKit.Hex(0xDFF0FF));
            number.Shadowed().GetComponent<Shadow>().effectColor = SystemBlue.A(0.5f);
            number.Size(84);
            var titleBox = UiKit.Rect("Title", level);
            titleBox.Stack<VerticalLayoutGroup>(6, new RectOffset(0, 0, 0, 12), TextAnchor.LowerLeft);
            titleBox.Size(100, 170, 0);
            SystemLabel(titleBox, "Титул");
            UiKit.Label(p.Title, titleBox, 15, SystemText).Size(36);

            SystemBar(who, "Здоровье", $"{Mathf.CeilToInt(host.Health.Current)} / {host.Health.Max:0}", host.Health.Current / host.Health.Max, UiKit.BarKind.Health);
            SystemBar(who, "Выносливость", $"{Mathf.CeilToInt(p.Stamina)} / {p.MaxStamina:0}", p.Stamina / p.MaxStamina, UiKit.BarKind.Stamina);
            SystemBar(who, "Море эссенции", $"{a.Essence:0}% · ранг {GuWorld.Cultivation.Aperture.RankNumeral(a.Rank)}", a.Essence / 100, UiKit.BarKind.Essence);

            var note = UiKit.Image("Clans", who, SystemBlue.A(0.05f), UiKit.Plain);
            UiKit.Frame(note.transform, SystemBlue.A(0.25f));
            var noteColumn = note.rectTransform.Stack<VerticalLayoutGroup>(4, new RectOffset(16, 16, 12, 12));
            noteColumn.childControlHeight = true;
            note.Size(-1).flexibleHeight = 1;

            foreach (var clan in GuWorld.Creatures.Clans.Rivals)
            {
                var h = GuWorld.Creatures.Clans.Hostility(clan);
                var colour = h >= GuWorld.Creatures.Clans.War ? "#F08A74" : h >= GuWorld.Creatures.Clans.OnSight ? "#F0C868" : "#CFE6FF";
                UiKit.Label($"{GuWorld.Creatures.MasterKinds.ClanName(clan)}: <color={colour}>{GuWorld.Creatures.Clans.Standing(clan)} · {h:0}</color>", note.transform, 13, SystemText).Size(18);
            }

            UiKit.Label("Тело растёт от того, чем вы его нагружаете.", note.transform, 12, SystemPale).Size(18);

            // The traits.
            var traits = UiKit.Rect("Traits", window.transform);
            traits.Place(Vector2.zero, Vector2.one, 394, 30, 44, 84);
            traits.Stack<VerticalLayoutGroup>(0);
            var heads = TraitColumns(traits, 30);
            UiKit.Lbl("Черта", heads[0], 10, UiKit.Hex(0x6F9CCC), 0.3f).rectTransform.Fill();
            UiKit.Lbl("Ур.", heads[1], 10, UiKit.Hex(0x6F9CCC), 0.3f).rectTransform.Fill();
            UiKit.Lbl("К следующему", heads[2], 10, UiKit.Hex(0x6F9CCC), 0.3f).rectTransform.Fill();
            UiKit.Lbl("Даёт сейчас", heads[3], 10, UiKit.Hex(0x6F9CCC), 0.3f).rectTransform.Fill();
            UiKit.Lbl("Растёт от", heads[4], 10, UiKit.Hex(0x6F9CCC), 0.3f).rectTransform.Fill();

            foreach (Trait trait in Enum.GetValues(typeof(Trait)))
            {
                TraitRow(traits, p, trait);
            }

            UiKit.Footer(bookStage, new[] { ("Q+E", "Страница"), ("O", null), ("Esc", "Закрыть") });
        }

        static void SystemGem(Transform parent)
        {
            var gem = UiKit.Image("Gem", parent, UiKit.Hex(0x9FD2FF), UiKit.Diamond);
            gem.Size(8, 8, 0);
            UiKit.HaloAt(gem.transform, SystemBlue.A(0.6f), Vector2.zero, 24);
        }

        static void SystemLabel(Transform parent, string text) => UiKit.Lbl(text, parent, 11, SystemPale).Size(14);

        /// <summary>A bar of the status window: its name in the system's blue, its numbers at the right, the bar under them.</summary>
        static void SystemBar(Transform parent, string name, string value, float share, UiKit.BarKind kind)
        {
            var holder = UiKit.Rect(name, parent);
            holder.Stack<VerticalLayoutGroup>(6);
            holder.Size(28);
            var line = UiKit.Rect("Line", holder);
            line.Size(18);
            UiKit.Label(name, line, 13, SystemPale).rectTransform.Fill();
            UiKit.Label(value, line, 13, UiKit.Text, TextAnchor.MiddleRight).rectTransform.Fill();
            UiKit.Bar(holder, kind, share);
        }

        /// <summary>A row of the traits' table: five cells (130, 54, the rest, 170, 160 wide, 16 apart), a hairline under it.</summary>
        static RectTransform[] TraitColumns(Transform parent, float height)
        {
            var row = UiKit.Rect("Row", parent);
            var group = row.Stack<HorizontalLayoutGroup>(16, null, TextAnchor.MiddleLeft);
            group.childForceExpandWidth = false;
            row.Size(height);
            var rule = UiKit.Image("Rule", row, SystemBlue.A(0.12f), UiKit.Plain);
            rule.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -1);
            rule.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var widths = new[] { 130f, 54f, -1f, 170f, 160f };
            var cells = new RectTransform[widths.Length];

            for (var i = 0; i < widths.Length; i++)
            {
                cells[i] = UiKit.Rect("Cell", row);
                cells[i].Size(height, widths[i] > 0 ? widths[i] : -1, widths[i] > 0 ? 0 : 1);
            }

            return cells;
        }

        /// <summary>A trait of the body: its name and short name, its level, how far toward the next, what it gives, what trains it.</summary>
        static void TraitRow(Transform parent, Physique p, Trait trait)
        {
            var cells = TraitColumns(parent, 64);
            UiKit.Disp(Physique.Name(trait), cells[0], 22).rectTransform.Place(new Vector2(0, 0.42f), Vector2.one);
            UiKit.Lbl(Physique.Short(trait), cells[0], 10, UiKit.Hex(0x6F9CCC), 0.2f).rectTransform.Place(Vector2.zero, new Vector2(1, 0.42f));
            var level = UiKit.Disp($"{p.Level(trait)}", cells[1], 36, UiKit.Hex(0xDFF0FF));
            level.Shadowed().GetComponent<Shadow>().effectColor = SystemBlue.A(0.5f);
            level.rectTransform.Fill();
            var share = p.Progress(trait);
            var bar = UiKit.Bar(cells[2], UiKit.BarKind.System, share);
            bar.transform.parent.GetComponent<RectTransform>().Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), 0, 2, 0, -6);
            UiKit.Label($"{share * 100:0}%", cells[2], 11, SystemPale).rectTransform.Place(Vector2.zero, new Vector2(1, 0.5f), 0, 0, 0, 6);
            var gives = UiKit.Label(p.Gives(trait), cells[3], 13, UiKit.Text);
            gives.rectTransform.Fill();
            var trained = UiKit.Label(Physique.HowTrained(trait), cells[4], 12, UiKit.Hex(0x8FA6B8));
            trained.rectTransform.Fill();
        }
    }
}
