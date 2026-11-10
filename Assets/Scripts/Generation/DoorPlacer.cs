using System.Collections.Generic;

namespace PCG.Generation
{
    // decide onde abrir porta, respeitando um limite por tipo de comodo.
    // quarto e banheiro tem uma porta so; sala e corredor nao tem limite.
    public static class DoorPlacer
    {
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

        private static bool IsCirculation(RoomType type)
        {
            return type == RoomType.Living || type == RoomType.Hall;
        }

        public static HashSet<WallKey> Place(FloorPlan plan, List<RoomGraph.Edge> edges)
        {
            HashSet<WallKey> doors = new HashSet<WallKey>();
            int[] used = new int[plan.Rooms.Count];

            // ordena: primeiro as arestas que tocam circulacao, depois
            // as de borda mais larga. assim o quarto gasta sua unica
            // porta no corredor, nao no quarto vizinho.
            List<RoomGraph.Edge> sorted = new List<RoomGraph.Edge>(edges);

            sorted.Sort((x, y) => Priority(plan, y).CompareTo(Priority(plan, x)));

            foreach (RoomGraph.Edge edge in sorted)
            {
                if (!HasRoom(plan, used, edge.A) || !HasRoom(plan, used, edge.B))
                    continue;

                Area a = plan.Rooms[edge.A].Area;
                Area b = plan.Rooms[edge.B].Area;

                List<WallKey> shared = FindSharedWall(a, b);

                if (shared.Count == 0)
                    continue;

                doors.Add(shared[shared.Count / 2]);

                used[edge.A]++;
                used[edge.B]++;
            }

            return doors;
        }

        // peso da aresta na ordenacao. maior = atendida antes.
        private static int Priority(FloorPlan plan, RoomGraph.Edge edge)
        {
            RoomType ta = plan.Rooms[edge.A].Type;
            RoomType tb = plan.Rooms[edge.B].Type;

            int score = 0;

            // comodo privativo prefere abrir no corredor
            bool privateRoom = IsPrivate(ta) || IsPrivate(tb);
            bool hasHall = ta == RoomType.Hall || tb == RoomType.Hall;

            if (privateRoom && hasHall)
                score += 3000;
            else if (IsCirculation(ta) || IsCirculation(tb))
                score += 1000;

            // entre duas opcoes, prefere a borda mais larga
            score += Adjacency.SharedLength(plan.Rooms[edge.A], plan.Rooms[edge.B]);

            return score;
        }

        private static bool IsPrivate(RoomType type)
        {
            return type == RoomType.Bedroom || type == RoomType.Bathroom;
        }

        // esse comodo ainda pode receber porta?
        private static bool HasRoom(FloorPlan plan, int[] used, int index)
        {
            int limit = DoorLimit(plan.Rooms[index].Type);

            return limit < 0 || used[index] < limit;
        }

        private static List<WallKey> FindSharedWall(Area a, Area b)
        {
            List<WallKey> found = new List<WallKey>();

            if (a.Right == b.X)
                AddVertical(found, a.Right, a.Y, a.Bottom, b.Y, b.Bottom);
            else if (b.Right == a.X)
                AddVertical(found, b.Right, a.Y, a.Bottom, b.Y, b.Bottom);
            else if (a.Bottom == b.Y)
                AddHorizontal(found, a.Bottom, a.X, a.Right, b.X, b.Right);
            else if (b.Bottom == a.Y)
                AddHorizontal(found, b.Bottom, a.X, a.Right, b.X, b.Right);

            return found;
        }

        private static void AddVertical(List<WallKey> list, int x,
                                        int startA, int endA, int startB, int endB)
        {
            int start = startA > startB ? startA : startB;
            int end = endA < endB ? endA : endB;

            for (int y = start; y < end; y++)
                list.Add(new WallKey(true, x, y));
        }

        private static void AddHorizontal(List<WallKey> list, int y,
                                          int startA, int endA, int startB, int endB)
        {
            int start = startA > startB ? startA : startB;
            int end = endA < endB ? endA : endB;

            for (int x = start; x < end; x++)
                list.Add(new WallKey(false, x, y));
        }
    }
}