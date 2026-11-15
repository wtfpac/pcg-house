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

        // o trecho cobre um lado inteiro desse retangulo?
        public bool CoversSideOf(Area area)
        {
            return Length == (Vertical ? area.Height : area.Width);
        }
    }
}