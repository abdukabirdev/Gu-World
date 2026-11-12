namespace GuWorld.Core
{
    /// <summary>
    /// The side stories of the wider mountain: the Bai and the Xiong (each a chain that opens as they
    /// come to trust the hero), the hamlets' troubles, the places' secrets — the tomb, the mine and
    /// the Black Tail, the spiders and the Great Tree, the marsh, the eagles, the peak — and the
    /// treaty that could end forty years of feud over the spring.
    /// </summary>
    public static class RegionQuests
    {
        static Line L(string who, string text) => new Line(who, text);

        static StepDef Talk(string npc, string text, params Line[] lines) => new StepDef { Kind = StepKind.Talk, Target = npc, Text = text, Lines = lines };

        static StepDef Kill(string enemy, int count, string text, string marker) => new StepDef { Kind = StepKind.Kill, Target = enemy, Count = count, Text = text, Marker = marker };

        static StepDef Have(string item, int count, string text, string marker, bool takes = true) => new StepDef { Kind = StepKind.Have, Target = item, Count = count, Text = text, Marker = marker, Takes = takes };

        static StepDef Reach(string place, string text) => new StepDef { Kind = StepKind.Reach, Target = place, Text = text };

        static StepDef Use(string thing, int count, string text, string marker = null) => new StepDef { Kind = StepKind.Use, Target = thing, Count = count, Text = text, Marker = marker };

        static StepDef Fight(string encounter, string text) => new StepDef { Kind = StepKind.Encounter, Target = encounter, Text = text };

        /// <summary>The step with items given when it is done (what was found there).</summary>
        static StepDef Giving(StepDef step, params string[] items)
        {
            step.Gives = items;

            return step;
        }

        /// <summary>The step with a condition before it counts.</summary>
        static StepDef Needing(StepDef step, string condition)
        {
            step.Needs = condition;

            return step;
        }

        static (string, int)[] Rep(params (string clan, int change)[] changes) => changes;

        public static readonly QuestDef[] Mountain =
        {
            // ================================================================== The Bai

            new QuestDef
            {
                Id = "q_bai_gate", Title = "Чужак у белых стен", Chapter = 3, Giver = "bai_gatekeeper", Needs = "m14_done", Clan = "bai",
                Summary = "Победитель турнира пришёл к Бай. Страж у ворот не рад, но старейшина Бай Юнь хочет на вас посмотреть.",
                Offer = new[] { L("bai_gatekeeper", "Гу Юэ. Тот самый, с турнира. Бай Нин до сих пор не разговаривает."), L("bai_gatekeeper", "Старейшина велел пропустить, если придёшь. Иди в зал совета. И руки держи на виду.") },
                Steps = new[]
                {
                    Talk("bai_yun", "Поговорите со старейшиной Бай Юнем",
                        L("bai_yun", "Ты победил нашего лучшего ученика. Или он тебе поддался. Мне всё равно — мне интересно, зачем ты здесь."),
                        L("hero", "Хочу понять, кто на горе убивает учеников со слабой апертурой."),
                        L("bai_yun", "…Мы тоже теряем детей. Хорошо. Докажи, что Гу Юэ умеют не только брать. Наша травница ждёт ледяных лотосов — у омута, под водопадом.")),
                    Have("frost_lotus", 3, "Соберите три ледяных лотоса у омута", "bai_pool"),
                    Talk("bai_lin", "Отнесите лотосы травнице Бай Лин",
                        L("bai_lin", "Свежие. И стебли не поломаны. Вас кто-то учил?"),
                        L("bai_lin", "Приходите ещё. Я учу варить пилюли тех, кто не ломает стебли.")),
                },
                Reward = new Reward { Insight = 200, Stones = 50, Reputation = Rep(("bai", 15)), Flags = new[] { "bai_welcome" } }
            },
            new QuestDef
            {
                Id = "q_bai_hairpin", Title = "Шпилька Бай Мэй", Chapter = 3, Giver = "bai_mei", Needs = "bai_welcome", Clan = "bai",
                Summary = "Дочь рыбака уронила мамину шпильку в омут. Нырять она боится — там змей.",
                Offer = new[] { L("bai_mei", "Вы же мастер Гу! Вы не боитесь змея?"), L("bai_mei", "Я уронила мамину шпильку. С камня над омутом. Она блестит на дне, у самого края. Папа узнает — убьёт.") },
                Steps = new[]
                {
                    Giving(Use("hairpin_pool", 1, "Достаньте шпильку со дна омута", "hairpin_pool"), "lost_hairpin"),
                    Talk("bai_mei", "Верните шпильку Бай Мэй",
                        L("bai_mei", "Вы её достали! А змея видели?"),
                        L("hero", "Видел. Он сказал, что ты ему должна."),
                        L("bai_mei", "…Вы шутите. Вы шутите же?")),
                },
                Reward = new Reward { Insight = 80, Gu = new[] { "water_shield" }, Reputation = Rep(("bai", 8)) }
            },
            new QuestDef
            {
                Id = "q_bai_wolves", Title = "Снежная стая", Chapter = 4, Giver = "bai_yun", Needs = "q_bai_gate_done", NeedsReputation = 20, Clan = "bai",
                Summary = "Снежные волки спустились с вершины и режут скот у Туманного хутора. Бай не может отпустить дозорных — у них свои беды.",
                Offer = new[] { L("bai_yun", "Ты помог травнице. Теперь помоги клану. Снежные волки — у вершины и на тропе к Туманному хутору. Шесть шкур — и мы поговорим как друзья.") },
                Steps = new[] { Kill("snow_wolf", 6, "Убейте шесть снежных волков", "peak") },
                Reward = new Reward { Insight = 300, Stones = 120, Reputation = Rep(("bai", 15), ("mortal", 5)) }
            },
            new QuestDef
            {
                Id = "q_bai_duel", Title = "Первый клинок Бай", Chapter = 4, Giver = "bai_yun", Needs = "q_bai_wolves_done", NeedsReputation = 30, Clan = "bai",
                Summary = "Бай Шэн, первый клинок клана, не верит, что Гу Юэ победил честно. Старейшина разрешил поединок — до первой крови.",
                Offer = new[] { L("bai_yun", "Бай Шэн хочет боя. Откажешь — клан решит, что ты трус. Согласишься — может, решит, что ты друг. Я ставлю на второе.") },
                Steps = new[]
                {
                    Fight("duel_bai", "Победите Бай Шэна в поединке на площади"),
                    Talk("bai_yun", "Поговорите с Бай Юнем",
                        L("bai_yun", "Бай Шэн поклонился. Я не видел этого двадцать лет."),
                        L("bai_yun", "Держи. Рецепт Белого облака. Мы не даём его чужим — значит, ты уже не чужой.")),
                },
                Reward = new Reward { Insight = 400, Stones = 150, Items = new[] { ("scroll_white_cloud", 1) }, Reputation = Rep(("bai", 15)), Flags = new[] { "bai_friend" } }
            },
            new QuestDef
            {
                Id = "q_bai_vault", Title = "Хранилище у водопада", Chapter = 4, Giver = "bai_yun", Needs = "bai_friend", NeedsReputation = 50, Clan = "bai",
                Summary = "Бай Юнь доверяет вам ключ от хранилища клана. Возьмите то, что вам предназначено, — и только это.",
                Offer = new[] { L("bai_yun", "Ключ от хранилища. В нём рецепт Ледяного лотоса — наше сокровище. Возьми его. Мы считаем, что долг надо отдавать, пока жив.") },
                Gives = new[] { "bai_vault_key" },
                Steps = new[] { new StepDef { Kind = StepKind.Use, Target = "chest_bai_vault", Text = "Откройте хранилище Бай" } },
                Reward = new Reward { Insight = 300, Reputation = Rep(("bai", 10), ("gu_yue", -5)) }
            },

            // ================================================================== The Xiong

            new QuestDef
            {
                Id = "q_xiong_honey", Title = "Мёд для главы", Chapter = 3, Giver = "xiong_hua", Needs = "m14_done", Clan = "xiong",
                Summary = "Глава клана Сюн любит дикий мёд из Медвежьего лога. Пасечница Хуа боится туда ходить — медведи вышли из берлог.",
                Offer = new[] { L("xiong_hua", "Ты тот Гу Юэ, что побил Сюн Ли? Он хвастался, что ты бьёшь как девчонка. Видимо, как я."), L("xiong_hua", "Три диких улья в логе. Собери мёд — и глава примет тебя за своего. Почти.") },
                Steps = new[]
                {
                    Use("hive_*", 3, "Соберите мёд из трёх диких ульев в Медвежьем логе", "bear_hollow"),
                    Talk("xiong_jian", "Отнесите мёд Сюн Цзяню",
                        L("xiong_jian", "Мёд из лога! И ты жив! Медведи, значит, обленились."),
                        L("xiong_jian", "Садись. Пей. Сюн не помнят, кто чей клан, — Сюн помнят, кто принёс мёд.")),
                },
                Reward = new Reward { Insight = 200, Stones = 50, Items = new[] { ("honey", 3) }, Reputation = Rep(("xiong", 15)), Flags = new[] { "xiong_welcome" } }
            },
            new QuestDef
            {
                Id = "q_xiong_bears", Title = "Медведи вышли из лога", Chapter = 3, Giver = "xiong_ling", Needs = "xiong_welcome", Clan = "xiong",
                Summary = "Бурые медведи бродят у самой деревни. Охотница Лин думает, их кто-то гонит из лога.",
                Offer = new[] { L("xiong_ling", "Медведи не уходят из лога. Никогда. А теперь — у ворот. Их что-то гонит. Шесть медведей — и мы узнаем, кто остался в логе.") },
                Steps = new[] { Kill("brown_bear", 6, "Убейте шесть бурых медведей", "bear_hollow") },
                Reward = new Reward { Insight = 250, Stones = 90, Reputation = Rep(("xiong", 12)) }
            },
            new QuestDef
            {
                Id = "q_xiong_king", Title = "Медвежий король", Chapter = 4, Giver = "xiong_ling", Needs = "q_xiong_bears_done", NeedsReputation = 25, Clan = "xiong",
                Summary = "В логе проснулся Медвежий король — старый, злой и голодный. Он и гонит медведей к людям.",
                Offer = new[] { L("xiong_ling", "Я видела следы. Лапа — как колесо телеги. Медвежий король проснулся. Его не убивали сто лет. Пойдёшь?") },
                Steps = new[]
                {
                    Fight("bear_king", "Убейте Медвежьего короля"),
                    Talk("xiong_jian", "Расскажите Сюн Цзяню",
                        L("xiong_jian", "Сердце короля? У тебя в сумке? Ха-ха! Сюн Ба лопнет от зависти!"),
                        L("xiong_jian", "Вот тебе рецепт Великого медведя. Сердце у тебя уже есть. Остальное — мёд и дурь.")),
                },
                Reward = new Reward { Insight = 500, Stones = 200, Items = new[] { ("scroll_great_bear", 1) }, Reputation = Rep(("xiong", 15), ("mortal", 5)) }
            },
            new QuestDef
            {
                Id = "q_xiong_duel", Title = "Медвежья лапа", Chapter = 4, Giver = "xiong_jian", Needs = "q_xiong_bears_done", NeedsReputation = 30, Clan = "xiong",
                Summary = "Сюн Ба, сильнейший боец Сюн, требует боя. У Сюн это значит: «Ты мне нравишься».",
                Offer = new[] { L("xiong_jian", "Сюн Ба хочет тебя побить. Это значит, он тебя уважает. Если побьёшь его — уважать будут все.") },
                Steps = new[]
                {
                    Fight("duel_xiong", "Победите Сюн Ба в поединке"),
                    Talk("xiong_jian", "Поговорите с Сюн Цзянем",
                        L("xiong_jian", "Он лежит и смеётся. Значит, ты его сломал красиво."),
                        L("xiong_jian", "Рецепт Белого вепря — твой. Ты теперь Сюн по сердцу. По лицу — нет, извини.")),
                },
                Reward = new Reward { Insight = 400, Stones = 150, Items = new[] { ("scroll_white_boar", 1) }, Reputation = Rep(("xiong", 15)), Flags = new[] { "xiong_friend" } }
            },
            new QuestDef
            {
                Id = "q_xiong_ore", Title = "Руда для кузни", Chapter = 3, Giver = "xiong_meng", Needs = "xiong_welcome", Clan = "xiong",
                Summary = "Кузнецу Сюн Мэну нужна руда, а рудник на западе занят Чёрным Хвостом. Жилы есть и в самом руднике.",
                Offer = new[] { L("xiong_meng", "Восемь кусков руды. Хоть из-под земли — да, именно из-под земли. Рудник на западе. Там бандиты. Тебя это пугает? Меня — да.") },
                Steps = new[] { Have("iron_ore", 8, "Принесите восемь кусков железной руды", "mine") },
                Reward = new Reward { Insight = 150, Stones = 80, Items = new[] { ("iron_pill", 2) }, Reputation = Rep(("xiong", 10)), Flags = new[] { "learnt_iron_pill" } }
            },

            // ================================================================== The hamlets

            new QuestDef
            {
                Id = "q_willow_wolves", Title = "Овцы Сяо Юй", Chapter = 2, Giver = "willow_head", Needs = "m07_done", Clan = "mortal",
                Summary = "Волки режут овец Ивового хутора. Кланы смертным не помогают — а вы, может быть, поможете.",
                Offer = new[] { L("willow_head", "Мастер, пять волков. Только пять. Они ходят у пастбища на юго-востоке. Мы заплатим, чем сможем.") },
                Steps = new[] { Kill("wolf", 5, "Убейте пять волков у Ивового хутора", "willow"), Talk("willow_head", "Вернитесь к старосте Ли", L("willow_head", "Сяо Юй плакала от радости. Вот, что собрали. И сундук в амбаре — открывайте, ваше.")) },
                Reward = new Reward { Insight = 120, Stones = 30, Items = new[] { ("jerky", 4) }, Reputation = Rep(("mortal", 10)), Flags = new[] { "willow_wolves_done" } }
            },
            new QuestDef
            {
                Id = "q_mill_stone", Title = "Жернов", Chapter = 3, Giver = "mill_head", Needs = "m12_done", Clan = "mortal",
                Summary = "Чёрный Хвост увёз жернов мельницы к руднику — на нём точат ножи. Без жернова хутор без хлеба.",
                Offer = new[] { L("mill_head", "Они увезли мой жернов. Мой! Телега стоит у входа в рудник, я видел. Верни его — я буду молоть тебе бесплатно до самой смерти. Моей.") },
                Steps = new[] { Giving(Use("millstone_cart", 1, "Заберите жернов с телеги у рудника", "millstone_cart"), "mill_stone"), Have("mill_stone", 1, "Принесите жернов мельнику", "mill") },
                Reward = new Reward { Insight = 200, Stones = 60, Items = new[] { ("bitter_root", 4) }, Reputation = Rep(("mortal", 12), ("bandit", -5)) }
            },
            new QuestDef
            {
                Id = "q_terraces_rice", Title = "Рис хуторов", Chapter = 3, Giver = "terraces_head", Needs = "m12_done", Clan = "mortal",
                Summary = "Чёрный Хвост забрал дань трёх хуторов. Мешки — у бандитов, кто их носит.",
                Offer = new[] { L("terraces_head", "Три мешка риса. Это наша зима. Бандиты таскают их с собой, продают по мешку на тракте. Отнимите.") },
                Steps = new[] { Have("stolen_rice", 3, "Отнимите у Чёрного Хвоста три мешка риса", "mine") },
                Reward = new Reward { Insight = 220, Stones = 40, Reputation = Rep(("mortal", 15), ("bandit", -10)) }
            },
            new QuestDef
            {
                Id = "q_pine_brother", Title = "Брат лесоруба", Chapter = 3, Giver = "pine_head", Needs = "m11_done", Clan = "mortal",
                Summary = "Брат лесоруба Ма ушёл к перевалу за кедром и не вернулся.",
                Offer = new[] { L("pine_head", "Мой брат пошёл к перевалу три дня назад. Он всегда возвращается к ужину. Всегда.") },
                Steps = new[]
                {
                    Use("woodcutter_body", 1, "Найдите брата у перевала", "woodcutter_body"),
                    Talk("pass_monk", "Спросите монаха на перевале",
                        L("pass_monk", "Волки. Обычные волки. Я видел, как он бежал. Я не успел."),
                        L("pass_monk", "Скажи брату: он бежал вниз, к дому. Это важно — что к дому.")),
                    Talk("pine_head", "Вернитесь к лесорубу Ма", L("pine_head", "…К дому. Да. Он бы так и сделал."), L("pine_head", "Спасибо, мастер. Возьмите его топор — нет, это я сам. Возьмите камни.")),
                },
                Reward = new Reward { Insight = 180, Stones = 40, Reputation = Rep(("mortal", 10)) }
            },
            new QuestDef
            {
                Id = "q_honey_bees", Title = "Взбесившиеся пчёлы", Chapter = 3, Giver = "honey_head", Needs = "m11_done", Clan = "mortal",
                Summary = "Дикие пчёлы жалят всё, что движется. Пасечник Цао думает, их растревожили медведи.",
                Offer = new[] { L("honey_head", "Мои пчёлы смирные. Дикие — нет. Пять роёв вьются у хутора. Разгоните, а? Мёдом отплачу.") },
                Steps = new[] { Kill("bee_swarm", 5, "Разгоните пять роёв диких пчёл", "honey") },
                Reward = new Reward { Insight = 150, Items = new[] { ("honey", 6) }, Gu = new[] { "honey_dew" }, Reputation = Rep(("mortal", 10), ("xiong", 3)) }
            },
            new QuestDef
            {
                Id = "q_stone_lights", Title = "Огни у гробницы", Chapter = 4, Giver = "stone_head", Needs = "m15_done", Clan = "mortal",
                Summary = "Каменотёсы видят синие огни у Гробницы Древнего мастера. Двое не вернулись.",
                Offer = new[] { L("stone_head", "Синие огни. Блуждающие. Они ходят вокруг гробницы и зовут по именам. По нашим именам.") },
                Steps = new[] { Reach("tomb", "Дойдите до гробницы"), Kill("tomb_wisp", 4, "Погасите четыре блуждающих огня", "tomb"), Talk("tomb_scholar", "Поговорите с учёным у входа", L("tomb_scholar", "Огни? Это стражи. Не души — стражи. Кто-то разбудил гробницу."), L("tomb_scholar", "Я хочу внутрь. Помогите мне — и я расскажу, что знаю.")) },
                Reward = new Reward { Insight = 300, Stones = 80, Reputation = Rep(("mortal", 10), ("wanderers", 5)) }
            },
            new QuestDef
            {
                Id = "q_bamboo_snakes", Title = "Змеи в огородах", Chapter = 3, Giver = "bamboo_head", Needs = "m11_done", Clan = "mortal",
                Summary = "Змеи с болота ползут в огороды Бамбукового хутора.",
                Offer = new[] { L("bamboo_head", "Змеи. Шесть, семь… сколько их! Раньше на болоте сидел кто-то большой, и мелочь его боялась. Теперь — нет.") },
                Steps = new[] { Kill("marsh_snake", 6, "Убейте шесть болотных гадюк", "bamboo") },
                Reward = new Reward { Insight = 180, Stones = 40, Items = new[] { ("antidote", 3) }, Reputation = Rep(("mortal", 10)) }
            },
            new QuestDef
            {
                Id = "q_marsh_lord", Title = "Хозяин болота", Chapter = 4, Giver = "bamboo_head", Needs = "q_bamboo_snakes_done", Clan = "mortal",
                Summary = "Тот, кто держал змей в страхе, — Болотный удав. Он вернулся и голоден, и змеи бегут от него к людям.",
                Offer = new[] { L("bamboo_head", "Он вернулся. Удав. Мой дед видел его голову — с бочку. Мелочь бежит от него к нам. Убьёте его — змеи вернутся в болото.") },
                Steps = new[] { Use("python_nest", 1, "Найдите гнездо удава в сердце болота", "python_nest"), Fight("swamp_python", "Убейте Болотного удава") },
                Reward = new Reward { Insight = 500, Stones = 180, Reputation = Rep(("mortal", 15), ("wanderers", 5)) }
            },
            new QuestDef
            {
                Id = "q_misty_egg", Title = "Орлиное яйцо", Chapter = 4, Giver = "misty_head", Needs = "m15_done", Clan = "mortal",
                Summary = "Орлы с Орлиных скал уносят ягнят. Охотник У хочет вырастить своего орла — и отогнать чужих.",
                Offer = new[] { L("misty_head", "Орёл, которого вырастил человек, не подпустит к стаду чужих. Мне нужно яйцо из гнезда на скалах. Лезть туда — смерть. Вам — прогулка.") },
                Steps = new[] { Giving(Use("eagle_nest", 1, "Достаньте яйцо из гнезда на Орлиных скалах", "eagle_nest"), "eagle_egg"), Have("eagle_egg", 1, "Отнесите яйцо охотнику У", "misty") },
                Reward = new Reward { Insight = 250, Stones = 70, Items = new[] { ("wind_pill", 2) }, Reputation = Rep(("mortal", 10)) }
            },
            new QuestDef
            {
                Id = "q_eagle_king", Title = "Царь Орлиных скал", Chapter = 5, Giver = "misty_head", Needs = "q_misty_egg_done", Clan = "mortal",
                Summary = "Грозовой орёл, вожак стаи, теперь охотится на людей. Охотник У просит о последнем.",
                Offer = new[] { L("misty_head", "Грозовой орёл. Он взял мальчишку с Каменного хутора. Мальчишка жив — орёл держит его на скале. Пока.") },
                Steps = new[] { Fight("storm_eagle", "Победите Грозового орла"), Talk("misty_head", "Вернитесь к охотнику У", L("misty_head", "Мальчишка дома. Орлёнок вылупился. Знаете, на кого он похож? На вас. Злой и мокрый.")) },
                Reward = new Reward { Insight = 600, Stones = 220, Reputation = Rep(("mortal", 15)) }
            },
            new QuestDef
            {
                Id = "q_ford_rogues", Title = "Бродячие мастера", Chapter = 3, Giver = "ford_head", Needs = "m12_done", Clan = "mortal",
                Summary = "Бродячие мастера Гу грабят путников у брода.",
                Offer = new[] { L("ford_head", "Трое. В чёрном. Берут плату за брод, а брод — мой. Отвадьте их, мастер.") },
                Steps = new[] { Kill("rogue_master", 3, "Победите трёх бродячих мастеров", "ford") },
                Reward = new Reward { Insight = 220, Stones = 60, Reputation = Rep(("mortal", 10), ("wanderers", -5)) }
            },

            // ================================================================== The Black Tail

            new QuestDef
            {
                Id = "q_black_tail_mine", Title = "Рудник Чёрного Хвоста", Chapter = 4, Giver = "trade_captain", Needs = "m13_done", Clan = "wanderers",
                Summary = "Кривой Клык брал деньги за молчание. Деньги шли выше — в старый рудник на западе, к Чёрному Хвосту. Капитан заставы хочет знать, что там.",
                Offer = new[] { L("trade_captain", "Кривой Клык мёртв — твоя работа? Хорошо. Но он был мелочью. Камни с обозов уходили на запад, в рудник."), L("trade_captain", "Там держат рудокопов. Принеси их бирки — я хочу знать, сколько их было.") },
                Steps = new[]
                {
                    Talk("mine_survivor", "Найдите беглеца у входа в рудник",
                        L("mine_survivor", "Не кричи. Они услышат."),
                        L("mine_survivor", "Нас было тридцать. Мы копали не руду — первобытные камни. Хвост говорил: для заказчика из долины."),
                        L("mine_survivor", "Потом из нижнего штрека пришла она. Тысяченожка. Бирки тех, кого она взяла, — внутри.")),
                    Kill("black_tail_thug", 5, "Пробейтесь в рудник", "mine"),
                    Have("miner_tag", 3, "Соберите бирки рудокопов", "mine", false),
                    Fight("thousand_legs", "Убейте Тысяченожку в нижнем штреке"),
                    Talk("trade_captain", "Вернитесь к капитану Лу",
                        L("trade_captain", "Тридцать человек. А мы на заставе думали, что тракт пустой, потому что зима."),
                        L("trade_captain", "«Заказчик из долины». Твоей долины, Гу Юэ. Хвост сидит в старом форте на северо-западе. Пора с ним кончать.")),
                },
                Reward = new Reward { Insight = 500, Stones = 180, Reputation = Rep(("wanderers", 15), ("mortal", 10), ("bandit", -15)) }
            },
            new QuestDef
            {
                Id = "q_black_tail_fort", Title = "Логово Чёрного Хвоста", Chapter = 4, Giver = "trade_captain", Needs = "q_black_tail_mine_done", Clan = "wanderers",
                Summary = "Логово Чёрного Хвоста — старый форт на северо-западе. Там пленные и серебро. И, может быть, имя заказчика.",
                Offer = new[] { L("trade_captain", "Нас трое. Тебя — один. По-моему, честно. Иди в форт. Ворота — ваши, остальное — твоё.") },
                Steps = new[]
                {
                    Fight("fort_assault", "Возьмите логово штурмом"),
                    Use("fort_cell", 1, "Освободите пленницу в погребе", "fort_cell"),
                    Talk("trade_captain", "Отнесите печать атамана капитану",
                        L("trade_captain", "Печать Хвоста. И письма. Смотри: тот же учительский почерк."),
                        L("trade_captain", "Отнеси это своему наставнику. Кланы долго будут делать вид, что не знали. Мы — не будем.")),
                },
                Reward = new Reward { Insight = 700, Stones = 300, Reputation = Rep(("wanderers", 20), ("mortal", 15), ("gu_yue", 5), ("bandit", -40)), Flags = new[] { "lian_freed", "black_tail_beaten" } }
            },

            // ================================================================== The places' secrets

            new QuestDef
            {
                Id = "q_tomb", Title = "Гробница Древнего мастера", Chapter = 5, Giver = "tomb_scholar", Needs = "q_stone_lights_done", Clan = "wanderers",
                Summary = "Учёный Юань хочет прочесть стелы гробницы. Он думает, там похоронен мастер пятого ранга — и его Гу.",
                Offer = new[] { L("tomb_scholar", "Три стелы. Три оттиска. Я составлю из них ключ к вратам. А потом — врата, саркофаг, тысяча лет тишины."), L("tomb_scholar", "Если там кто-то есть — вы его разбудите. Простите заранее.") },
                Steps = new[]
                {
                    Use("stele_tomb_*", 3, "Прочтите три стелы гробницы", "tomb"),
                    Talk("tomb_scholar", "Вернитесь к учёному",
                        L("tomb_scholar", "«Здесь лежит тот, кто искал весну после осени». Вы понимаете? Я — нет."),
                        L("tomb_scholar", "Печать от врат — в погребальном ларце у южной стены. Я видел её через щель.")),
                    new StepDef { Kind = StepKind.Have, Target = "tomb_key", Text = "Найдите нефритовую печать", Marker = "chest_tomb" },
                    Use("tomb_gate", 1, "Откройте нефритовые врата", "tomb_gate"),
                    Fight("ancient_shade", "Упокойте Тень Древнего мастера"),
                },
                Reward = new Reward { Insight = 1200, Stones = 400, Reputation = Rep(("wanderers", 15)), Flags = new[] { "tomb_cleared" }, Gu = new[] { "spring_autumn_cicada" } }
            },
            new QuestDef
            {
                Id = "q_great_tree", Title = "Сердце древа", Chapter = 4, Giver = "old_witch", Needs = "m15_done", Clan = "mortal",
                Summary = "Великое древо умирает. Бабушка Гуй говорит: пауки с востока украли его семя.",
                Offer = new[] { L("old_witch", "Древу тысяча лет. Каждые сто лет оно роняет семя, и семя прорастает в его корнях. Сто лет назад — не проросло."), L("old_witch", "Пауки забрали. Паучья мать держит его в коконе, греет. Ей нравится свет. Принеси — и древо будет жить.") },
                Steps = new[]
                {
                    Fight("spider_queen", "Победите Паучью мать в паучьих пещерах"),
                    Giving(Use("spider_egg", 1, "Заберите семя из светящегося кокона", "spider_egg"), "spirit_seed"),
                    Use("tree_roots", 1, "Посадите семя в корни Великого древа", "tree_roots"),
                    Talk("old_witch", "Поговорите с бабушкой Гуй", L("old_witch", "Слышишь? Корни пьют. Сто лет не пили."), L("old_witch", "Древо помнит добро. Дупло открыто — возьми, что оно копило.")),
                },
                Reward = new Reward { Insight = 800, Stones = 150, Reputation = Rep(("mortal", 20)), Flags = new[] { "tree_healed" } }
            },
            new QuestDef
            {
                Id = "q_peak", Title = "Вершина Цинмао", Chapter = 4, Giver = "pass_monk", Needs = "m15_done", Clan = "wanderers",
                Summary = "Монах перевала говорит: на вершине старое святилище, и кто помолится там — увидит гору целиком.",
                Offer = new[] { L("pass_monk", "Ты ходишь по горе, как муравей по столу. Поднимись на вершину. Святилище покажет тебе стол.") },
                Steps = new[] { Reach("peak", "Поднимитесь на вершину Цинмао"), Use("shrine_peak", 1, "Помолитесь в святилище вершины", "shrine_peak"), Use("stele_peak", 1, "Прочтите надпись на вершине", "stele_peak") },
                Reward = new Reward { Insight = 500, Reputation = Rep(("wanderers", 10)), Flags = new[] { "peak_blessed", "map_revealed" } }
            },

            // ================================================================== The treaty

            new QuestDef
            {
                Id = "q_treaty", Title = "Договор о водах", Gives = new[] { "peace_treaty" }, Chapter = 5, Giver = "pass_monk", Needs = "black_tail_beaten", Clan = "wanderers",
                Summary = "Сорок лет три клана спорят из-за источника. Монах перевала держит договор, на котором не хватает трёх печатей.",
                Offer = new[] { L("pass_monk", "Этот договор написали сорок лет назад. Ни один клан его не подписал. Ты — друг всем трём. Почти."), L("pass_monk", "Принеси три печати. Если хоть один клан откажет — порви бумагу. Так тоже будет честно.") },
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Talk, Target = "bai_yun", Text = "Получите печать Бай (нужна дружба с Бай)", Needs = "rep:bai>=20",
                        Lines = new[] { L("bai_yun", "Договор. Монах ещё жив? Удивительно."), L("bai_yun", "Бай подпишут. Не ради Гу Юэ — ради тебя. Надеюсь, ты понимаешь разницу.") } },
                    Needing(Talk("xiong_jian", "Получите печать Сюн (нужна дружба с Сюн)", L("xiong_jian", "Вода общая? Как мёд? Ха! Ладно."), L("xiong_jian", "Сюн подпишут. Но если Гу Юэ нарушат — я сам приду. С медведем.")), "rep:xiong>=20"),
                    Talk("instructor", "Получите печать Гу Юэ у наставника Тэ", L("instructor", "Старейшины не подпишут. Старейшины… уже не старейшины, ты же знаешь."), L("instructor", "Подпишу я. Как глава академии. Пусть попробуют возразить.")),
                    Talk("pass_monk", "Вернитесь к монаху на перевал", L("pass_monk", "Три печати. Сорок лет. Я думал, умру раньше."), L("pass_monk", "Иди, мастер. Гора теперь чуть тише. Чуть.")),
                },
                Reward = new Reward { Insight = 1000, Stones = 500, Reputation = Rep(("bai", 20), ("xiong", 20), ("gu_yue", 20), ("mortal", 20), ("wanderers", 20)), Flags = new[] { "treaty_signed" } }
            },
        };

        /// <summary>The mountain's errands and the wild lands'.</summary>
        public static readonly QuestDef[] All = Lists.Join(Mountain, Wilds.Quests);
    }
}
