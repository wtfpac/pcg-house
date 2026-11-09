using System.Collections.Generic;

namespace PCG.Generation
{
    // descobre quais comodos encostam em quais
    public static class RoomGraph
    {
        public struct Edge
        {
            public int A;
            public int B;

            public Edge(int a, int b)
            {
                A = a;
                B = b;
            }
        }

        public static List<Edge> Build(List<Room> rooms, int minShared)
        {
            List<Edge> edges = new List<Edge>();

            // j comeca em i+1 pra nao repetir par nem comparar consigo mesmo
            for (int i = 0; i < rooms.Count; i++)
                for (int j = i + 1; j < rooms.Count; j++)
                    if (Adjacency.AreNeighbours(rooms[i].Area, rooms[j].Area, minShared))
                        edges.Add(new Edge(i, j));

            return edges;
        }
    }
}