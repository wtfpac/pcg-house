namespace PCG.Generation
{
    //retangulo em coordenadas inteiras
    public struct Area
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public Area(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        //bordas opostas, caalcula na hora
        public int Right => X + Width;
        public int Bottom => Y + Height;

        //area em celulas, usada p decidir ql comodo é o maior
        public int Size => Width * Height;
    }
}