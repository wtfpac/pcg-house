namespace PCG.Generation
{
    public struct WallSpan
    {
        public bool Vertical;
        public int Line;
        public int Start;
        public int Length;

        public WallSpan(bool vertical, int line, int start, int length)
        {
            Vertical = vertical;
            Line = line;
            Start = start;
            Length = length;
        }

        // o pedaco de parede do meio do trecho. e onde as portas sao abertas
        public WallKey Middle
        {
            get
            {
                int middle = Start + Length / 2;

                return Vertical
                    ? new WallKey(true, Line, middle)
                    : new WallKey(false, middle, Line);
            }
        }

        public WallSpan Centered(int size)
        {
            int length = System.Math.Min(size, Length);
            int start = Start + (Length - length) / 2;

            return new WallSpan(Vertical, Line, start, length);
        }

        public System.Collections.Generic.IEnumerable<WallKey> Keys
        {
            get
            {
                for (int i = 0; i < Length; i++)
                    yield return Vertical
                        ? new WallKey(true, Line, Start + i)
                        : new WallKey(false, Start + i, Line);
            }
        }

        // o trecho cobre um lado inteiro desse retangulo?
        public bool CoversSideOf(Area area)
        {
            return Length == (Vertical ? area.Height : area.Width);
        }
    }
}