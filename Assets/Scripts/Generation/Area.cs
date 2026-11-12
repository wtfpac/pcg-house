namespace PCG.Generation
{
    // retangulo em coordenadas inteiras de celula. x,y e o canto superior esquerdo
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

        // bordas opostas, calculadas na hora pra nunca ficarem desatualizadas
        public int Right => X + Width;
        public int Bottom => Y + Height;

        // area em celulas, usada pra comparar o tamanho dos comodos
        public int Size => Width * Height;

        // os dois retangulos ocupam alguma celula em comum?
        public bool Overlaps(Area other)
        {
            return X < other.Right && other.X < Right
                && Y < other.Bottom && other.Y < Bottom;
        }

        // encosta em alguma borda do retangulo de fora?
        // com o contorno da casa, responde se o comodo toca a fachada.
        public bool TouchesEdgeOf(Area outer)
        {
            return X == outer.X || Right == outer.Right
                || Y == outer.Y || Bottom == outer.Bottom;
        }

        // corta o retangulo em dois pedacos, sem sobra nem buraco
        // alongX = true: o corte fica na posicao "at" do eixo X e separa esquerda e direita
        // alongX = false: fica na posicao "at" do eixo Y e separa cima e baixo
        public void Split(bool alongX, int at, out Area first, out Area second)
        {
            if (alongX)
            {
                first = new Area(X, Y, at, Height);
                second = new Area(X + at, Y, Width - at, Height);
            }
            else
            {
                first = new Area(X, Y, Width, at);
                second = new Area(X, Y + at, Width, Height - at);
            }
        }
    }
}
