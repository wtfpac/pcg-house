using System.Collections.Generic;

namespace PCG.Generation
{
    // regra unica de vizinhanca. trabalha tanto com retangulos soltos
    // quanto com comodos compostos por varios retangulos
    public static class Adjacency
    {
        // dois retangulos sao vizinhos caso se toquem, um trecho em comum tem pelo menos MINSHARED celulas
        public static bool AreNeighbours(Area a, Area b, int minShared)
        {
            return SharedLength(a, b) >= minShared;
        }

        // dois comodos sao vizinhos se uma celula d um encosta na celula do outro
        public static bool AreNeighbours(Room a, Room b, int minShared)
        {
            return SharedLength(a, b) >= minShared;
        }

        // tamanho da borda compartilhada entre dois retangulos
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

        // maior borda compartilhada entre quaisquer partes dos dois comodos
        public static int SharedLength(Room a, Room b)
        {
            int best = 0;

            foreach (Area pa in a.Parts)
            {
                foreach (Area pb in b.Parts)
                {
                    int len = SharedLength(pa, pb);

                    if (len > best)
                        best = len;
                }
            }

            return best;
        }

        // quanto dois intervalos se cruzam. zero ou menos = nao se cruzam
        public static int Overlap(int startA, int endA, int startB, int endB)
        {
            int start = startA > startB ? startA : startB;
            int end = endA < endB ? endA : endB;

            return end - start;
        }
    }
}