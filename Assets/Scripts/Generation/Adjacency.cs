using System;

namespace PCG.Generation
{
    // regra unica de vizinhanca do projeto: dois comodos sao vizinhos quando
    // compartilham um trecho de parede, todo o resto ;grafo, portas, validacao
    // usa esta classe, entao a regra nunca diverge de um lugar pro outro.
    public static class Adjacency
    {
        // a parede que dois retangulos compartilham. Length zero = nao se encostam.
        public static WallSpan Wall(Area a, Area b)
        {
            // lado a lado: parede vertical, na coluna onde um termina e o outro comeca
            if (a.Right == b.X || b.Right == a.X)
            {
                int column = a.Right == b.X ? a.Right : b.Right;
                return Span(true, column, a.Y, a.Bottom, b.Y, b.Bottom);
            }

            // um em cima do outro: parede horizontal
            if (a.Bottom == b.Y || b.Bottom == a.Y)
            {
                int row = a.Bottom == b.Y ? a.Bottom : b.Bottom;
                return Span(false, row, a.X, a.Right, b.X, b.Right);
            }

            return new WallSpan();
        }

        // a maior parede em comum entre dois comodos, olhando todas as partes de cada um
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

        // vizinhos = a parede em comum tem pelo menos minShared celulas,
        // que e o espaco necessario pra caber uma porta
        public static bool AreNeighbours(Area a, Area b, int minShared)
        {
            return Wall(a, b).Length >= minShared;
        }

        public static bool AreNeighbours(Room a, Room b, int minShared)
        {
            return Wall(a, b).Length >= minShared;
        }

        // monta o trecho onde os intervalos [startA, endA) e [startB, endB) se cruzam
        private static WallSpan Span(bool vertical, int line,
                                     int startA, int endA, int startB, int endB)
        {
            int start = Math.Max(startA, startB);
            int end = Math.Min(endA, endB);

            return new WallSpan(vertical, line, start, Math.Max(0, end - start));
        }
    }
}
