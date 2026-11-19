using System;
using GuWorld.Core;

/// <summary>The rules of the hero's controls (ROADMAP 2.7.2): two places an action, one place an input, swaps.</summary>
static partial class Program
{
    static void RebindRules()
    {
        const int Q = 1, E = 2, R = 3, Mouse = 1001, Wheel = 1006;
        int[][] Of(params int[] flat)
        {
            var places = new int[flat.Length / 2][];

            for (var i = 0; i < places.Length; i++)
            {
                places[i] = new[] { flat[i * 2], flat[i * 2 + 1] };
            }

            return places;
        }

        bool Is(int[][] places, params int[] flat)
        {
            for (var i = 0; i < places.Length; i++)
            {
                if (places[i][0] != flat[i * 2] || places[i][1] != flat[i * 2 + 1])
                {
                    return false;
                }
            }

            return true;
        }

        var a = Of(Q, 0, E, 0);
        Rebind.Bind(a, 0, 0, E);
        Check(Is(a, E, 0, Q, 0), "controls: a key another action had is swapped to it");

        a = Of(Q, 0, E, R);
        Rebind.Bind(a, 0, 1, R);
        Check(Is(a, Q, R, E, 0), "controls: a second key taken from another action leaves it its main one");

        a = Of(Q, R);
        Rebind.Bind(a, 0, 0, R);
        Check(Is(a, R, Q), "controls: an action's own two keys swap places");

        a = Of(Q, 0, E, 0);
        Rebind.Bind(a, 0, 1, E);
        Check(Is(a, Q, E, 0, 0), "controls: taking another action's only key into an empty place leaves it with none (the page shows it)");

        a = Of(Q, Mouse);
        Rebind.Clear(a, 0, 0);
        Check(Is(a, Mouse, 0), "controls: a main place emptied takes the second input");

        a = Of(Q, 0, Q, E, E, Wheel);
        Rebind.Untangle(a);
        Check(Is(a, Q, 0, E, 0, Wheel, 0), "controls: inputs read back twice are kept by the first action only");

        a = Of(Q, Q);
        Rebind.Tidy(a[0]);
        Check(Is(a, Q, 0), "controls: one input in both places is kept once");

        // Any run of changes keeps every input in one place at most.
        var random = new Random(7);
        a = Of(1, 0, 2, 0, 3, 1002, 4, 0, 5, 1003, 6, 0, 7, 0, 1001, 0, 1006, 0, 1007, 0);
        var unique = true;

        for (var n = 0; n < 3000; n++)
        {
            var action = random.Next(a.Length);
            var input = random.Next(4) == 0 ? 1001 + random.Next(7) : 1 + random.Next(12);

            if (random.Next(6) == 0)
            {
                Rebind.Clear(a, action, random.Next(2));
            }
            else
            {
                Rebind.Bind(a, action, random.Next(2), input);
            }

            unique &= Rebind.Unique(a);
        }

        Check(unique, "controls: after 3000 changes no input is in two places");
    }
}
