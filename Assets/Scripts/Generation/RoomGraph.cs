using System;
using System.Collections.Generic;

namespace PCG.Generation
{
    // descobr quais comodos encostam em quais
    // guarda os pares como indices da lista d comodos
    public static class RoomGraph
    {
        // par de comodos vizinhos
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

        // minshared = qts celulas d borda em comum sao necessarias
        public static List<Edge> Build(List<Room> rooms, int minShared)
        {
            List<Edge> edges = new List<Edge>();

            // compara cada par uma vez só
            // j começa em i+1 p nao repetir par nem comparar a si mesmo
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    if (AreNeighbours(rooms[i].Area, rooms[j].Area, minShared))
                        edges.Add(new Edge(i, j));
                }
            }

            return edges;
        }

        // dois comodos sao vizinhos se as bordas se tocam e o trecho em comum tem TAM suficient
        private static bool AreNeighbours(Area a, Area b, int minShared)
        {
            // lado a lado: a borda direita de um toca a esquerda do outro
            if (a.Right == b.X || b.Right == a.X)
                return Overlap(a.Y, a.Bottom, b.Y, b.Bottom) >= minShared;

            // um em cima do outro
            if (a.Bottom == b.Y || b.Bottom == a.Y)
                return Overlap(a.X, a.Right, b.X, b.Right) >= minShared;

            return false;
        }

        // quanto dois intervalos se cruzam; zero ou negativo = nao se cruzam
        private static int Overlap(int startA, int endA, int startB, int endB)
        {
            int start = Math.Max(startA, startB);
            int end = Math.Min(endA, endB);

            return end - start;
        }
    }
}