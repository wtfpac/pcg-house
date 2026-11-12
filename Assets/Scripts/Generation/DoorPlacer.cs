using System.Collections.Generic;
using System.Linq;

namespace PCG.Generation
{
    // escolhe quais arestas do grafo viram porta. nem todo par de vizinhos
    // ganha uma: cada tipo de comodo tem um limite, e as ligacoes mais
    // importantes sao atendidas primeiro.
    public static class DoorPlacer
    {
        // pesos da ordenacao. a parede em comum soma no final, como desempate.
        private const int HallBonus = 3000;
        private const int CirculationBonus = 1000;

        public static HashSet<WallKey> Place(FloorPlan plan)
        {
            HashSet<WallKey> doors = new HashSet<WallKey>();
            int[] used = new int[plan.Rooms.Count];   // portas que cada comodo ja tem

            // percorre as arestas da mais importante pra menos importante
            foreach (RoomGraph.Edge edge in plan.Edges.OrderByDescending(e => Priority(plan, e)))
            {
                if (IsFull(plan, used, edge.A) || IsFull(plan, used, edge.B))
                    continue;

                doors.Add(edge.Wall.Middle);

                used[edge.A]++;
                used[edge.B]++;
            }

            return doors;
        }

        // quanto maior o valor, mais cedo a ligacao e atendida
        private static int Priority(FloorPlan plan, RoomGraph.Edge edge)
        {
            Room a = plan.Rooms[edge.A];
            Room b = plan.Rooms[edge.B];

            // entre duas ligacoes do mesmo nivel, ganha a de parede mais larga
            int score = edge.Wall.Length;

            bool toHall = a.Type == RoomType.Hall || b.Type == RoomType.Hall;

            // quarto e banheiro preferem abrir no corredor, que e a zona intima.
            // depois vem qualquer ligacao com a circulacao.
            if ((a.IsPrivate || b.IsPrivate) && toHall)
                score += HallBonus;
            else if (a.IsCirculation || b.IsCirculation)
                score += CirculationBonus;

            return score;
        }

        // esse comodo ja atingiu o limite de portas do tipo dele?
        private static bool IsFull(FloorPlan plan, int[] used, int index)
        {
            int limit = DoorLimit(plan.Rooms[index].Type);

            return limit >= 0 && used[index] >= limit;
        }

        // quantas portas cada tipo pode ter. -1 significa sem limite.
        private static int DoorLimit(RoomType type)
        {
            switch (type)
            {
                case RoomType.Bedroom:  return 1;
                case RoomType.Bathroom: return 1;
                case RoomType.Kitchen:  return 2;
                default:                return -1;   // sala e corredor
            }
        }
    }
}
