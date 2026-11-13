using System.Collections.Generic;

namespace PCG.Generation
{
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

        private static int DoorLimit(RoomType type)
        {
            switch (type)
            {
                case RoomType.Bedroom:  return 1;
                case RoomType.Bathroom: return 1;
                case RoomType.Kitchen:  return 2;
                default:                return -1;
            }
        }

        private static bool IsCirculation(RoomType type)
        {
            return type == RoomType.Living || type == RoomType.Hall;
        }

        private static bool IsPrivate(RoomType type)
        {
            return type == RoomType.Bedroom || type == RoomType.Bathroom;
        }

        public static HashSet<WallKey> Place(FloorPlan plan, List<RoomGraph.Edge> edges)
        {
            HashSet<WallKey> doors = new HashSet<WallKey>();
            int[] used = new int[plan.Rooms.Count];

            // comodos privados tem preferencia d porta no corredor
            // depois vem qualquer ligacao com circulacao
            List<RoomGraph.Edge> sorted = new List<RoomGraph.Edge>(edges);

            sorted.Sort((x, y) => Priority(plan, y).CompareTo(Priority(plan, x)));

            foreach (RoomGraph.Edge edge in sorted)
            {
                if (!HasRoom(plan, used, edge.A) || !HasRoom(plan, used, edge.B))
                    continue;

                List<WallKey> shared = FindSharedWall(plan.Rooms[edge.A],
                                                      plan.Rooms[edge.B]);

                if (shared.Count == 0)
                    continue;

                doors.Add(shared[shared.Count / 2]);

                used[edge.A]++;
                used[edge.B]++;
            }

            return doors;
        }

        private static int Priority(FloorPlan plan, RoomGraph.Edge edge)
        {
            RoomType ta = plan.Rooms[edge.A].Type;
            RoomType tb = plan.Rooms[edge.B].Type;

            int score = 0;

            bool privateRoom = IsPrivate(ta) || IsPrivate(tb);
            bool hasHall = ta == RoomType.Hall || tb == RoomType.Hall;

            if (privateRoom && hasHall)
                score += 3000;
            else if (IsCirculation(ta) || IsCirculation(tb))
                score += 1000;

            score += Adjacency.SharedLength(plan.Rooms[edge.A], plan.Rooms[edge.B]);

            return score;
        }

        private static bool HasRoom(FloorPlan plan, int[] used, int index)
        {
            int limit = DoorLimit(plan.Rooms[index].Type);

            return limit < 0 || used[index] < limit;
        }

        // testa tds celulas d um comodo contra as partes do outro lado
        // devolve o maior trecho continuo encontrado
        private static List<WallKey> FindSharedWall(Room a, Room b)
        {
            List<WallKey> best = new List<WallKey>();

            foreach (Area pa in a.Parts)
            {
                foreach (Area pb in b.Parts)
                {
                    List<WallKey> found = FindSharedWall(pa, pb);

                    if (found.Count > best.Count)
                        best = found;
                }
            }

            return best;
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