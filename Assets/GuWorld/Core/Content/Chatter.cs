using System;
using GuWorld.Core.Gen;

namespace GuWorld.Core
{
    /// <summary>
    /// What the townsfolk say when the hero stops one of them: a line by their trade, the hour, the
    /// weather and how they see the hero — a nobody of the first rank is spoken to as a boy, a master of
    /// the fourth is bowed to, someone the clan hates is muttered at. Names are made for them from the
    /// town's syllables (deterministic per person). Traders behind stalls offer their goods.
    /// </summary>
    public static class Chatter
    {
        static readonly string[] Family = { "Ван", "Ли", "Чжан", "Лю", "Чэнь", "Ян", "Хуан", "Чжао", "У", "Чжоу", "Сюй", "Сунь", "Ма", "Чжу", "Ху", "Го", "Хэ", "Линь", "Ло", "Гао" };
        static readonly string[] Given = { "Эр", "Сань", "Сы", "Да", "Сяо", "Мин", "Хуа", "Лань", "Мэй", "Фэн", "Шань", "Тао", "Цин", "Юнь", "Бао", "Чунь", "Лэй", "Пин", "Ань", "Жу" };

        /// <summary>A townsperson's name and what they are, for the talk's header.</summary>
        public static (string name, string title) Who(Walker walker)
        {
            var rng = new Rng(walker.Id * 7349 + 11);
            var family = Family[rng.Range(0, Family.Length)];
            var given = Given[rng.Range(0, Given.Length)];
            var clan = walker.Clan == "bai" ? "Бай" : walker.Clan == "xiong" ? "Сюн" : "Гу Юэ";

            switch (walker.Trade)
            {
                case Trade.Child: return ("Малыш " + given, "Ребёнок");
                case Trade.Elder: return ("Старейшина " + given, "Старший клана");
                case Trade.Student: return (clan + " " + given + (rng.Chance(0.5f) ? given.ToLowerInvariant() : ""), "Ученик академии");
                case Trade.Guard: return (clan + " " + family, "Страж ворот");
                case Trade.Watch: return ("Ночной сторож " + family, "Ночная стража");
                case Trade.Trader: return ((rng.Chance(0.4f) ? "Тётушка " : "Лао ") + family, "Торговец");
                case Trade.Porter: return (family + " " + given, "Носильщик");
                case Trade.Farmer: return (family + " " + given, "Крестьянин");
                default: return (family + " " + given, "Горожанин");
            }
        }

        /// <summary>One or two lines a townsperson says now.</summary>
        public static Line[] Say(Walker walker, float hour, int heroRank, Standing standing, bool rain, int seed)
        {
            var (name, title) = Who(walker);
            var who = "~" + name + "|" + title;
            var rng = new Rng(walker.Id * 31 + seed * 977 + 5);

            if (standing <= Standing.Disliked && walker.Trade != Trade.Child)
            {
                return new[] { new Line(who, rng.Pick(Hated)) };
            }

            var pool = walker.Trade switch
            {
                Trade.Trader => Trader,
                Trade.Porter => Porter,
                Trade.Farmer => Farmer,
                Trade.Student => Student,
                Trade.Guard => Guard,
                Trade.Watch => Watch,
                Trade.Elder => Elder,
                Trade.Child => Child,
                _ => Idler,
            };

            var first = rng.Pick(pool);

            if (rng.Chance(0.35f))
            {
                first = rain ? rng.Pick(Rain) : hour < 9 ? rng.Pick(Morning) : hour >= 19 ? rng.Pick(Evening) : first;
            }

            var second = heroRank >= 4 ? rng.Pick(ToMaster) : heroRank == 3 ? rng.Pick(ToRising) : heroRank <= 1 && walker.Trade != Trade.Child ? rng.Pick(ToNobody) : null;

            return second != null && rng.Chance(0.5f) ? new[] { new Line(who, first), new Line(who, second) } : new[] { new Line(who, first) };
        }

        /// <summary>The goods of a stall by its variant (food, herbs, pills, Gu food).</summary>
        public static string ShelfOf(int stallVariant)
        {
            switch (stallVariant % 4)
            {
                case 0: return "stall_food";
                case 1: return "stall_herbs";
                case 2: return "stall_pills";
                default: return "stall_gu_food";
            }
        }

        static readonly string[] Trader =
        {
            "Свежее, сегодняшнее! Берите, пока соседка не перебила цену.",
            "Торговля нынче — что вода в решете. Все копят камни на Гу.",
            "Я вам уступлю. Немного. Совсем чуть-чуть.",
            "С рассвета стою. Если ничего не берёте — хоть не загораживайте.",
            "Говорят, в горах опять видели волков. Значит, шкуры подешевеют.",
            "У Бай товар белее, а у нас честнее. Так и запишите.",
        };

        static readonly string[] Porter =
        {
            "Посторонитесь! Мешок тяжёлый, ноги — нет.",
            "Третья ходка с утра. Кузнецу всё мало угля.",
            "Мастерам Гу хорошо: у них ветер под ногами. А у нас — мозоли.",
            "Не держите. Опоздаю — хозяин вычтет.",
            "Рис с южных полей пришёл. К вечеру весь разберут.",
        };

        static readonly string[] Farmer =
        {
            "Лунные орхидеи в этом году хорошие. Гу будут сыты.",
            "Дождя бы. Или хотя бы тумана.",
            "Кабаны опять потоптали край поля. Позвать бы кого из мастеров…",
            "Земля тут добрая. Камни только вылезают каждую весну.",
            "Встал до света — лягу до тьмы. Так и живём.",
        };

        static readonly string[] Student =
        {
            "Наставник опять гонял нас по формуле очищения. Я её во сне читаю.",
            "Ты слышал? Говорят, кто-то в нашем году уже на второй ступени.",
            "Мой Гу Лунного света сегодня дважды промахнулся. Он ленится, я знаю.",
            "Экзамен через месяц. Камней на прорыв не хватает.",
            "В академии кормят так, что Гу сыты лучше учеников.",
        };

        static readonly string[] Guard =
        {
            "Проходите. Только без драк на рынке.",
            "За стеной неспокойно. Волки подходят к самым полям.",
            "Смена до заката. Потом — в таверну, а там как повезёт.",
            "Вчера у южных ворот поймали вора. Без Гу, дурак, лез.",
            "Пропуск? Шучу. Своих в лицо знаю.",
        };

        static readonly string[] Watch =
        {
            "Третья стража! Огонь берегите, двери запирайте!",
            "Ночью по улицам ходят только сторожа да воры. Ты не похож на сторожа.",
            "Фонарь гаснет — значит, ветер с перевала. К дождю.",
            "Тише. Старейшины спят, а у них уши — как у лисы.",
            "Видел огни за стеной. Волки, наверное. Или хуже.",
        };

        static readonly string[] Elder =
        {
            "Клан держится не на силе, а на памяти. Помни это.",
            "Источник мелеет, молодые торопятся. Плохое сочетание.",
            "В мои годы третий ранг был редкостью. Теперь — нетерпение.",
            "Не трать камни на мелочи. Гу ничего не прощает.",
            "Ходи осторожно. Гора видит больше, чем кажется.",
        };

        static readonly string[] Child =
        {
            "А правда, что у вас в животе живёт Гу? Покажите!",
            "Когда вырасту — стану мастером и улечу к облакам.",
            "Догоните! Не догоните!",
            "Мама говорит, к источнику ходить нельзя. А вы ходили?",
            "Я видел светлячка размером с кулак. Честно-честно.",
        };

        static readonly string[] Idler =
        {
            "Чай в «Восьмигранной» сегодня крепкий. Советую.",
            "Слыхали? Сюн и Бай опять не поделили перевал.",
            "Сидел бы и сидел. Солнце, площадь, никто не гонит.",
            "Говорят, в гробнице за горой нашли что-то. Или кого-то.",
            "У старого Вана снова сбежал петух. Третий раз за весну.",
        };

        static readonly string[] Morning = { "Рано вы. Даже лавки ещё не все открылись.", "Утро доброе. Туман сойдёт к полудню." };
        static readonly string[] Evening = { "Поздно уже. Скоро фонари зажгут.", "Вечер. Кто куда, а я — к ужину." };
        static readonly string[] Rain = { "Ну и льёт. Гу воды, говорят, в такую погоду сильнее.", "Промок до нитки. Хоть бы под навес пустили." };
        static readonly string[] ToNobody = { "Иди, мальчик, не мешай взрослым.", "Ты из тех, кому «не повезло» на церемонии? Бывает." };
        static readonly string[] ToRising = { "А вы, вижу, уже третьего ранга. Быстро нынче растут.", "Мастер… простите, не признал сразу." };
        static readonly string[] ToMaster = { "Господин мастер! Чем могу служить?", "Для меня честь, что такой мастер со мной заговорил." };
        static readonly string[] Hated = { "…", "Нечего тебе тут ходить.", "Смотри-ка, кто явился. Клан ещё припомнит." };
    }
}
