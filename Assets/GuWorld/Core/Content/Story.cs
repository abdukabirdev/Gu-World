using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// The story: five chapters of main quests that follow one another, the named side quests
    /// and the board's errands. In a line, Who is an NPC's id, "hero", or "" for the narrator.
    /// </summary>
    public static class Story
    {
        public const string First = "m01";

        public static readonly string[] ChapterNames = { "", "Пробуждение", "Долг клана", "Три клана", "Волчий прилив", "Лунная тень" };

        static Line L(string who, string text) => new Line(who, text);

        public static readonly QuestDef[] All =
        {
            // ---------------------------------------------------------------- Chapter I
            new QuestDef
            {
                Id = "m01", Title = "Церемония пробуждения", Chapter = 1, Main = true, Next = "m02",
                Summary = "Сегодня вам пятнадцать. Старейшина академии ждёт у зала предков, чтобы открыть вашу апертуру.",
                Steps = new[]
                {
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "elder", Text = "Подойдите к старейшине Чанмину у зала предков",
                        Lines = new[]
                        {
                            L("elder", "Лань. Последний в списке, как всегда. Подойди, положи ладонь на камень."),
                            L("", "Камень предков холоден. Потом под рёбрами что-то раскрывается, как глаз, и в нём плещется свет — меньше половины чаши."),
                            L("elder", "Сорок четыре доли. Талант C. Старейшины уже отвернулись, ты видел? Не смотри на них. Смотри на меня."),
                            L("elder", "Я учу здесь дольше, чем живут их деды. Колодец мелок — значит, будешь носить воду чаще других. Иди в питомник, Синь даст тебе первого Гу."),
                        }
                    },
                },
                Reward = new Reward { Insight = 20 }
            },
            new QuestDef
            {
                Id = "m02", Title = "Первый Гу", Chapter = 1, Main = true, Next = "m03",
                Summary = "Каждый ученик Гу Юэ начинает с Гу Лунного света. Его надо подчинить волей и посадить в апертуру.",
                Steps = new[]
                {
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "keeper", Text = "Получите Гу Лунного света у смотрительницы Синь", Gives = new[] { "moonlight" },
                        Lines = new[]
                        {
                            L("keeper", "С талантом C? Ну, Гу талант не нюхает. Держи. Он дикий: пока не очистишь — не слушается."),
                            L("keeper", "Открой апертуру — клавиша K. Выбери его, удерживай «Очистить», пока твоя эссенция не вытеснит его волю. Потом посади в слот."),
                        }
                    },
                    new StepDef { Kind = StepKind.Equip, Target = "moonlight", Text = "Откройте апертуру (K), очистите Гу Лунного света и поставьте его в слот" },
                },
                Reward = new Reward { Insight = 20, Stones = 5 }
            },
            new QuestDef
            {
                Id = "m03", Title = "Лунный клинок", Chapter = 1, Main = true, Next = "m04",
                Summary = "Наставник боя Тэ ждёт на дворе академии. Там манекены — на них и учатся.",
                Steps = new[]
                {
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "instructor", Text = "Найдите наставника Тэ на дворе академии",
                        Lines = new[]
                        {
                            L("instructor", "А, колодец на сорок четыре. Ничего. Я видел талант A, который умер в первую же охоту — стоял и бросал клинки, пока не опустел."),
                            L("instructor", "Запомни круг. Ладонь — ЛКМ — копит эссенцию. Гу на Q её тратит. Пробел — перекат, в нём тебя не задеть. Разбей три манекена."),
                        }
                    },
                    new StepDef { Kind = StepKind.Kill, Target = "dummy", Count = 3, Text = "Разбейте манекены: ладонь (ЛКМ) и лунный клинок (Q)", Marker = "yard" },
                },
                Reward = new Reward { Insight = 30, Stones = 5, Items = new[] { ("heal_pill", 2) } }
            },
            new QuestDef
            {
                Id = "m04", Title = "Первая охота", Chapter = 1, Main = true, Next = "m05",
                Summary = "В бамбуковой роще на западе расплодились горные лисы. Наставнику нужны три шкуры — и чтобы вы вернулись целым.",
                Steps = new[]
                {
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "instructor", Text = "Поговорите с наставником Тэ",
                        Lines = new[]
                        {
                            L("instructor", "Манекен сдачи не даёт. Иди в рощу на западе, по дороге мимо академии. Лис кусает и отскакивает — лови его на отскоке."),
                            L("instructor", "У дороги стоят лунные фонари. Коснись — и если ляжешь, очнёшься у него. Принеси три шкуры."),
                        }
                    },
                    new StepDef { Kind = StepKind.Kill, Target = "fox", Count = 5, Text = "Убейте горных лисов в бамбуковой роще", Marker = "grove" },
                    new StepDef { Kind = StepKind.Have, Target = "fox_pelt", Count = 3, Text = "Соберите три лисьи шкуры", Marker = "grove", Takes = true },
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "instructor", Text = "Вернитесь к наставнику Тэ",
                        Lines = new[]
                        {
                            L("instructor", "Живой. И шкуры целые. Хм."),
                            L("instructor", "Старейшина велел прислать тебя к Духовному источнику. Обычно туда пускают со второго года. Не знаю, что он в тебе нашёл."),
                        }
                    },
                },
                Reward = new Reward { Insight = 60, Stones = 20, Items = new[] { ("heal_pill", 3) } }
            },
            new QuestDef
            {
                Id = "m05", Title = "Первая ступень", Chapter = 1, Main = true, Next = "m06",
                Summary = "У Духовного источника эссенция густеет. Первобытные камни там превращаются в прорыв.",
                Steps = new[]
                {
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "elder", Text = "Поговорите со старейшиной Чанмином",
                        Lines = new[]
                        {
                            L("elder", "Источник слева от зала предков. Сядь у воды, раскрой апертуру, сожги камень — и стенки колодца станут твёрже."),
                            L("elder", "Победы и поручения тоже дают прорыв. Но камни — быстрее. Клан даёт их тем, кто полезен. Вот твои первые."),
                        },
                        Gives = new[] { "stones:15" }
                    },
                    new StepDef { Kind = StepKind.Cultivate, Count = 2, Text = "Медитируйте у Духовного источника (T): сожгите камни и дойдите до высшей ступени", Marker = "spring" },
                },
                Reward = new Reward { Insight = 0, Stones = 10, Flags = new[] { "ch2" } }
            },

            // ---------------------------------------------------------------- Chapter II
            new QuestDef
            {
                Id = "m06", Title = "Травы для лекарки", Chapter = 2, Main = true, Next = "m07",
                Summary = "Лекарке Яо нужны лунные орхидеи с опушки леса Цинмао. Раньше их носил дозорный Тянь.",
                Steps = new[]
                {
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "herbalist", Text = "Зайдите к лекарке Яо",
                        Lines = new[]
                        {
                            L("herbalist", "Ты от Чанмина? Хорошо. Мне нужно пять лунных орхидей. Растут у опушки, за северными воротами."),
                            L("herbalist", "Раньше носил Тянь, дозорный. Третий день его нет. И волки стали подходить к самой опушке. Смотри в оба."),
                        }
                    },
                    new StepDef { Kind = StepKind.Have, Target = "moon_orchid", Count = 5, Text = "Соберите пять лунных орхидей на опушке", Marker = "forest_edge", Takes = true },
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "herbalist", Text = "Отнесите орхидеи лекарке",
                        Lines = new[]
                        {
                            L("herbalist", "Свежие. Возьми Гу Травы жизни — пригодится больше, чем мне. И зайди к Цзяну на ворота: он спрашивал, кто ходил к опушке."),
                        }
                    },
                },
                Reward = new Reward { Insight = 80, Stones = 25, Gu = new[] { "life_grass" } }
            },
            new QuestDef
            {
                Id = "m07", Title = "Серые тени", Chapter = 2, Main = true, Next = "m08",
                Summary = "Стая серых волков держит опушку. Ведёт её вожак с рваным ухом — его логово на холме в глубине леса.",
                Steps = new[]
                {
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "captain", Text = "Поговорите с начальником стражи Цзяном у северных ворот",
                        Lines = new[]
                        {
                            L("captain", "Ты ходил к опушке и вернулся. Значит, годишься. Стая серых режет наших охотников. Убей шестерых — остальные присмиреют."),
                            L("captain", "А вожак сидит на холме, к северо-востоку от тропы. Рваное Ухо. Воет — и к нему сбегаются. Сначала бей тех, кто прибежал."),
                        }
                    },
                    new StepDef { Kind = StepKind.Kill, Target = "wolf", Count = 6, Text = "Убейте серых волков в лесу Цинмао", Marker = "forest" },
                    new StepDef { Kind = StepKind.Encounter, Target = "ragged_ear", Text = "Убейте вожака Рваное Ухо на Волчьем холме", Marker = "knoll" },
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "captain", Text = "Вернитесь к Цзяну",
                        Lines = new[]
                        {
                            L("captain", "Рваное Ухо? Один? С талантом C? …Ладно. Молчу."),
                            L("captain", "Тяня так и нет. Он ушёл к речному броду, на восток. Жена его, Лин, каждый вечер стоит у ворот. Сходи, а?"),
                        }
                    },
                },
                Reward = new Reward { Insight = 150, Stones = 40 }
            },
            new QuestDef
            {
                Id = "m08", Title = "Пропавший дозорный", Chapter = 2, Main = true, Next = "m09",
                Summary = "Дозорный Гу Юэ Тянь ушёл к реке три дня назад. Дорога на восток ведёт к броду, а выше по течению шумит водопад.",
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Reach, Target = "river", Text = "Дойдите до речного брода на востоке" },
                    new StepDef { Kind = StepKind.Use, Target = "scout_body", Text = "Осмотрите берег выше брода, у водопада", Gives = new[] { "scout_token" } },
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "elder", Text = "Расскажите старейшине Чанмину",
                        Lines = new[]
                        {
                            L("hero", "Тянь мёртв. Лежит у водопада. Ран нет — он будто высох. А жетон расколот, и половинка тянется к воде."),
                            L("elder", "…Высох. Бедняга. Волки, должно быть. Волки и жажда."),
                            L("elder", "К водопаду больше не ходи, Лань. Там старая печать, она не для учеников. Слышишь? Не ходи. Жетон оставь себе — отдашь вдове, когда придёт время."),
                            L("", "Он улыбается, как всегда. Но жетон в вашей ладони дрожит и тянет на восток."),
                        }
                    },
                    new StepDef { Kind = StepKind.Cultivate, Count = 3, Text = "Достигните пиковой ступени: печать не откроется слабому", Marker = "spring" },
                },
                Reward = new Reward { Insight = 100, Flags = new[] { "hermit_open" } }
            },
            new QuestDef
            {
                Id = "m09", Title = "За водопадом", Chapter = 2, Main = true, Next = "m10",
                Summary = "Жетон дозорного открывает лунную печать за водопадом. Старейшина запретил туда ходить.",
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Reach, Target = "hermit_cave", Text = "Войдите в проход за водопадом", Marker = "door_hermit" },
                    new StepDef { Kind = StepKind.Kill, Target = "cave_gu", Count = 8, Text = "Пробейтесь через рой диких Гу", Marker = "hermit_cave" },
                    new StepDef { Kind = StepKind.Encounter, Target = "stone_guard", Text = "Одолейте стража наследия в глубине пещеры", Marker = "stone_guard" },
                },
                Reward = new Reward { Insight = 180, Stones = 30 }
            },
            new QuestDef
            {
                Id = "m10", Title = "Наследие Отшельника", Chapter = 2, Main = true, Next = "m11",
                Summary = "В глубине пещеры — останки человека в одежде Гу Юэ и свиток, придавленный винной чашей.",
                Steps = new[]
                {
                    new StepDef
                    {
                        Kind = StepKind.Use, Target = "hermit_remains", Text = "Осмотрите останки", Gives = new[] { "hermit_will", "liquor_worm" },
                        Lines = new[]
                        {
                            L("", "«Тому, кого клан счёл слабым. Значит, ты ещё жив. Я — Гу Юэ Шэнь. Меня звали Лунным Отшельником и изгнали за клевету на старейшин»."),
                            L("", "«Это не клевета. Волчьи приливы не приходят сами — их зовут. Под залом предков кормят кровью Гу, которому двести лет. Источник мелеет не от старости: его пьют»."),
                            L("", "«Ученики со слабым талантом пропадают на охоте чаще других. Посчитай сам. Никому из старейшин не верь. Червя забери — он честнее людей»."),
                            L("", "В чаше шевелится Винный червь. А ваша апертура, глотнув здешнего воздуха, раздаётся вширь."),
                        }
                    },
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "widow", Text = "Отдайте жетон жене дозорного",
                        Lines = new[]
                        {
                            L("widow", "Его жетон. …Он говорил, что нашёл что-то у водопада. Что пойдёт к старейшине Чанмину и всё расскажет. Он так радовался."),
                            L("widow", "Он ведь рассказал? Старейшина знает?"),
                            L("hero", "…Знает."),
                        }
                    },
                },
                Reward = new Reward { Insight = 250, Stones = 60, Talent = 0.62f, Items = new[] { ("scroll_moonglow", 1) }, Flags = new[] { "ch3" } }
            },

            // ---------------------------------------------------------------- Chapter III
            new QuestDef
            {
                Id = "m11", Title = "Второй ранг", Chapter = 3, Main = true, Next = "m12",
                Summary = "Чтобы спорить со старейшинами, мало знать правду. Пройдите ступени первого ранга и прорвитесь на второй у Духовного источника.",
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Cultivate, Count = 4, Text = "Прорвитесь на второй ранг у Духовного источника (нужно 50 камней)", Marker = "spring" },
                },
                Reward = new Reward { Stones = 30 }
            },
            new QuestDef
            {
                Id = "m12", Title = "Разбойники на тракте", Chapter = 3, Main = true, Next = "m13",
                Summary = "Южный тракт закрыт: обозы с первобытными камнями не доходят до деревни. Стан разбойников — за южными воротами.",
                Steps = new[]
                {
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "captain", Text = "Поговорите с Цзяном",
                        Lines = new[]
                        {
                            L("captain", "Второй ранг. В твои годы. Старейшины теперь здороваются, а?"),
                            L("captain", "Слушай. На южном тракте стан. Демонические Гу-мастера, беглые. Третий обоз с камнями пропал. И вот что странно: они всегда знают, когда обоз выходит."),
                            L("captain", "Атамана зовут Кривой Клык. Когда он каменеет — не бей, жди. Принеси мне всё, что найдёшь при нём."),
                        }
                    },
                    new StepDef { Kind = StepKind.Kill, Target = "bandit*", Count = 6, Text = "Перебейте разбойников в стане", Marker = "camp" },
                    new StepDef { Kind = StepKind.Encounter, Target = "crooked_fang", Text = "Убейте атамана Кривого Клыка", Marker = "crooked_fang" },
                    new StepDef { Kind = StepKind.Have, Target = "unsealed_letter", Text = "Обыщите атамана", Marker = "crooked_fang" },
                },
                Reward = new Reward { Insight = 300, Stones = 60 }
            },
            new QuestDef
            {
                Id = "m13", Title = "Письмо без печати", Chapter = 3, Main = true, Next = "m14",
                Summary = "«Сорок камней за луну, пока тракт закрыт. Учеников со слабой апертурой не трогать — они нужны». Почерк ровный, учительский.",
                Steps = new[]
                {
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "instructor", Text = "Покажите письмо наставнику Тэ, а не старейшинам",
                        Lines = new[]
                        {
                            L("instructor", "Убери. Убери с глаз. …Дай сюда."),
                            L("instructor", "«Со слабой апертурой не трогать». Я двадцать лет вожу учеников на охоту. Слабые пропадают. Я думал — потому что слабые."),
                            L("instructor", "Один почерк я знаю. Но одного письма мало, чтобы поднять руку на старейшину. Нужны свидетели из других кланов. Скоро турнир у пограничного камня. Выиграй — и тебя станут слушать."),
                        }
                    },
                },
                Reward = new Reward { Insight = 150 }
            },
            new QuestDef
            {
                Id = "m14", Title = "Турнир трёх кланов", Chapter = 3, Main = true, Next = "m15",
                Summary = "Раз в год Гу Юэ, Сюн и Бай меряются учениками у пограничного камня на юго-востоке. От Гу Юэ в этом году некому выйти.",
                Steps = new[]
                {
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "judge", Text = "Заявитесь у распорядителя турнира у пограничного камня",
                        Lines = new[]
                        {
                            L("judge", "От Гу Юэ? Ваш старейшина сказал, что в этом году клан не выставит никого. Что ученики слабы."),
                            L("judge", "Что ж, один нашёлся. Два боя. Сюн Ли прёт, как медведь, — не стой у него на пути. Бай Нин бьёт издали — не давай ему дистанцию. Войди в круг, когда будешь готов."),
                        }
                    },
                    new StepDef { Kind = StepKind.Encounter, Target = "tournament", Text = "Победите бойцов Сюн и Бай", Marker = "border_stone" },
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "judge", Text = "Поговорите с распорядителем",
                        Lines = new[]
                        {
                            L("judge", "Победил Гу Юэ. Запишу — хотя ваш старейшина будет недоволен. Проигравшие по обычаю дарят победителю Гу. Бери оба."),
                            L("judge", "И ещё, мальчик. Сюн и Бай тоже теряют людей на Волчьем гребне. Там на камнях знаки, которых раньше не было. Если будешь рядом — взгляни."),
                        }
                    },
                },
                Reward = new Reward { Insight = 300, Stones = 100, Gu = new[] { "bear_strength", "ice_spike" } }
            },
            new QuestDef
            {
                Id = "m15", Title = "Знаки на камнях", Chapter = 3, Main = true, Next = "m16",
                Summary = "На Волчьем гребне, далеко за лесом, кто-то поставил три алтаря. Молниевые волки ходят вокруг них, как привязанные.",
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Use, Target = "blood_altar*", Count = 3, Text = "Разбейте три кровавых алтаря на Волчьем гребне", Marker = "ridge" },
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "captain", Text = "Предупредите Цзяна",
                        Lines = new[]
                        {
                            L("hero", "Три алтаря. В каждом кровь и воск. Волки не уходят от них, даже когда их бьёшь."),
                            L("captain", "Приманка. Кто-то созывал стаю со всей горы. Ты разбил алтари — и теперь им незачем там стоять."),
                            L("captain", "Они пойдут вниз. Сюда. Отдохни, если успеешь."),
                        }
                    },
                },
                Reward = new Reward { Insight = 350, Stones = 80, Flags = new[] { "ch4" } }
            },

            // ---------------------------------------------------------------- Chapter IV
            new QuestDef
            {
                Id = "m16", Title = "Набат", Chapter = 4, Main = true, Next = "m17",
                Summary = "Набат на северных воротах. Волчий прилив.",
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Encounter, Target = "tide", Text = "Отбейте три волны у северных ворот", Marker = "tide" },
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "captain", Text = "Поговорите с Цзяном",
                        Lines = new[]
                        {
                            L("captain", "Сотенный вожак. Мёртвый. У моих ворот. Я расскажу об этом внукам, если будут внуки."),
                            L("captain", "Но сотенные не водят прилив. Их самих гонит тот, кто выше. На гребне, за алтарями, логово. Там тысячный король — Громовая Корона."),
                            L("captain", "Старейшины заперлись в зале совета. Чанмина никто не видел с утра. Идти некому, Лань. Кроме тебя."),
                        }
                    },
                },
                Reward = new Reward { Insight = 400, Stones = 120, Items = new[] { ("scroll_golden_moon", 1) } }
            },
            new QuestDef
            {
                Id = "m17", Title = "Громовая Корона", Chapter = 4, Main = true, Next = "m18",
                Summary = "Логово тысячного короля — на вершине Волчьего гребня. Пока он жив, прилив вернётся.",
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Encounter, Target = "den", Text = "Убейте Громовую Корону в логове на вершине гребня", Marker = "den" },
                    new StepDef
                    {
                        Kind = StepKind.Use, Target = "den_lure", Text = "Осмотрите логово", Gives = new[] { "blood_lure" },
                        Lines = new[]
                        {
                            L("", "В глубине логова, под костями, — восковой шар с мёртвым Гу внутри. От него тянет кровью так, что сводит зубы."),
                            L("", "На воске оттиск. Полумесяц в круге — печать академии Гу Юэ. Такая одна. Вы видели её каждый день, на поясе у старейшины."),
                            L("", "Сердцевина Короны в вашей руке ещё гремит. Апертура отзывается и раздаётся снова."),
                        }
                    },
                },
                Reward = new Reward { Insight = 500, Stones = 100, Talent = 0.82f }
            },
            new QuestDef
            {
                Id = "m18", Title = "Кровавый след", Chapter = 4, Main = true, Next = "m19",
                Summary = "Приманка с печатью академии. Письмо учительским почерком. Высохший дозорный. Наставник Тэ должен это увидеть.",
                Steps = new[]
                {
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "instructor", Text = "Покажите приманку наставнику Тэ",
                        Lines = new[]
                        {
                            L("instructor", "Его печать. Его почерк. Его ученики."),
                            L("instructor", "Он был старейшиной при трёх главах клана. Мой дед звал его «старик Чанмин». Никто не спрашивал, сколько ему лет."),
                            L("instructor", "Его нет в деревне. Плита за залом предков сдвинута и заперта изнутри. Она откроется третьему рангу — так он сам говорил, когда учил меня."),
                            L("instructor", "Я не дорос. Ты — дорастёшь. Иди к источнику, пока в нём ещё есть вода."),
                        }
                    },
                },
                Reward = new Reward { Insight = 200, Flags = new[] { "elder_gone", "ch5" } }
            },

            // ---------------------------------------------------------------- Chapter V
            new QuestDef
            {
                Id = "m19", Title = "Третий ранг", Chapter = 5, Main = true, Next = "m20",
                Summary = "Плита за залом предков откроется только третьему рангу. Источник мелеет с каждым днём.",
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Cultivate, Count = 8, Text = "Прорвитесь на третий ранг у Духовного источника (нужно 200 камней)", Marker = "spring" },
                },
                Reward = new Reward { Stones = 50, Flags = new[] { "blood_open" } }
            },
            new QuestDef
            {
                Id = "m20", Title = "Под залом предков", Chapter = 5, Main = true, Next = "m21",
                Summary = "Лестница за плитой уходит вниз, туда, где двести лет никто не считал пропавших учеников.",
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Reach, Target = "blood_cave", Text = "Спуститесь под зал предков", Marker = "door_blood" },
                    new StepDef { Kind = StepKind.Kill, Target = "blood_*", Count = 6, Text = "Пробейтесь через марионеток и кровавых волков", Marker = "blood_cave" },
                    new StepDef { Kind = StepKind.Encounter, Target = "elder", Text = "Старейшина ждёт у чаши", Marker = "elder" },
                },
                Reward = new Reward { Insight = 600, Stones = 200 }
            },
            new QuestDef
            {
                Id = "m21", Title = "Лунная тень", Chapter = 5, Main = true,
                Summary = "Старейшина мёртв. В чаше под залом предков пульсирует Гу Кровавой луны — двести лет чужих жизней.",
                Steps = new[]
                {
                    new StepDef
                    {
                        Kind = StepKind.Use, Target = "blood_gu", Text = "Подойдите к чаше",
                        Lines = new[]
                        {
                            L("", "Гу Кровавой луны поворачивается к вам, как цветок к солнцу. Он голоден. Он узнал ваш талант — тот самый, «которого не жалко»."),
                            L("", "Раздавить его — и всё, что он выпил, вернётся в источник. Взять — и ни один старейшина больше не посмотрит на вас сверху вниз."),
                        }
                    },
                    new StepDef
                    {
                        Kind = StepKind.Choose, Target = "ending", Text = "Решите судьбу Гу Кровавой луны", Marker = "blood_gu",
                        Choices = new[] { ("Раздавить. Пусть источник оживёт.", "ending_righteous"), ("Взять. Колодец больше не будет мелким.", "ending_demonic") }
                    },
                },
                Reward = new Reward { Insight = 800, Flags = new[] { "finished" } }
            },

            // ---------------------------------------------------------------- Side quests
            new QuestDef
            {
                Id = "s_hua", Title = "Лапша тётушки Хуа", Chapter = 1, Giver = "hua", Needs = "m03_done",
                Summary = "Тётушке Хуа нужны побеги бамбука из западной рощи.",
                Offer = new[]
                {
                    L("hua", "Ты ведь в рощу ходишь? Принеси четыре побега бамбука. Молодых, светлых. А я тебя накормлю так, что раны сами затянутся."),
                },
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Have, Target = "bamboo_shoot", Count = 4, Text = "Соберите четыре побега бамбука в роще", Marker = "grove", Takes = true },
                    new StepDef { Kind = StepKind.Talk, Target = "hua", Text = "Отнесите побеги тётушке Хуа", Lines = new[] { L("hua", "Вот это побеги! Садись. Ешь. И с собой возьми — в дороге пригодится.") } },
                },
                Reward = new Reward { Insight = 40, Stones = 15, Items = new[] { ("heal_pill", 4) } }
            },
            new QuestDef
            {
                Id = "s_smith", Title = "Клыки для стражи", Chapter = 2, Giver = "smith", Needs = "m06_done",
                Summary = "Кузнецу Гану нужны волчьи клыки на наконечники для стражи.",
                Offer = new[]
                {
                    L("smith", "Страже нужны копья, а копьям — наконечники. Волчий клык твёрже моего железа. Принеси шесть — отдам Гу, что остался от отца. Мне он ни к чему: апертуру мне так и не открыли."),
                },
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Have, Target = "wolf_fang", Count = 6, Text = "Соберите шесть волчьих клыков", Marker = "forest", Takes = true },
                    new StepDef { Kind = StepKind.Talk, Target = "smith", Text = "Отнесите клыки кузнецу", Lines = new[] { L("smith", "Добрые клыки. Держи жука. Отец говорил: бей им о землю, когда обступят.") } },
                },
                Reward = new Reward { Insight = 80, Stones = 40, Gu = new[] { "stone_beetle" } }
            },
            new QuestDef
            {
                Id = "s_gambler", Title = "Должок", Chapter = 3, Giver = "gambler", Needs = "m11_done",
                Summary = "Разбойники-метатели обчистили Старого Цяня на тракте. Он хочет, чтобы им это запомнилось.",
                Offer = new[]
                {
                    L("gambler", "Трое с ледяными шипами остановили меня на тракте и забрали мешок самородков. Мой мешок! Проучи троих метателей — и я поделюсь тем, что они не успели расколоть."),
                },
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Kill, Target = "bandit_thrower", Count = 3, Text = "Убейте трёх разбойников-метателей", Marker = "camp" },
                    new StepDef { Kind = StepKind.Talk, Target = "gambler", Text = "Вернитесь к Старому Цяню", Lines = new[] { L("gambler", "Вот это я понимаю — возврат долга. Держи два камушка с пятном. В одном точно что-то есть. Наверное.") } },
                },
                Reward = new Reward { Insight = 100, Items = new[] { ("rock_2", 2) } }
            },
            new QuestDef
            {
                Id = "s_widow", Title = "Цветы для Тяня", Chapter = 3, Giver = "widow", Needs = "m10_done",
                Summary = "Лин просит отнести лунные орхидеи туда, где лежит её муж.",
                Offer = new[]
                {
                    L("widow", "Мне нельзя за ворота. Отнеси ему орхидеи, три. Он носил их лекарке и каждый раз одну оставлял мне."),
                },
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Have, Target = "moon_orchid", Count = 3, Text = "Соберите три лунные орхидеи", Marker = "forest_edge" },
                    new StepDef { Kind = StepKind.Use, Target = "scout_body", Text = "Положите орхидеи у водопада", Lines = new[] { L("", "Вы кладёте цветы на камни. В сумерках они светятся, и кажется, что у воды кто-то стоит.") } },
                    new StepDef { Kind = StepKind.Talk, Target = "widow", Text = "Вернитесь к Лин", Lines = new[] { L("widow", "Спасибо. Это его Гу — Малый свет. Он светил им мне в окно, когда возвращался. Пусть теперь светит тебе.") } },
                },
                Reward = new Reward { Insight = 120, Gu = new[] { "little_light" } }
            },

            // ---------------------------------------------------------------- The board
            new QuestDef
            {
                Id = "b_foxes", Title = "Поручение: лисы в роще", Chapter = 1, Giver = "scribe", Needs = "m04_done", Repeatable = true,
                Summary = "Клан платит за каждых восемь горных лисов.",
                Offer = new[] { L("scribe", "Лисы портят бамбук. Восемь хвостов — двадцать камней.") },
                Steps = new[] { new StepDef { Kind = StepKind.Kill, Target = "fox", Count = 8, Text = "Убейте восемь горных лисов", Marker = "grove" } },
                Reward = new Reward { Insight = 30, Stones = 20 }
            },
            new QuestDef
            {
                Id = "b_orchids", Title = "Поручение: орхидеи", Chapter = 2, Giver = "scribe", Needs = "m06_done", Repeatable = true,
                Summary = "Питомнику нужны лунные орхидеи на корм Гу.",
                Offer = new[] { L("scribe", "Питомник просит шесть лунных орхидей. Тридцать пять камней.") },
                Steps = new[] { new StepDef { Kind = StepKind.Have, Target = "moon_orchid", Count = 6, Text = "Соберите шесть лунных орхидей", Marker = "forest_edge", Takes = true } },
                Reward = new Reward { Insight = 40, Stones = 35 }
            },
            new QuestDef
            {
                Id = "b_wolves", Title = "Поручение: волки у опушки", Chapter = 2, Giver = "scribe", Needs = "m07_done", Repeatable = true,
                Summary = "Стража платит за каждых восемь серых волков.",
                Offer = new[] { L("scribe", "Серые опять у опушки. Восемь голов — сорок пять камней.") },
                Steps = new[] { new StepDef { Kind = StepKind.Kill, Target = "wolf", Count = 8, Text = "Убейте восемь серых волков", Marker = "forest" } },
                Reward = new Reward { Insight = 70, Stones = 45 }
            },
            new QuestDef
            {
                Id = "b_bandits", Title = "Поручение: тракт", Chapter = 3, Giver = "scribe", Needs = "m12_done", Repeatable = true,
                Summary = "Купцы платят за спокойный тракт.",
                Offer = new[] { L("scribe", "Купцы скинулись. Шесть разбойников — семьдесят камней.") },
                Steps = new[] { new StepDef { Kind = StepKind.Kill, Target = "bandit*", Count = 6, Text = "Убейте шестерых разбойников", Marker = "camp" } },
                Reward = new Reward { Insight = 110, Stones = 70 }
            },
            new QuestDef
            {
                Id = "b_storm", Title = "Поручение: гребень", Chapter = 4, Giver = "scribe", Needs = "m15_done", Repeatable = true,
                Summary = "Мастерам слияния нужны грозовые ядра, а страже — чтобы молниевых волков стало меньше.",
                Offer = new[] { L("scribe", "Шесть молниевых волков на гребне. Девяносто камней и благодарность стражи.") },
                Steps = new[] { new StepDef { Kind = StepKind.Kill, Target = "storm_wolf", Count = 6, Text = "Убейте шесть молниевых волков", Marker = "ridge" } },
                Reward = new Reward { Insight = 150, Stones = 90 }
            },
        };

        /// <summary>The words at the end, by the ending's flag.</summary>
        public static readonly Dictionary<string, string[]> Endings = new Dictionary<string, string[]>
        {
            ["ending_righteous"] = new[]
            {
                "Гу лопается в кулаке, как перезрелая ягода.",
                "Наверху, у зала предков, Духовный источник вздыхает — и вода в нём поднимается до краёв, впервые за двести лет.",
                "Наставник Тэ рассказывает клану всё. Старейшины молчат. Потом самый старый из них снимает с пояса лунную печать академии и протягивает вам.",
                "Колодец на сорок четыре доли. Самый молодой старейшина в истории Гу Юэ.",
                "Конец. Долина остаётся вашей: поручения, слияние и всё, что вы не успели найти.",
            },
            ["ending_demonic"] = new[]
            {
                "Гу Кровавой луны вползает в апертуру, как к себе домой. Колодец наполняется до краёв — и не вашим светом.",
                "Наверху набат. Наставник Тэ стоит у плиты с двумя десятками стражей. Он смотрит на ваши глаза и опускает руку.",
                "— Уходи, Лань. Я скажу, что не догнал.",
                "Клан Гу Юэ объявляет охоту на демонического Гу-мастера. Награда — тысяча камней. Вы стоите больше.",
                "Конец. Долина остаётся вашей — пока вас не нашли.",
            },
        };

        static Dictionary<string, QuestDef> byId;

        public static QuestDef Get(string id)
        {
            if (byId == null)
            {
                byId = new Dictionary<string, QuestDef>();

                foreach (var quest in All)
                {
                    byId[quest.Id] = quest;
                }
            }

            return id != null && byId.TryGetValue(id, out var found) ? found : null;
        }
    }
}
