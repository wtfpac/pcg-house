using System.Collections.Generic;

namespace PCG.Generation
{
    // descobre quais comodos encostam em quais
    // funciona com comodos compostos por varias partes
    public static class RoomGraph
    {
        public struct Edge
        {
            public int A;
            public int B;
            public WallSpan Wall;   // a parede em comum, onde pode entrar uma porta

            public Edge(int a, int b, WallSpan wall)
            {
                A = a;
                B = b;
                Wall = wall;
            }
        }

        public static List<Edge> Build(List<Room> rooms, int minShared)
        {
            List<Edge> edges = new List<Edge>();

            // j comeca em i+1 pra nao repetir par nem comparar consigo mesmo
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    WallSpan wall = Adjacency.Wall(rooms[i], rooms[j]);

                    if (wall.Length >= minShared)
                        edges.Add(new Edge(i, j, wall));
                }
            }

            return edges;
        }
    }
}