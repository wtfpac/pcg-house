namespace PCG.Generation
{
    // regra unica de vizinhanca entre dois retangulos.
    // usada pelo grafo e pela nomeacao dos comodos.
    public static class Adjacency
    {
        // dois retangulos sao vizinhos se as bordas se tocam e o
        // trecho em comum tem pelo menos minShared celulas
        public static bool AreNeighbours(Area a, Area b, int minShared)
        {
            // lado a lado
            if (a.Right == b.X || b.Right == a.X)
                return Overlap(a.Y, a.Bottom, b.Y, b.Bottom) >= minShared;

            // um acima do outro
            if (a.Bottom == b.Y || b.Bottom == a.Y)
                return Overlap(a.X, a.Right, b.X, b.Right) >= minShared;

            return false;
        }

        // quanto dois intervalos se cruzam. zero ou menos = nao se cruzam.
        public static int Overlap(int startA, int endA, int startB, int endB)
        {
            int start = startA > startB ? startA : startB;
            int end = endA < endB ? endA : endB;

            return end - start;
        }

        // tamanho da borda compartilhada. zero se nao se tocam.
        public static int SharedLength(Area a, Area b)
        {
            if (a.Right == b.X || b.Right == a.X)
            {
                int v = Overlap(a.Y, a.Bottom, b.Y, b.Bottom);
                return v > 0 ? v : 0;
            }

            if (a.Bottom == b.Y || b.Bottom == a.Y)
            {
                int h = Overlap(a.X, a.Right, b.X, b.Right);
                return h > 0 ? h : 0;
            }

            return 0;
        }
    }
}