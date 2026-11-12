namespace GuWorld.Core
{
    /// <summary>The hints the hero has been shown, and the next one due.</summary>
    public partial class Session
    {
        /// <summary>The first hint not yet shown whose condition holds (null if none); it is marked shown.</summary>
        public HintDef NextHint()
        {
            foreach (var hint in Hints.All)
            {
                if (!State.Hints.Contains(hint.Id) && Meets(hint.When))
                {
                    State.Hints.Add(hint.Id);

                    return hint;
                }
            }

            return null;
        }
    }
}
