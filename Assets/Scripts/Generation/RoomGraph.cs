using System.Collections.Generic;

namespace PCG.Generation
{
    // grafo de adjacencia da casa: os comodos sao os nos, e cada
    // aresta liga dois comodos que compartilham parede.
    public static class RoomGraph
    {
        // A e B sao as posicoes dos dois comodos na lista plan.Rooms
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

            // j comeca em i+1 pra testar cada par uma vez so
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
