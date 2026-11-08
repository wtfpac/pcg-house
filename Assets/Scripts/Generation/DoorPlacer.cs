using System.Collections.Generic;

namespace PCG.Generation
{
    // decide onde abrir porta entre comodos vizinhos
    public static class DoorPlacer
    {
        // identifica uma celula de parede.
        // vertical = parede que corre no eixo Z, numa coluna x fixa.
        public struct WallKey
        {
            public bool Vertical;
            public int X;
            public int Y;

            public WallKey(bool vertical, int x, int y)
            {
                Vertical = vertical;
                X = x;
                Y = y;
            }
        }

        // devolve as celulas de parede que viram vao de porta
        public static HashSet<WallKey> Place(FloorPlan plan, List<RoomGraph.Edge> edges)
        {
            HashSet<WallKey> doors = new HashSet<WallKey>();

            foreach (RoomGraph.Edge edge in edges)
            {
                Area a = plan.Rooms[edge.A].Area;
                Area b = plan.Rooms[edge.B].Area;

                List<WallKey> shared = FindSharedWall(a, b);

                if (shared.Count == 0)
                    continue;

                // abre a celula do meio da fronteira
                doors.Add(shared[shared.Count / 2]);
            }

            return doors;
        }

        // lista as celulas de parede que ficam entre os dois comodos
        private static List<WallKey> FindSharedWall(Area a, Area b)
        {
            List<WallKey> found = new List<WallKey>();

            // b esta a direita de a
            if (a.Right == b.X)
                AddVertical(found, a.Right, a.Y, a.Bottom, b.Y, b.Bottom);

            // a esta a direita de b
            else if (b.Right == a.X)
                AddVertical(found, b.Right, a.Y, a.Bottom, b.Y, b.Bottom);

            // b esta abaixo de a
            else if (a.Bottom == b.Y)
                AddHorizontal(found, a.Bottom, a.X, a.Right, b.X, b.Right);

            // a esta abaixo de b
            else if (b.Bottom == a.Y)
                AddHorizontal(found, b.Bottom, a.X, a.Right, b.X, b.Right);

            return found;
        }

        // celulas da parede vertical, no trecho em que os dois se cruzam
        private static void AddVertical(List<WallKey> list, int x,
                                        int startA, int endA, int startB, int endB)
        {
            int start = startA > startB ? startA : startB;
            int end = endA < endB ? endA : endB;

            for (int y = start; y < end; y++)
                list.Add(new WallKey(true, x, y));
        }

        // celulas da parede horizontal, no trecho em comum
        private static void AddHorizontal(List<WallKey> list, int y,
                                          int startA, int endA, int startB, int endB)
        {
            int start = startA > startB ? startA : startB;
            int end = endA < endB ? endA : endB;

            for (int x = start; x < end; x++)
                list.Add(new WallKey(false, y: y, x: x));
        }
    }
}