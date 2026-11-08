using GuWorld.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The journal (J, UI-Journal.dc.html): at the left the academy's task in hand on a panel
    /// — how far along in a gold arc, who gave it, what it asks, what it pays — and under it the
    /// side tasks taken, each with its progress and a way to give it up, and the places still
    /// free; at the right the clan — the merit big, the standing, the discount, where to earn
    /// more — and how the mountain's other clans stand toward the hero.
    /// </summary>
    public partial class Menus
    {
        void JournalPage()
        {
            var body = BookFrame("book", "Журнал", Page.Journal);
            var quests = host.Quests;
            var brush = UiKit.Brush("誌", bookStage, 540, UiKit.Gold.A(0.035f));
            brush.rectTransform.anchorMin = brush.rectTransform.anchorMax = new Vector2(0, 1);
            brush.rectTransform.anchoredPosition = new Vector2(420 + 270, -150 - 270);
            brush.transform.SetSiblingIndex(1);

            // The tasks.
            var left = UiKit.Rect("Tasks", body);
            left.Place(Vector2.zero, Vector2.one, 0, 0, 508, 0);
            left.Stack<VerticalLayoutGroup>(14);
            UiKit.Lbl("Задание академии", left).Size(14);

            if (quests.Current is QuestKind current)
            {
                MainTask(left, current, quests.ProgressOf(current), quests.Ready);
            }
            else
            {
                var none = UiKit.Panel("None", left);
                none.Size(80);
                var words = quests.Next is QuestKind next ? $"Вас ждёт {next.GiverTitle} {next.Giver} — с новым поручением." : "Все задания академии выполнены.";
                UiKit.Label(words, none.transform, 15, UiKit.Text2).rectTransform.Fill(24);
            }

            var sideHead = UiKit.Rect("Side Head", left);
            sideHead.Size(24);
            UiKit.Lbl($"Побочные поручения · {quests.Sides.Count} из {Quests.MaxSides}", sideHead).rectTransform.Fill();
            var where = UiKit.Rect("Where", sideHead);
            where.Fill();
            var whereRow = where.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleRight);
            whereRow.childForceExpandWidth = false;
            UiKit.Label("Поручения дают жители деревни", where, 12, UiKit.Muted).Size(24);
            UiKit.Key("E", where);
            UiKit.Label("и павильон клана", where, 12, UiKit.Muted).Size(24);

            foreach (var side in quests.Sides)
            {
                SideTask(left, side, quests.ProgressOf(side), quests.IsReady(side));
            }

            for (var i = quests.Sides.Count; i < Quests.MaxSides; i++)
            {
                var free = UiKit.Image("Free", left, new Color(0, 0, 0, 0), UiKit.Plain);
                free.Size(64);
                UiKit.Frame(free.transform, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.1f));
                UiKit.Label("Свободное место для поручения", free.transform, 13, UiKit.Hex(0x4E5E61), TextAnchor.MiddleCenter).rectTransform.Fill();
            }

            // The clan.
            var right = UiKit.Rect("Clan", body);
            right.Place(new Vector2(1, 0), Vector2.one, -440, 0, 0, 0);
            right.Stack<VerticalLayoutGroup>(16);
            var clanHead = UiKit.Rect("Head", right);
            var clanRow = clanHead.Stack<HorizontalLayoutGroup>(14, null, TextAnchor.MiddleLeft);
            clanRow.childForceExpandWidth = false;
            clanHead.Size(44);
            var moon = UiKit.Icon("moon", clanHead, 32, UiKit.Hex(0xC9D2DC));
            moon.Size(40, 40, 0);
            UiKit.Orbit(moon.transform, 40, UiKit.Gold.A(0.4f), 0, 4);
            var clanName = UiKit.Rect("Name", clanHead);
            clanName.Stack<VerticalLayoutGroup>(4);
            clanName.Size(44, 260, 0);
            UiKit.Lbl("Клан", clanName).Size(12);
            UiKit.Disp("Гу Юэ", clanName, 30).Size(28);

            var merit = UiKit.Rect("Merit", right);
            var meritRow = merit.Stack<HorizontalLayoutGroup>(16, null, TextAnchor.LowerLeft);
            meritRow.childForceExpandWidth = false;
            merit.Size(74);
            var number = UiKit.Disp($"{quests.Merit}", merit, 76, UiKit.GoldLit, TextAnchor.LowerLeft);
            number.horizontalOverflow = HorizontalWrapMode.Overflow;
            number.Size(74, number.preferredWidth + 4, 0);
            var meritWord = UiKit.Lbl("Вклад", merit, 12, UiKit.LabelGold, 0.24f, TextAnchor.LowerLeft);
            meritWord.Size(60);

            var stats = UiKit.Rect("Stats", right);
            stats.Stack<VerticalLayoutGroup>(0);
            Stat(stats, "Всего заработано", $"{quests.MeritEarned}");
            Stat(stats, "Положение", quests.Standing, UiKit.Gold);
            Stat(stats, "Скидка в лавках", quests.Discount > 0 ? $"{quests.Discount * 100:0}%" : "нет");
            UiKit.Label($"Вклад дают задания и поручения. Распорядительница {SideQuests.Steward} в павильоне клана каждый день даёт три новых " +
                        "поручения и держит сокровищницу — там за вклад Гу, которых не купить за камни.", right, 13, UiKit.Muted, TextAnchor.UpperLeft).Size(62);
            UiKit.Rule(right, 0.3f);
            UiKit.Lbl("Кланы горы", right).Size(14);

            foreach (var rival in GuWorld.Creatures.Clans.Rivals)
            {
                var h = GuWorld.Creatures.Clans.Hostility(rival);
                var colour = h >= GuWorld.Creatures.Clans.War ? UiKit.Danger : h >= GuWorld.Creatures.Clans.OnSight ? UiKit.Hex(0xF0C868) : UiKit.Moon;
                var holder = UiKit.Rect(rival, right);
                holder.Stack<VerticalLayoutGroup>(6);
                holder.Size(40);
                var line = UiKit.Rect("Line", holder);
                line.Size(26);
                UiKit.Disp(GuWorld.Creatures.MasterKinds.ClanName(rival), line, 22).rectTransform.Fill();
                UiKit.Label($"{GuWorld.Creatures.Clans.Standing(rival)} · вражда {h:0}", line, 12, colour, TextAnchor.MiddleRight).rectTransform.Fill();
                var track = UiKit.Image("Track", holder, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.08f), UiKit.Plain);
                track.Size(4);
                var fill = UiKit.Image("Fill", track.transform, Color.white, UiKit.Plain);
                fill.rectTransform.Place(Vector2.zero, new Vector2(Mathf.Clamp01(h / 100f), 1));
                fill.Gradient(UiKit.GoldDeep, colour, true);

                foreach (var (at, mark) in new[] { (GuWorld.Creatures.Clans.OnSight / 100f, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.4f)), (GuWorld.Creatures.Clans.War / 100f, UiKit.Danger.A(0.6f)) })
                {
                    var tick = UiKit.Image("Tick", track.transform, mark, UiKit.Plain);
                    tick.rectTransform.At(new Vector2(at, 0.5f), Vector2.zero, new Vector2(1, 10));
                }
            }

            UiKit.Label($"Убитые Гу-мастера злят клан: от {GuWorld.Creatures.Clans.OnSight:0} дозоры нападают без слов, от {GuWorld.Creatures.Clans.War:0} его отряды " +
                        "охотятся на вас в лесах. Со временем вражда остывает.", right, 12, UiKit.Muted, TextAnchor.UpperLeft).Size(44);

            UiKit.Footer(bookStage, new[] { ("M", "Карта"), ("Q+E", "Страница"), ("Esc", "Закрыть") });
        }

        /// <summary>The academy's task: a gold arc of how far along, its title, giver and goal, what it pays and a way to the map.</summary>
        void MainTask(Transform parent, QuestKind quest, int progress, bool ready)
        {
            var card = UiKit.Panel(quest.Title, parent);
            card.Size(150);
            var row = card.rectTransform.Stack<HorizontalLayoutGroup>(26, new RectOffset(28, 28, 24, 24), TextAnchor.MiddleLeft);
            row.childForceExpandWidth = false;

            var arc = UiKit.Rect("Arc", card.transform);
            arc.Size(96, 96, 0);
            var track = UiKit.Image("Track", arc, UiKit.Gold.A(0.15f), UiKit.ArcRing(86, 3));
            track.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(86, 86));
            var fill = UiKit.Image("Fill", arc, ready ? UiKit.Ok : UiKit.Gold, UiKit.ArcRing(86, 3));
            Radial(fill);
            fill.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(86, 86));
            fill.fillAmount = quest.Target > 0 ? Mathf.Clamp01(progress / (float)quest.Target) : 1;
            UiKit.Disp($"{progress}/{quest.Target}", arc, 30, UiKit.Text, TextAnchor.MiddleCenter).rectTransform.Fill();

            var words = UiKit.Rect("Words", card.transform);
            words.Stack<VerticalLayoutGroup>(6, null, TextAnchor.MiddleLeft);
            words.Size(100, -1, 1);
            var title = UiKit.Disp(quest.Title, words, 32);
            title.Size(34);
            UiKit.Label($"{quest.Giver} · {quest.GiverTitle}", words, 13, UiKit.Muted).Size(18);
            UiKit.Label(ready ? $"Готово — вернитесь к: {quest.Giver}" : quest.Goal, words, 15, ready ? UiKit.Ok : UiKit.Text2).Size(36);

            var pay = UiKit.Rect("Pay", card.transform);
            pay.Stack<VerticalLayoutGroup>(6, null, TextAnchor.MiddleRight);
            pay.Size(100, 170, 0);
            UiKit.Lbl("Награда", pay, 10, UiKit.Muted, 0.3f, TextAnchor.MiddleRight).Size(14);
            UiKit.Label(Items.ItemBook.Stones(quest.RewardStones), pay, 13, UiKit.Text, TextAnchor.MiddleRight).Size(18);

            if (quest.RewardMerit > 0)
            {
                UiKit.Label($"+{quest.RewardMerit} вклада", pay, 13, UiKit.Gold, TextAnchor.MiddleRight).Size(18);
            }

            var toMap = UiKit.Lbl("Показать на карте · M", pay, 11, UiKit.Gold, 0.18f, TextAnchor.MiddleRight);
            toMap.raycastTarget = true;
            toMap.gameObject.AddComponent<Clickable>().Click = () => host.OpenPage(Page.Map);
            toMap.Size(20);
        }

        /// <summary>A side task: its title and giver, what it asks; how far along and what it pays; giving it up.</summary>
        void SideTask(Transform parent, QuestKind quest, int progress, bool ready)
        {
            var card = UiKit.Panel(quest.Title, parent);
            card.Size(96);
            var row = card.rectTransform.Stack<HorizontalLayoutGroup>(24, new RectOffset(24, 24, 18, 18), TextAnchor.MiddleLeft);
            row.childForceExpandWidth = false;
            var words = UiKit.Rect("Words", card.transform);
            words.Stack<VerticalLayoutGroup>(5, null, TextAnchor.MiddleLeft);
            words.Size(60, -1, 1);
            UiKit.Label($"<size=24>{quest.Title}</size>  <color=#7E9093>· {quest.Giver}</color>", words, 12, UiKit.Text).font = UiKit.Display;
            words.GetChild(0).Size(28);
            UiKit.Label(ready ? $"Готово — вернитесь к: {quest.Giver}" : quest.Goal, words, 14, ready ? UiKit.Ok : UiKit.Text2).Size(20);

            var how = UiKit.Rect("Progress", card.transform);
            how.Stack<VerticalLayoutGroup>(6, null, TextAnchor.MiddleLeft);
            how.Size(60, 180, 0);
            var line = UiKit.Rect("Line", how);
            line.Size(16);
            UiKit.Label("Прогресс", line, 12, UiKit.Muted).rectTransform.Fill();
            UiKit.Label($"{progress} / {quest.Target}", line, 12, UiKit.Text, TextAnchor.MiddleRight).rectTransform.Fill();
            var track = UiKit.Image("Track", how, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.08f), UiKit.Plain);
            track.Size(4);
            var fill = UiKit.Image("Fill", track.transform, ready ? UiKit.Ok : UiKit.Gold, UiKit.Plain);
            fill.rectTransform.Place(Vector2.zero, new Vector2(quest.Target > 0 ? Mathf.Clamp01(progress / (float)quest.Target) : 1, 1));
            UiKit.Label(Items.ItemBook.Stones(quest.RewardStones) + (quest.RewardMerit > 0 ? $" · +{quest.RewardMerit} вклада" : ""), how, 12, UiKit.Hex(0xA8B6B8)).Size(16);

            UiKit.Tb("Оставить", card.transform, false, () =>
            {
                host.Quests.Drop(quest);
                Show(Page.Journal);
            }, 11).Size(34, 120, 0);
        }
    }
}
