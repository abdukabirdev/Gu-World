namespace GuWorld.Core
{
    /// <summary>
    /// The rules of the hero's controls (Scripts/Actors/Bindings holds them, ROADMAP 2.7.2): every action has
    /// two places, a main one and a second one; an input (a key, a mouse button, a wheel's turn — any number
    /// but 0, which is none) is in one place at most. Put in a place, an input others had goes to them in
    /// exchange for what was there; a main place left empty takes the second input.
    /// </summary>
    public static class Rebind
    {
        /// <summary>Puts `input` in place `slot` of action `action`; whoever had it takes what was there (the action's own other place: the two swap).</summary>
        public static void Bind(int[][] inputs, int action, int slot, int input)
        {
            if (inputs == null || action < 0 || action >= inputs.Length || input == 0)
            {
                return;
            }

            slot = slot <= 0 ? 0 : 1;
            var own = inputs[action];
            var old = own[slot];

            if (old == input)
            {
                return;
            }

            if (own[1 - slot] == input)
            {
                own[1 - slot] = old;
            }

            for (var i = 0; i < inputs.Length; i++)
            {
                if (i == action)
                {
                    continue;
                }

                for (var s = 0; s < 2; s++)
                {
                    if (inputs[i][s] == input)
                    {
                        inputs[i][s] = old;
                    }
                }

                Tidy(inputs[i]);
            }

            own[slot] = input;
            Tidy(own);
        }

        /// <summary>Empties a place (a main one takes the second input then).</summary>
        public static void Clear(int[][] inputs, int action, int slot)
        {
            if (inputs == null || action < 0 || action >= inputs.Length)
            {
                return;
            }

            inputs[action][slot <= 0 ? 0 : 1] = 0;
            Tidy(inputs[action]);
        }

        /// <summary>A main place left empty takes the second input; one input in both places is kept once.</summary>
        public static void Tidy(int[] places)
        {
            if (places[1] == places[0])
            {
                places[1] = 0;
            }

            if (places[0] == 0 && places[1] != 0)
            {
                places[0] = places[1];
                places[1] = 0;
            }
        }

        /// <summary>Inputs read back that two actions share (an older run's keys against newer defaults): the later action gives its place up.</summary>
        public static void Untangle(int[][] inputs)
        {
            for (var i = 0; i < inputs.Length; i++)
            {
                Tidy(inputs[i]);

                for (var s = 0; s < 2; s++)
                {
                    var input = inputs[i][s];

                    for (var j = 0; j < i && input != 0; j++)
                    {
                        if (inputs[j][0] == input || inputs[j][1] == input)
                        {
                            inputs[i][s] = 0;
                            input = 0;
                        }
                    }
                }

                Tidy(inputs[i]);
            }
        }

        /// <summary>Whether no input is in two places at once.</summary>
        public static bool Unique(int[][] inputs)
        {
            var seen = new System.Collections.Generic.HashSet<int>();

            foreach (var places in inputs)
            {
                foreach (var input in places)
                {
                    if (input != 0 && !seen.Add(input))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
