using System;
using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The pages of the wider mountain: a conversation with answers, the furnace, a settlement's board
    /// of errands, the codex, the clans, travel between lanterns, an inn's bed, and the game's settings.
    /// </summary>
    public partial class Ui
    {
        // ------------------------------------------------------------------ Shops of the clans

        string shopShelf = "valley";
        string shopTitle = "Лавка Баошаня";
        string shopClan = "gu_yue";

        /// <summary>Opens a shelf of the Markets at the prices its clan asks of the hero.</summary>
        public void OpenShop(string shelf, string title, string clan)
        {
            shopShelf = string.IsNullOrEmpty(shelf) ? "valley" : shelf;
            shopTitle = string.IsNullOrEmpty(title) ? "Лавка" : title;
            shopClan = clan ?? "";
            Show(shopShelf == "gamble" ? Page.Gamble : Page.Shop);
        }

        // ------------------------------------------------------------------ Conversations

        DialogueRun dialogue;
        int dialogueLine;
        Action dialogueDone;

        /// <summary>Holds a conversation: its lines one by one, then the answers.</summary>
        public void Converse(DialogueRun run, Action done)
        {
            dialogue = run;
            dialogueLine = 0;
            dialogueDone = done;

            if (run == null || run.Over)
            {
                EndDialogue();
                return;
            }

            Show(Page.Dialogue);
        }

        void EndDialogue()
        {
            var done = dialogueDone;
            dialogueDone = null;
            dialogue = null;

            if (Open == Page.Dialogue)
            {
                Close();
            }

            done?.Invoke();
        }

        void DialogueKeys()
        {
            if (dialogue == null)
            {
                return;
            }

            var lines = dialogue.Lines;

            if (dialogueLine < lines.Length - 1)
            {
                if (Keys.Down(Key.Space) || Keys.Down(Key.Enter) || Keys.Down(Key.T) || Keys.LeftDown)
                {
                    dialogueLine++;
                    Redraw();
                }

                return;
            }

            var answers = dialogue.Answers;
            Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };

            for (var i = 0; i < answers.Count && i < digits.Length; i++)
            {
                if (Keys.Down(digits[i]))
                {
                    Answer(i);
                    return;
                }
            }

            if (Keys.Down(Key.Escape))
            {
                EndDialogue();
            }
        }

        void Answer(int index)
        {
            if (dialogue == null || !dialogue.Choose(index))
            {
                return;
            }

            if (Open != Page.Dialogue)
            {
                // An effect of the answer opened another page (a shop, a fight): the conversation is over.
                dialogue = null;
                dialogueDone = null;
                return;
            }

            dialogueLine = 0;

            if (dialogue.Over)
            {
                EndDialogue();
            }
            else if (Open == Page.Dialogue)
            {
                Redraw();
            }
        }

        void DialoguePage()
        {
            var run = dialogue;

            if (run == null || run.Node == null)
            {
                return;
            }

            var lines = run.Lines;
            var stage = UiKit.Stage(pageRoot);
            var shade = UiKit.Image("Shade", stage, Color.white, UiKit.Plain);
            shade.rectTransform.Fill();
            shade.Gradient(new Color(3 / 255f, 4 / 255f, 10 / 255f, 0), new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.97f), false, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.2f), 0.5f);
            var words = UiKit.Rect("Words", stage);
            words.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), -480, 60, -480, -420);
            words.Stack<VerticalLayoutGroup>(10);
            var line = lines.Length > 0 ? lines[Mathf.Clamp(dialogueLine, 0, lines.Length - 1)] : new Line(run.Npc?.Id ?? "", "…");
            var speaker = line.Who == "hero" ? null : Atlas.Npc(line.Who) ?? run.Npc;
            var who = UiKit.Rect("Who", words);
            who.Size(36);
            var row = who.Stack<HorizontalLayoutGroup>(14, null, TextAnchor.MiddleLeft);
            row.childForceExpandWidth = false;
            UiKit.Disp(line.Who == "hero" ? "Гу Юэ " + S.State.HeroName : speaker?.Name ?? "", who, 30, UiKit.GoldText).Size(36);

            if (speaker != null && line.Who != "hero")
            {
                var clan = Clans.Get(speaker.Clan);
                UiKit.Lbl(speaker.Title, who, 11, UiKit.Muted, 0.3f).Size(36);

                if (clan != null)
                {
                    UiKit.Chip($"{clan.Name} · {Clans.StandingName(S.StandingWith(clan.Id))}", who, UiKit.Hex(clan.Colour));
                }
            }

            UiKit.Rule(words);
            var said = UiKit.Disp(line.Who == "" ? line.Text : $"«{line.Text}»", words, 25, UiKit.Hex(0xF2EADB), TextAnchor.UpperLeft);
            said.horizontalOverflow = HorizontalWrapMode.Wrap;
            said.lineSpacing = 1.05f;
            said.Size(110);

            if (dialogueLine < lines.Length - 1)
            {
                UiKit.Footer(stage, new[] { ("Пробел", "Дальше"), ("Esc", "Уйти") }, 56, 22, false, $"{dialogueLine + 1} / {lines.Length}");
                stage.gameObject.AddComponent<PageKeys>();

                return;
            }

            // The answers.
            Gap(words, 6);
            var answers = run.Answers;

            for (var i = 0; i < answers.Count; i++)
            {
                var answer = answers[i];
                var index = i;
                var (answerRow, _) = UiKit.ListRow(words, 38, false, answer.Enabled ? () => Answer(index) : (Action)null);
                var inside = answerRow.Stack<HorizontalLayoutGroup>(12, new RectOffset(16, 14, 0, 0), TextAnchor.MiddleLeft);
                inside.childForceExpandWidth = false;
                UiKit.Key($"{i + 1}", answerRow, answer.Enabled ? UiKit.Gold : UiKit.Muted);
                UiKit.Label(answer.Text, answerRow, 17, answer.Enabled ? UiKit.Text : UiKit.Muted).Size(38, -1, 1);

                if (!answer.Enabled && answer.Why != "")
                {
                    UiKit.Label(answer.Why, answerRow, 13, UiKit.Danger, TextAnchor.MiddleRight).Size(38, 260, 0);
                }
            }

            UiKit.Footer(stage, new[] { ("1–9", "Ответить"), ("Esc", "Уйти") });
        }

        // ------------------------------------------------------------------ The furnace

        string pickedPill;
        Batch lastBatch;

        void AlchemyPage()
        {
            var session = S;
            var known = session.KnownPills();
            var body = Frame("丹", "Котёл", $"Мастерство {session.State.Refining} · {(int)(session.RefiningShare * 100)}% до следующего", false, null, Purse,
                new[] { ("Enter", "Варить"), ("Esc", "Уйти") });
            var left = Column(body, 0, 0.44f, 6);
            UiKit.Lbl("Рецепты", left, 11, UiKit.LabelGold).Size(18);
            var list = Scroll(Rest(left));

            if (!known.Exists(r => r.Id == pickedPill))
            {
                pickedPill = known.Count > 0 ? known[0].Id : null;
            }

            foreach (var recipe in known)
            {
                var id = recipe.Id;
                var item = ItemBook.Get(recipe.Result);
                var can = session.BatchesPossible(recipe);
                ListLine(list, item.Seal, item.Name, can > 0 ? $"можно ×{can}" : "нет трав", UiKit.Hex(item.Colour), id == pickedPill, () => { pickedPill = id; lastBatch = null; Redraw(); }, can > 0 ? UiKit.Ok : UiKit.Muted);
            }

            var right = Column(body, 0.5f, 1, 8);

            if (pickedPill == null)
            {
                Para("Рецептов пока нет. Их продают травники и алхимики, а некоторые записаны в книгах.", right, 16, UiKit.Muted);
                return;
            }

            var picked = Alchemy.Get(pickedPill);
            var result = ItemBook.Get(picked.Result);
            UiKit.Zh(result.Seal, right, 60, UiKit.Hex(result.Colour)).Size(70);
            UiKit.Disp(picked.Count > 1 ? $"{result.Name} × {picked.Count}" : result.Name, right, 28, UiKit.Text).Size(34);
            UiKit.Lbl($"Сложность {picked.Difficulty} · удача {(int)(session.BatchChance(picked) * 100)}%", right, 11, session.BatchChance(picked) >= 1 ? UiKit.Ok : UiKit.Hunger).Size(16);
            UiKit.Rule(right);
            Para(result.Description, right, 15);
            Gap(right, 8);
            UiKit.Lbl("Что нужно", right, 11, UiKit.LabelGold).Size(18);

            foreach (var (item, count) in picked.Items)
            {
                var def = ItemBook.Get(item);
                var have = session.Count(item);
                Fact(right, def.Name, $"{have} / {count}", have >= count ? UiKit.Ok : UiKit.Danger);
            }

            if (picked.Stones > 0)
            {
                Fact(right, "Первобытные камни", $"{session.State.Stones} / {picked.Stones}", session.State.Stones >= picked.Stones ? UiKit.Ok : UiKit.Danger);
            }

            Gap(right, 10);
            var why = session.WhyNoBatch(picked);

            void Brew(int times)
            {
                for (var i = 0; i < times && session.WhyNoBatch(picked) == null; i++)
                {
                    lastBatch = session.Brew(picked.Id);
                }

                Redraw();
            }

            UiKit.HexButton("Варить", right, () => Brew(1), why == null ? UiKit.HexKind.Main : UiKit.HexKind.Off, 13, 52, "Enter");

            if (why == null && session.BatchesPossible(picked) > 1)
            {
                UiKit.HexButton($"Варить всё (×{session.BatchesPossible(picked)})", right, () => Brew(session.BatchesPossible(picked)), UiKit.HexKind.Ghost, 12, 44);
            }

            if (why != null)
            {
                Para(why, right, 14, UiKit.Danger);
            }
            else if (lastBatch != null)
            {
                Para(lastBatch.Ok ? $"Готово: {result.Name} × {lastBatch.Count}" + (lastBatch.Bonus ? " — одна лишняя!" : "") : lastBatch.Why, right, 14, lastBatch.Ok ? UiKit.Ok : UiKit.Danger);
            }

            body.gameObject.AddComponent<PageKeys>().On(Key.Enter, () => { if (session.WhyNoBatch(picked) == null) { Brew(1); } });
        }

        // ------------------------------------------------------------------ A settlement's board

        string boardSettlement = "";
        string pickedBounty;

        public void OpenBoard(string settlement)
        {
            boardSettlement = settlement;
            Show(Page.Bounties);
        }

        void BountyPage()
        {
            var session = S;
            var settlement = Regions.Settlement(boardSettlement);
            var body = Frame("榜", "Доска поручений", settlement != null ? settlement.Name : "", false, null, Purse);
            var left = Column(body, 0, 0.46f, 6);
            UiKit.Lbl($"День {session.State.Day}", left, 11, UiKit.LabelGold).Size(18);
            var list = Scroll(Rest(left));
            var errands = session.BoardToday(boardSettlement);

            if (!errands.Exists(e => e.Id == pickedBounty))
            {
                pickedBounty = errands.Count > 0 ? errands[0].Id : null;
            }

            foreach (var errand in errands)
            {
                var id = errand.Id;
                var ready = session.BountyReady(errand);
                var state = errand.Done ? "сдано" : ready ? "готово" : errand.Taken ? "взято" : $"{errand.Stones} камней";
                ListLine(list, errand.Kind == "kill" ? "猎" : "采", session.BountyText(errand), state, errand.Done ? UiKit.Muted : UiKit.Text2, id == pickedBounty, () => { pickedBounty = id; Redraw(); },
                    ready ? UiKit.Ok : errand.Taken ? UiKit.Essence : UiKit.GoldText);
            }

            var right = Column(body, 0.52f, 1, 8);
            var picked = errands.Find(e => e.Id == pickedBounty);

            if (picked == null)
            {
                Para("Доска пуста.", right, 16, UiKit.Muted);
                return;
            }

            UiKit.Disp(picked.Kind == "kill" ? "Охота" : "Сбор", right, 30, UiKit.Text).Size(38);
            UiKit.Rule(right);
            Para(session.BountyText(picked), right, 17, UiKit.Text);
            Gap(right, 6);
            Fact(right, "Награда", $"{picked.Stones} камней", UiKit.GoldText);
            Fact(right, "Прозрение", $"+{picked.Insight}", UiKit.Essence);

            if (settlement != null)
            {
                Fact(right, Clans.Get(Clans.OfSettlement(settlement.Id))?.Name ?? "", $"+{picked.Reputation}", UiKit.Ok);
            }

            Fact(right, "Срок", $"{Session.BountyDays} дн.");
            Gap(right, 10);

            if (picked.Done)
            {
                Para("Сдано. Писарь вычеркнул поручение.", right, 15, UiKit.Muted);
            }
            else if (!picked.Taken)
            {
                UiKit.HexButton("Взять", right, () => { session.TakeBounty(picked); Redraw(); }, UiKit.HexKind.Main, 13, 52);
            }
            else if (session.BountyReady(picked))
            {
                UiKit.HexButton("Сдать", right, () => { session.CompleteBounty(picked); Redraw(); }, UiKit.HexKind.Main, 13, 52);
            }
            else
            {
                Para(picked.Kind == "kill" ? "Добыча ещё бегает по горе." : "В сумке ещё не хватает.", right, 15, UiKit.Muted);
            }
        }

        // ------------------------------------------------------------------ The codex

        CodexKind codexKind = CodexKind.Lore;
        string pickedPage;

        static string CodexName(CodexKind kind)
        {
            switch (kind)
            {
                case CodexKind.Lore: return "Записи";
                case CodexKind.Place: return "Места";
                case CodexKind.Beast: return "Звери";
                case CodexKind.Gu: return "Гу";
                case CodexKind.Herb: return "Травы";
                default: return "Люди";
            }
        }

        void CodexPage()
        {
            var session = S;
            var body = Frame("典", "Кодекс", $"Открыто страниц: {session.State.Codex.Count}", true, null, null, new[] { ("Q E", "Страницы"), ("Tab", "Раздел"), ("Esc", "Закрыть") });
            var kinds = (CodexKind[])Enum.GetValues(typeof(CodexKind));
            body.gameObject.AddComponent<PageKeys>().On(Key.Tab, () => { codexKind = kinds[(Array.IndexOf(kinds, codexKind) + 1) % kinds.Length]; pickedPage = null; Redraw(); });
            var left = Column(body, 0, 0.42f, 6);
            var tabs = UiKit.Rect("Tabs", left);
            tabs.Stack<HorizontalLayoutGroup>(16, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
            tabs.Size(40);

            foreach (var kind in kinds)
            {
                var k = kind;
                var (open, all) = session.CodexCount(kind);
                UiKit.Tb($"{CodexName(kind)} {open}/{all}", tabs, kind == codexKind, () => { codexKind = k; pickedPage = null; Redraw(); }, 11);
            }

            var list = Scroll(Rest(left));
            var pages = session.CodexPages(codexKind);

            if (!pages.Exists(p => p.Id == pickedPage))
            {
                pickedPage = pages.Count > 0 ? pages[0].Id : null;
            }

            foreach (var page in pages)
            {
                var id = page.Id;
                ListLine(list, Seal(page), page.Title, "", UiKit.Text2, id == pickedPage, () => { pickedPage = id; Redraw(); });
            }

            var right = Column(body, 0.48f, 1, 8);
            var picked = pages.Find(p => p.Id == pickedPage);

            if (picked == null)
            {
                Para("Здесь пока пусто. Страницы открываются сами: в пути, в бою, в книгах и на старых камнях.", right, 16, UiKit.Muted);
                return;
            }

            UiKit.Zh(Seal(picked), right, 54, UiKit.Gold).Size(62);
            UiKit.Disp(picked.Title, right, 30, UiKit.Text).Size(38);
            UiKit.Rule(right);
            Para(picked.Text, right, 16);
            Gap(right, 8);
            PageFacts(right, picked);
        }

        static string Seal(CodexEntry page)
        {
            var id = page.Id.Substring(page.Id.IndexOf(':') + 1);

            switch (page.Kind)
            {
                case CodexKind.Gu: return GuBook.Get(id)?.Seal ?? "蛊";
                case CodexKind.Herb: return ItemBook.Get(id)?.Seal ?? "草";
                case CodexKind.Beast: return "兽";
                case CodexKind.Place: return "地";
                case CodexKind.Person: return "人";
                default: return "书";
            }
        }

        /// <summary>The numbers a page of the codex knows: a beast's level and kills, a Gu's path, a herb's price.</summary>
        void PageFacts(RectTransform right, CodexEntry page)
        {
            var id = page.Id.Substring(page.Id.IndexOf(':') + 1);

            switch (page.Kind)
            {
                case CodexKind.Beast:
                {
                    var enemy = Bestiary.Get(id);

                    if (enemy != null)
                    {
                        Fact(right, "Уровень", $"{enemy.Level}");
                        Fact(right, "Здоровье", $"{enemy.Health:0}");
                        Fact(right, "Побеждено", $"{S.KillsOf(id)}", UiKit.GoldText);

                        if (enemy.Inflicts != StatusKind.None)
                        {
                            Fact(right, "Оставляет", Session.StatusName(enemy.Inflicts), UiKit.Danger);
                        }

                        if (enemy.Nocturnal)
                        {
                            Fact(right, "Охотится", "ночью");
                        }
                    }

                    break;
                }
                case CodexKind.Gu:
                {
                    var gu = GuBook.Get(id);

                    if (gu != null)
                    {
                        Fact(right, "Путь", GuBook.PathName(gu.Path));
                        Fact(right, "Ранг", GuBook.RankNumerals[gu.Rank]);
                        Fact(right, "Корм", gu.Food == "" ? "не нужен" : $"{ItemBook.Get(gu.Food)?.Name} раз в {gu.Appetite} дн.");

                        if (gu.Clan != "")
                        {
                            Fact(right, "Хранит", Clans.Get(gu.Clan)?.Name ?? gu.Clan);
                        }
                    }

                    break;
                }
                case CodexKind.Herb:
                {
                    var herb = ItemBook.Get(id);

                    if (herb != null)
                    {
                        Fact(right, "Цена", $"{herb.Price} камней", UiKit.GoldText);
                        Fact(right, "В сумке", $"{S.Count(id)}");
                    }

                    break;
                }
                case CodexKind.Person:
                {
                    var npc = Atlas.Npc(id);

                    if (npc != null)
                    {
                        Fact(right, "Клан", Clans.Get(npc.Clan)?.Name ?? "");
                        Fact(right, "Где", npc.Home != "" ? Regions.Settlement(npc.Home)?.Name ?? "" : Atlas.NameAt(npc.At));
                    }

                    break;
                }
            }
        }

        // ------------------------------------------------------------------ The clans

        string pickedClan = "gu_yue";

        void ClansPage()
        {
            var session = S;
            var body = Frame("族", "Кланы", "Что о вас думают на горе", true, null, null, new[] { ("Q E", "Страницы"), ("Esc", "Закрыть") });
            var left = Column(body, 0, 0.5f, 10);

            foreach (var clan in Clans.All)
            {
                var id = clan.Id;
                var value = session.ReputationOf(id);
                var standing = session.StandingWith(id);
                var (row, _) = UiKit.ListRow(left, 64, id == pickedClan, () => { pickedClan = id; Redraw(); });
                var inside = row.Stack<VerticalLayoutGroup>(4, new RectOffset(22, 18, 10, 8));
                var top = UiKit.Rect("Top", row);
                top.Size(24);
                UiKit.Zh(clan.Seal, top, 20, UiKit.Hex(clan.Colour)).rectTransform.Fill();
                var name = UiKit.Label("      " + clan.Name, top, 16, UiKit.Text);
                name.rectTransform.Fill();
                var state = UiKit.Label($"{Clans.StandingName(standing)} · {value}", top, 14, standing <= Standing.Disliked ? UiKit.Danger : standing >= Standing.Friendly ? UiKit.Ok : UiKit.Muted, TextAnchor.MiddleRight);
                state.rectTransform.Fill();
                UiKit.Bar(row, standing <= Standing.Disliked ? UiKit.BarKind.Health : UiKit.BarKind.Essence, (value - Clans.Min) / (float)(Clans.Max - Clans.Min), 4);
            }

            var right = Column(body, 0.56f, 1, 8);
            var picked = Clans.Get(pickedClan) ?? Clans.All[0];
            var rep = session.ReputationOf(picked.Id);
            UiKit.Zh(picked.Seal, right, 64, UiKit.Hex(picked.Colour)).Size(74);
            UiKit.Disp(picked.Name, right, 30, UiKit.Text).Size(38);
            UiKit.Lbl(Clans.StandingName(session.StandingWith(picked.Id)), right, 11, UiKit.LabelGold).Size(16);
            UiKit.Rule(right);
            Para(picked.Description, right, 15);
            Gap(right, 8);
            Fact(right, "Цены в их лавках", $"{(int)Math.Round(Clans.PriceFactor(rep) * 100)}%", Clans.PriceFactor(rep) <= 1 ? UiKit.Ok : UiKit.Danger);
            Fact(right, "Торгуют с вами", Clans.Trades(rep) ? "да" : "нет", Clans.Trades(rep) ? UiKit.Ok : UiKit.Danger);

            if (picked.Guards)
            {
                Fact(right, "Стража", session.Hostile(picked.Id) ? "нападает" : "пропускает", session.Hostile(picked.Id) ? UiKit.Danger : UiKit.Ok);
            }

            if (picked.Rivals.Length > 0)
            {
                var rivals = new List<string>();

                foreach (var rival in picked.Rivals)
                {
                    rivals.Add(Clans.Get(rival)?.Name ?? rival);
                }

                Gap(right, 6);
                Para("Соперники: " + string.Join(", ", rivals) + ". Дружба с ними немного охлаждает эту.", right, 14, UiKit.Muted);
            }
        }

        // ------------------------------------------------------------------ Travel

        void TravelPage()
        {
            var session = S;
            var hero = Game.Hero != null ? Game.Hero.transform.position : Vector3.zero;
            var from = new MapPoint(hero.x, hero.z);
            var body = Frame("灯", "Лунные фонари", "Между зажжёнными фонарями можно переходить", false, null, Purse, new[] { ("Esc", "Остаться") });
            var list = Scroll(Rest(Column(body, 0.15f, 0.85f, 6)));

            foreach (var lantern in session.TravelTargets(from))
            {
                var id = lantern.Id;
                var why = session.WhyNoTravel(from, lantern, Game.Fighting);
                var cost = session.TravelCost(from, lantern);
                var distance = from.DistanceTo(lantern.At);
                var right = why == "Вы и так здесь." ? "здесь" : why != null ? why : cost > 0 ? $"{distance / 1000:0.0} км · {cost} камней" : $"{distance / 1000:0.0} км";
                ListLine(list, "灯", lantern.Name, right, why == null ? UiKit.Text : UiKit.Muted, false, why == null ? () =>
                {
                    if (session.Travel(from, id, Game.Fighting))
                    {
                        Close();
                        Game.Instance.TravelTo(lantern);
                    }
                } : (Action)null, why == null ? UiKit.GoldText : UiKit.Muted);
            }
        }

        // ------------------------------------------------------------------ An inn

        NpcDef innkeeper;

        public void OpenInn(NpcDef keeper)
        {
            innkeeper = keeper;
            Show(Page.Inn);
        }

        void InnPage()
        {
            var session = S;
            var price = 5;
            var body = Frame("宿", innkeeper?.Title ?? "Постоялый двор", innkeeper?.Name ?? "", false, null, Purse, new[] { ("Esc", "Уйти") });
            var middle = Column(body, 0.25f, 0.75f, 10);
            Para($"Сейчас {(int)session.State.Hour}:{(int)(session.State.Hour % 1 * 60):00}, день {session.State.Day}. Кровать — {price} камней. Сон лечит раны, снимает яды и сбрасывает усталость.", middle, 16);
            Gap(middle, 10);
            var can = session.State.Stones >= price;

            void Sleep(Action how)
            {
                if (session.State.Stones < price)
                {
                    return;
                }

                session.State.Stones -= price;
                session.CureAll();
                how();
                Game.Save();
                Close();
            }

            UiKit.HexButton("Спать до утра", middle, () => Sleep(session.SleepTillMorning), can ? UiKit.HexKind.Main : UiKit.HexKind.Off, 13, 52);
            UiKit.HexButton("Вздремнуть 4 часа", middle, () => Sleep(() => session.Sleep(4)), can ? UiKit.HexKind.Ghost : UiKit.HexKind.Off, 12, 44);
            UiKit.HexButton("Проспать до вечера", middle, () => Sleep(() => session.Sleep((int)Mathf.Max(1, (20 - session.State.Hour + 24) % 24))), can ? UiKit.HexKind.Ghost : UiKit.HexKind.Off, 12, 44);

            if (!can)
            {
                Para("Не хватает камней.", middle, 14, UiKit.Danger);
            }

            var hungry = 0;

            foreach (var gu in session.State.Gu)
            {
                hungry += session.IsHungry(gu) ? 1 : 0;
            }

            if (hungry > 0)
            {
                Gap(middle, 10);
                Para($"Голодных Гу: {hungry}. Утром они возьмут корм из сумки, если он есть.", middle, 14, UiKit.Hunger);
            }
        }

        // ------------------------------------------------------------------ Settings of the game

        void SettingsPage()
        {
            var session = S;
            var settings = session.State.Settings;
            var body = Frame("设", "Настройки игры", "Сохраняются вместе с игрой", false, () => Show(Page.Pause), null, new[] { ("Esc", "Назад") });
            var left = Column(body, 0.1f, 0.9f, 14);

            UiKit.Lbl("Сложность", left, 11, UiKit.LabelGold).Size(18);
            var difficulties = (Difficulty[])Enum.GetValues(typeof(Difficulty));
            var names = new List<string>();

            foreach (var difficulty in difficulties)
            {
                names.Add(DifficultyRules.Name(difficulty));
            }

            UiKit.Choices(left, names, Array.IndexOf(difficulties, settings.Difficulty), i => { settings.Difficulty = difficulties[i]; session.Recompute(); Redraw(); });
            Para(DifficultyRules.Text(settings.Difficulty), left, 14, UiKit.Muted);

            UiKit.Lbl("Голод Гу", left, 11, UiKit.LabelGold).Size(18);
            UiKit.Choices(left, new[] { "Нет", "Мягкий", "Как в каноне" }, (int)settings.Hunger, i => { settings.Hunger = (HungerRule)i; session.Recompute(); Redraw(); });
            Para(settings.Hunger == HungerRule.Off ? "Гу не нужно кормить." : settings.Hunger == HungerRule.Soft ? "Голодный Гу слабее на треть и берёт больше эссенции, но не умирает."
                : $"Голодный Гу слабеет, а через {Session.StarveDays} дня после срока умирает. Как у мастеров Гу на самом деле.", left, 14, UiKit.Muted);

            var feed = UiKit.Rect("Feed", left);
            feed.Size(30);
            UiKit.Label("Гу сами едят из сумки на рассвете", feed, 15, UiKit.Text).rectTransform.Fill();
            var toggle = UiKit.Toggle(feed, settings.AutoFeed, on => { settings.AutoFeed = on; Redraw(); });
            toggle.anchorMin = toggle.anchorMax = new Vector2(1, 0.5f);
            toggle.anchoredPosition = new Vector2(-20, 0);

            var travel = UiKit.Rect("Travel", left);
            travel.Size(30);
            UiKit.Label("Переход между фонарями стоит камней", travel, 15, UiKit.Text).rectTransform.Fill();
            var travelToggle = UiKit.Toggle(travel, settings.TravelCosts, on => { settings.TravelCosts = on; Redraw(); });
            travelToggle.anchorMin = travelToggle.anchorMax = new Vector2(1, 0.5f);
            travelToggle.anchoredPosition = new Vector2(-20, 0);
        }
    }
}
