using System;

namespace PCG.Generation
{
    // regra de adjacencia, trabalha com comodos vizinhos
    public static class Adjacency
    {
        public static WallSpan Wall(Area a, Area b)
        {
            if (a.Right == b.X || b.Right == a.X)
            {
                int column = a.Right == b.X ? a.Right : b.Right;
                return Span(true, column, a.Y, a.Bottom, b.Y, b.Bottom);
            }

            if (a.Bottom == b.Y || b.Bottom == a.Y)
            {
                int row = a.Bottom == b.Y ? a.Bottom : b.Bottom;
                return Span(false, row, a.X, a.Right, b.X, b.Right);
            }

            return new WallSpan();
        }

        // maior borda compartilhada entre quaisquer partes dos dois comodos
        public static WallSpan Wall(Room a, Room b)
        {
            WallSpan best = new WallSpan();

            foreach (Area partA in a.Parts)
            {
                foreach (Area partB in b.Parts)
                {
                    WallSpan wall = Wall(partA, partB);

                    if (wall.Length > best.Length)
                        best = wall;
                }
            }

            return best;
        }

        // dois retangulos sao vizinhos caso se toquem, um trecho em comum tem pelo menos MINSHARED celulas
        public static bool AreNeighbours(Area a, Area b, int minShared)
        {
            return Wall(a, b).Length >= minShared;
        }

        // dois comodos sao vizinhos se uma celula d um encosta na celula do outro
        public static bool AreNeighbours(Room a, Room b, int minShared)
        {
            return Wall(a, b).Length >= minShared;
        }

        private static WallSpan Span(bool vertical, int line,
                                     int startA, int endA, int startB, int endB)
        {
            int start = Math.Max(startA, startB);
            int end = Math.Min(endA, endB);

            return new WallSpan(vertical, line, start, Math.Max(0, end - start));
        }
    }
}