namespace GuWorld.Core
{
    /// <summary>A hint shown once, when its condition first holds (conditions in the content language).</summary>
    public sealed class HintDef
    {
        public string Id;
        public string When;
        public string Text;
    }

    /// <summary>The game's hints: each system explained the first time it matters, never twice.</summary>
    public static class Hints
    {
        public static readonly HintDef[] All =
        {
            new HintDef { Id = "slots", When = "", Text = "Гу в активных слотах — клавиши Q E R F (позже Z и C). Апертура и слоты — K." },
            new HintDef { Id = "guard", When = "kills>=3", Text = "Ctrl — блок. Поставленный в последний миг перед ударом — парирование: враг пошатнётся." },
            new HintDef { Id = "lantern", When = "chapter>=2", Text = "Лунные фонари запоминают вас: у них вы очнётесь, а между зажжёнными можно перемещаться." },
            new HintDef { Id = "paths", When = "kills>=15", Text = "Каждый Гу учит вас своему пути. Постижение путей — страница P: мастер пути сильнее и тратит меньше эссенции." },
            new HintDef { Id = "moves", When = "rank>=2", Text = "Убийственные приёмы — несколько Гу разом. Страница U: соберите нужные Гу в апертуре и постигните приём. V — применить, X — следующий." },
            new HintDef { Id = "tempering", When = "rank>=2", Text = "Гу можно закалить до лучшего качества: K → выберите Гу → «Закалка…». Нужны осколки нефрита и камни." },
            new HintDef { Id = "tide", When = "wolf_tide", Text = "Волчий прилив: стаи идут к северным воротам деревни. Встаньте там и отбейте три волны — клан заплатит." },
            new HintDef { Id = "home", When = "stones>=700", Text = "В городе Гу Юэ продаётся дом у академии: кладовая, питомник для Гу и кровать." },
            new HintDef { Id = "aperture", When = "rank>=3", Text = "Попробовать любые Гу и приёмы без последствий можно в Апертуре бессмертного: Esc → «Апертура бессмертного»." },
            new HintDef { Id = "wilds", When = "rank>=4", Text = "За горой — дикие земли с хозяевами четвёртого и пятого ранга. Капитан торгового поста знает, куда идти." },
            new HintDef { Id = "relics", When = "rank>=2", Text = "Гу-реликвии поднимают на ступень сразу — но только в своём ранге и не через ворота ранга." },
            new HintDef { Id = "affinity", When = "kills>=25", Text = "Звери боятся одних путей и стойки к другим: число с «!» — удар по слабости, серое — по стойкости. Победив троих одного вида, вы узнаете это в кодексе." },
            new HintDef { Id = "harmony", When = "rank>=2", Text = "Три Гу одного пути в апертуре звучат созвучно — их удары сильнее. Пять путей и больше — разлад, слабеет всё. Но разные пути дают реакции: огонь по холоду — пар, яд с кровью — заражение." },
            new HintDef { Id = "elites", When = "kills>=40", Text = "Иногда в стае выходит элитный зверь — светится и зовётся «Свирепым», «Ледяным»… Он вдвое опаснее и вдвое щедрее." },
        };
    }
}
