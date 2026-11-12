using System.Collections.Generic;

namespace GuWorld.Core
{
    public enum CodexKind { Lore, Place, Beast, Gu, Herb, Person }

    /// <summary>A page of the codex: the written ones (lore) are found in books and on steles; the rest open by themselves as the hero meets things.</summary>
    public class CodexEntry
    {
        public string Id;
        public CodexKind Kind;
        public string Title;
        public string Text;
    }

    /// <summary>A shrine's blessing: a lasting change for some minutes, once a day.</summary>
    public class BlessingDef
    {
        public string Id;
        public string Name;
        public string Text;
        public StatKind Stat;
        public float Value;
        public float Minutes = 10;
    }

    /// <summary>A deed the game remembers (and shows) once done.</summary>
    public class AchievementDef
    {
        public string Id;
        public string Name;
        public string Text;
        /// <summary>What it counts: "kills", "kills:enemy", "gu" (owned), "fusions", "pills", "lanterns", "codex", "quests", "flag:x", "rep:clan", "stones", "step", "deaths", "bosses".</summary>
        public string Counts;
        public int Goal = 1;
    }

    public static class Codex
    {
        static CodexEntry L(string id, string title, string text) => new CodexEntry { Id = id, Kind = CodexKind.Lore, Title = title, Text = text };

        public static readonly CodexEntry[] Lore =
        {
            L("lore_apertures", "О пяти апертурах",
                "Апертура — колодец в теле мастера. Талант — то, насколько он полон: десятая доля — талант D, четыре десятых — C, шесть — B, восемь — A. Ступени ранга: начальная, средняя, высшая, пиковая. На пиковой ступени мастер может пробиться на следующий ранг, если в нём достаточно прозрения и первобытных камней."),
            L("lore_feeding", "Как кормить Гу",
                "Гу живые. Каждый ест своё: лунные — орхидеи, земляные — каменную крошку, ледяные — лотос, винные — вино. Еда в сумке, и Гу берёт её сам, когда голоден. Голодный Гу слабеет; по старым правилам — улетает или умирает. Смотрители питомников держат корм на месяц вперёд."),
            L("lore_alchemy", "Котёл и огонь",
                "Пилюля — это травы, связанные огнём и эссенцией первобытного камня. Рецепт сложнее умения — котёл чадит, травы сгорают. Умение растёт с каждой сваренной пилюлей. Опытный алхимик иногда получает лишнюю пилюлю из той же горсти трав."),
            L("lore_three_clans", "Хроника трёх кланов",
                "Гу Юэ пришли в долину у подножия Цинмао триста лет назад. Бай — двести, к водопаду за хребтом. Сюн — сто шестьдесят, на переднюю гору, к пасекам. Все трое делили Духовный источник, пока сорок лет назад Гу Юэ не построили над ним зал предков. С тех пор — турниры, стычки и договор, который никто не подписал."),
            L("lore_beasts", "Звери Цинмао",
                "Лисы — мелочь. Серые волки ходят стаями по пять-семь, молниевые — по трое и держатся гребня. Бурые медведи живут в логе на передней горе, орлы — на скалах к востоку от вершины. Змеиное болото на западе кишит гадюками и жабами; пещеры на востоке — пауками. Каждые сто лет в стае рождается король — сотенный, тысячный."),
            L("lore_ancient_master", "Дневник гробокопателя",
                "«День третий. Стелы читаются плохо — письмо старше всех, что я знаю. Огни ходят по кругу и не трогают, если стоять у стелы. День пятый. Врата не открыть без печати. Печать в ларце. Ларец у южной стены. День шестой. Огни стали красными. Я…»"),
            L("lore_paths", "Десять путей",
                "Путей Гу больше, чем звёзд, но на Цинмао знают десяток: Луна и Свет — у Гу Юэ, Лёд и Вода — у Бай, Сила и Земля — у Сюн. Огонь носят бродяги с юга, Яд — болотные одиночки, Кость и Тьма — те, о ком не говорят. Путь Крови — демонический: он сильнее всех, пока не съест хозяина."),
            L("lore_cicada", "Цикада Весны и Осени",
                "Пять строк без подписи: «Гу шестого ранга. Возвращает хозяина в весну его жизни, когда приходит осень. Платит хозяин — всем, что успел. Один раз. Или больше — если хватит духа». Тот, кто написал это, очень хотел, чтобы это забыли. Тот, кто сохранил, — нет."),
            L("lore_cicada_hint", "Гадание бабушки Гуй",
                "«Вижу осень. Потом весну. Потом снова осень. У людей так не бывает». Старуха гадала на корнях Великого древа и сама не поняла, что увидела."),
            L("lore_tomb_1", "Стела у входа", "«Не входи. Если вошёл — не трогай. Если тронул — беги». Под надписью кто-то нацарапал: «Не помогло»."),
            L("lore_tomb_2", "Стела в зале", "Ряды знаков, похожих на цикад. Каждая следующая — меньше. Последняя — точка."),
            L("lore_tomb_3", "Стела у саркофага", "«Здесь лежит тот, кто искал весну после осени. Он нашёл зиму»."),
            L("lore_peak", "Надпись на вершине", "Высечено в скале: «С этой вершины видно три клана. Ни один из них не видит другие два»."),
            L("lore_pass", "Пограничный столб", "Три полосы краски — синяя, белая, охристая. Под краской — бурая, старше всех."),
            L("lore_mine", "Доска штейгера", "Списки: «Чжоу — 4 камня. Ван — 6. Ли — 2 (слаб, отправить к заказчику)». Последняя пометка повторяется часто."),
            L("lore_spiders", "Кости у входа", "Кости оленей, волков и одного человека с поясом мастера Гу. Пояс пуст — Гу ушли от мёртвого. Или их забрали пауки."),
            L("lore_marsh", "Затопленный алтарь", "Каменная змея, свернувшаяся кольцом. В пасти — пустое гнездо для камня. Болотные жабы сидят вокруг, как прихожане."),
            L("lore_eagles", "Гнездо на скале", "Гнездо из веток, костей и одного детского башмачка. Орлы приносят то, что блестит."),
            L("lore_missing", "Слабые ученики", "Сюн Цзянь: ваш старейшина просил отдать ему слабых учеников «на обучение». Сюн отказали — ученики всё равно пропали. Бай Юнь: «Гу не уходят от хозяина. Их отнимают»."),
            L("lore_herbs", "Травы горы",
                "Ледяной лотос — у омута Бай. Горький корень — у дорог. Женьшень и кровяная лоза — у Великого древа. Духовный мох — в пещерах. Красный гриб и тростник — в болоте. Облачный чай — в тумане у перевала. Солнечник — на открытых скалах. Черемша — в Медвежьем логе."),
            L("lore_tree", "Великое древо", "Древо старше кланов. Каждые сто лет оно роняет семя в свои корни — и живёт ещё сто. Сто лет назад семя не проросло."),
        };

        public static readonly BlessingDef[] Blessings =
        {
            new BlessingDef { Id = "blessing_peak", Name = "Благословение вершины", Text = "Гу бьют на десятую часть сильнее.", Stat = StatKind.Power, Value = 0.1f, Minutes = 15 },
            new BlessingDef { Id = "blessing_tree", Name = "Благословение древа", Text = "Здоровье +40.", Stat = StatKind.Health, Value = 40, Minutes = 15 },
            new BlessingDef { Id = "blessing_pass", Name = "Благословение ветра", Text = "Перезарядка короче на десятую часть.", Stat = StatKind.Cooldown, Value = -0.1f, Minutes = 12 },
            new BlessingDef { Id = "blessing_pool", Name = "Благословение омута", Text = "Эссенция восстанавливается на треть быстрее.", Stat = StatKind.EssenceRegen, Value = 0.33f, Minutes = 12 },
        };

        public static readonly AchievementDef[] Achievements =
        {
            new AchievementDef { Id = "first_blood", Name = "Первая кровь", Text = "Победите первого врага.", Counts = "kills", Goal = 1 },
            new AchievementDef { Id = "hunter", Name = "Охотник", Text = "Победите сто врагов.", Counts = "kills", Goal = 100 },
            new AchievementDef { Id = "slayer", Name = "Гроза горы", Text = "Победите тысячу врагов.", Counts = "kills", Goal = 1000 },
            new AchievementDef { Id = "wolfbane", Name = "Волчья погибель", Text = "Победите пятьдесят волков.", Counts = "kills:wolf", Goal = 50 },
            new AchievementDef { Id = "bosses", Name = "Короли падают", Text = "Победите пять боссов.", Counts = "bosses", Goal = 5 },
            new AchievementDef { Id = "first_move", Name = "Больше суммы", Text = "Постигните первый убийственный приём.", Counts = "moves", Goal = 1 },
            new AchievementDef { Id = "many_moves", Name = "Сто путей одного удара", Text = "Постигните десять убийственных приёмов.", Counts = "moves", Goal = 10 },
            new AchievementDef { Id = "move_master", Name = "Мастер приёмов", Text = "Примените убийственные приёмы сто раз.", Counts = "moves_cast", Goal = 100 },
            new AchievementDef { Id = "perfect_gu", Name = "Без изъяна", Text = "Закалите Гу до совершенства.", Counts = "perfect", Goal = 1 },
            new AchievementDef { Id = "rank_four", Name = "Мастер четвёртого ранга", Text = "Прорвитесь на четвёртый ранг.", Counts = "step", Goal = 12 },
            new AchievementDef { Id = "rank_five", Name = "Вершина смертных", Text = "Прорвитесь на пятый ранг.", Counts = "step", Goal = 16 },
            new AchievementDef { Id = "clan_member", Name = "Свой в клане", Text = "Станьте членом клана Гу Юэ по заслугам.", Counts = "clan_rank", Goal = 2 },
            new AchievementDef { Id = "clan_elder", Name = "Старейшина", Text = "Станьте старейшиной клана по заслугам.", Counts = "clan_rank", Goal = 4 },
            new AchievementDef { Id = "tide", Name = "Стена против прилива", Text = "Отбейте волчий прилив.", Counts = "tides", Goal = 1 },
            new AchievementDef { Id = "tides", Name = "Сторож деревни", Text = "Отбейте пять волчьих приливов.", Counts = "tides", Goal = 5 },
            new AchievementDef { Id = "homeowner", Name = "Свой угол", Text = "Купите дом.", Counts = "homes", Goal = 1 },
            new AchievementDef { Id = "parry", Name = "В последний миг", Text = "Отразите двадцать ударов, поставив блок в последний миг.", Counts = "parries", Goal = 20 },
            new AchievementDef { Id = "elite_hunter", Name = "Охотник на сильных", Text = "Одолейте десять элитных врагов.", Counts = "elites", Goal = 10 },
            new AchievementDef { Id = "elite_bane", Name = "Гроза элиты", Text = "Одолейте сто элитных врагов.", Counts = "elites", Goal = 100 },
            new AchievementDef { Id = "path_master", Name = "Мастер пути", Text = "Достигните мастерства на каком-нибудь пути.", Counts = "grade", Goal = 2 },
            new AchievementDef { Id = "path_grandmaster", Name = "Великий мастер", Text = "Станьте великим мастером пути.", Counts = "grade", Goal = 4 },
            new AchievementDef { Id = "spring_after_autumn", Name = "Весна после осени", Text = "Вернитесь из смерти с Цикадой Весны и Осени.", Counts = "revived", Goal = 1 },
            new AchievementDef { Id = "all_bosses", Name = "Тот, кто прошёл гору", Text = "Победите пятнадцать боссов.", Counts = "bosses", Goal = 15 },
            new AchievementDef { Id = "collector", Name = "Собиратель", Text = "Владейте двадцатью Гу одновременно.", Counts = "gu", Goal = 20 },
            new AchievementDef { Id = "fuser", Name = "Мастер слияния", Text = "Создайте десять Гу слиянием.", Counts = "fusions", Goal = 10 },
            new AchievementDef { Id = "alchemist", Name = "Алхимик", Text = "Сварите пятьдесят пилюль.", Counts = "pills", Goal = 50 },
            new AchievementDef { Id = "traveller", Name = "Странник", Text = "Зажгите двадцать фонарей.", Counts = "lanterns", Goal = 20 },
            new AchievementDef { Id = "scholar", Name = "Книжник", Text = "Откройте тридцать страниц кодекса.", Counts = "codex", Goal = 30 },
            new AchievementDef { Id = "helper", Name = "Добрая душа", Text = "Выполните двадцать побочных поручений.", Counts = "quests", Goal = 20 },
            new AchievementDef { Id = "bai_kin", Name = "Белая кровь", Text = "Станьте своим для клана Бай.", Counts = "rep:bai", Goal = 80 },
            new AchievementDef { Id = "xiong_kin", Name = "Медвежья лапа", Text = "Станьте своим для клана Сюн.", Counts = "rep:xiong", Goal = 80 },
            new AchievementDef { Id = "mortal_kin", Name = "Заступник", Text = "Станьте своим для смертных хуторов.", Counts = "rep:mortal", Goal = 80 },
            new AchievementDef { Id = "rich", Name = "Тысяча камней", Text = "Скопите тысячу первобытных камней.", Counts = "stones", Goal = 1000 },
            new AchievementDef { Id = "rank_three", Name = "Третий ранг", Text = "Достигните третьего ранга.", Counts = "step", Goal = 8 },
            new AchievementDef { Id = "peak", Name = "Над облаками", Text = "Помолитесь на вершине Цинмао.", Counts = "flag:peak_blessed" },
            new AchievementDef { Id = "treaty", Name = "Мир у источника", Text = "Подпишите Договор о водах.", Counts = "flag:treaty_signed" },
            new AchievementDef { Id = "tree", Name = "Сто лет древу", Text = "Спасите Великое древо.", Counts = "flag:tree_healed" },
            new AchievementDef { Id = "stubborn", Name = "Упрямец", Text = "Падите десять раз — и встаньте.", Counts = "deaths", Goal = 10 },
            new AchievementDef { Id = "cicada", Name = "Весна после осени", Text = "Прочтите о Цикаде Весны и Осени.", Counts = "flag:codex_lore_cicada" },
        };

        static Dictionary<string, CodexEntry> lore;
        static Dictionary<string, BlessingDef> blessings;

        public static CodexEntry LoreEntry(string id)
        {
            lore = lore ?? Lists.Index(Lore, e => e.Id);

            return id != null && lore.TryGetValue(id, out var found) ? found : null;
        }

        public static BlessingDef Blessing(string id)
        {
            blessings = blessings ?? Lists.Index(Blessings, e => e.Id);

            return id != null && blessings.TryGetValue(id, out var found) ? found : null;
        }

        /// <summary>
        /// Every page the codex can hold, the written ones and those made from the books of the game:
        /// a page for each place, beast, Gu, herb and person (ids "place:x", "beast:x", "gu:x", "herb:x", "person:x").
        /// </summary>
        public static IEnumerable<CodexEntry> Everything()
        {
            foreach (var entry in Lore)
            {
                yield return entry;
            }

            foreach (var place in Atlas.Places)
            {
                yield return new CodexEntry { Id = "place:" + place.Id, Kind = CodexKind.Place, Title = place.Name, Text = "" };
            }

            foreach (var settlement in Regions.Settlements)
            {
                yield return new CodexEntry { Id = "place:" + settlement.Id, Kind = CodexKind.Place, Title = settlement.Name, Text = Clans.Get(settlement.Clan)?.Name ?? "" };
            }

            foreach (var enemy in Bestiary.All)
            {
                if (enemy.Id != "dummy")
                {
                    yield return new CodexEntry { Id = "beast:" + enemy.Id, Kind = CodexKind.Beast, Title = enemy.Name, Text = enemy.Title ?? "" };
                }
            }

            foreach (var gu in GuBook.All)
            {
                yield return new CodexEntry { Id = "gu:" + gu.Id, Kind = CodexKind.Gu, Title = gu.Name, Text = gu.Description };
            }

            foreach (var item in ItemBook.All)
            {
                if (item.Kind == ItemKind.Herb)
                {
                    yield return new CodexEntry { Id = "herb:" + item.Id, Kind = CodexKind.Herb, Title = item.Name, Text = item.Description };
                }
            }

            foreach (var npc in Atlas.AllNpcs)
            {
                yield return new CodexEntry { Id = "person:" + npc.Id, Kind = CodexKind.Person, Title = npc.Name, Text = npc.Title ?? "" };
            }
        }
    }
}
