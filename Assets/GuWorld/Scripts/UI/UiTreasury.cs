using GuWorld.Core;
using UnityEngine;

namespace GuWorld.UI
{
    /// <summary>The Gu Yue clan's treasury: the hero's clan rank and merit, and what merit buys.</summary>
    public partial class Ui
    {
        void TreasuryPage()
        {
            var session = S;
            var rank = session.ClanRank;
            var next = rank + 1 < Session.ClanRankNeeds.Length ? $" · до «{Session.ClanRanks[rank + 1].ToLowerInvariant()}» {Session.ClanRankNeeds[rank + 1] - session.State.MeritEarned} заслуг" : "";
            var body = Frame("庫", "Сокровищница клана", $"{Session.ClanRanks[rank]} · заслуги {session.State.Merit}{next}", false, null, null, new[] { ("Esc", "Закрыть") });
            var left = Column(body, 0, 0.6f, 6);
            UiKit.Lbl("Что даёт клан", left, 11, UiKit.LabelGold).Size(18);
            var list = Scroll(Rest(left));

            foreach (var entry in Session.Treasury)
            {
                var e = entry;
                var gu = entry.Gu != null ? GuBook.Get(entry.Gu) : null;
                var item = entry.Item != null ? ItemBook.Get(entry.Item) : null;
                var name = gu != null ? gu.Name + (entry.Quality > 0 ? $" ({GuBook.QualityNames[entry.Quality]})" : "") : item?.Name ?? "?";
                var seal = gu?.Seal ?? item?.Seal ?? "宝";
                var why = session.WhyNoTreasury(entry);
                ListLine(list, seal, name, why == null ? $"{entry.Merit} заслуг" : entry.ClanRank > rank ? Session.ClanRanks[entry.ClanRank] : $"{entry.Merit} заслуг", why == null ? UiKit.Text : UiKit.Muted, false, () =>
                {
                    if (session.TakeFromTreasury(e))
                    {
                        Sound.AudioDirector.Play("loot", null, 0.8f, 0);
                    }
                    else
                    {
                        Toast(session.WhyNoTreasury(e));
                    }

                    Redraw();
                }, why == null ? UiKit.GoldText : UiKit.Danger);
            }

            var right = Column(body, 0.65f, 1, 8);
            UiKit.Lbl("Ранги клана", right, 11, UiKit.LabelGold).Size(18);

            for (var i = 0; i < Session.ClanRanks.Length; i++)
            {
                Fact(right, Session.ClanRanks[i], $"{Session.ClanRankNeeds[i]} заслуг", i == rank ? UiKit.GoldText : i < rank ? UiKit.Text2 : UiKit.Muted);
            }

            Gap(right, 8);
            Para("Заслуги дают поручения доски Гу Юэ, отбитые волчьи приливы, задания клана и первые победы над великими зверями.", right, 14, UiKit.Muted);
        }
    }
}
