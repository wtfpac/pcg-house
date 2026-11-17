using System.Collections.Generic;
using System.Linq;

namespace PCG.Generation
{
    public static class DoorPlacer
    {
        // pesos da ordenacao. a parede em comum soma no fina como deesempate
        private const int HallBonus = 3000;
        private const int CirculationBonus = 1000;

        public static List<WallSpan> Place(FloorPlan plan, HouseSettings s, bool openLivingHall)
        {
            List<WallSpan> doors = new List<WallSpan>();
            plan.Openings = new List<WallSpan>();
            plan.LivingOpen = false;
            int[] used = new int[plan.Rooms.Count];

            // comodos privados tem preferencia d porta no corredor depois vem qualquer ligacao
            foreach (RoomGraph.Edge edge in plan.Edges.OrderByDescending(e => Priority(plan, e)))
            {
                if (IsFull(plan, used, edge.A) || IsFull(plan, used, edge.B))
                    continue;

                if (openLivingHall && IsLivingHall(plan, edge) && edge.Wall.Length > s.DoorWidth)
                {
                    plan.Openings.AddRange(AllWalls(plan.Rooms[edge.A], plan.Rooms[edge.B]));
                    plan.LivingOpen = true;
                }
                else
                    doors.Add(edge.Wall.Centered(s.DoorWidth));

                used[edge.A]++;
                used[edge.B]++;
            }

            OpenSmallGaps(plan);

            return doors;
        }

        private static void OpenSmallGaps(FloorPlan plan)
        {
            Room living = plan.Find(RoomType.Living);
            Room hall = plan.Find(RoomType.Hall);

            if (living == null || hall == null)
                return;

            int li = plan.Rooms.IndexOf(living);
            int hi = plan.Rooms.IndexOf(hall);
            CellMap map = new CellMap(plan);

            for (int x = 0; x <= plan.Bounds.Width; x++)
                OpenSingleRuns(plan, true, x, plan.Bounds.Height,
                               y => Between(map.At(x - 1, y), map.At(x, y), li, hi));

            for (int y = 0; y <= plan.Bounds.Height; y++)
                OpenSingleRuns(plan, false, y, plan.Bounds.Width,
                               x => Between(map.At(x, y - 1), map.At(x, y), li, hi));
        }

        private static void OpenSingleRuns(FloorPlan plan, bool vertical, int line, int length,
                                           System.Func<int, bool> isWall)
        {
            int run = 0;

            for (int i = 0; i <= length; i++)
            {
                if (i < length && isWall(i))
                {
                    run++;
                    continue;
                }

                if (run == 1)
                {
                    WallSpan gap = new WallSpan(vertical, line, i - 1, 1);

                    if (!plan.Openings.Contains(gap))
                        plan.Openings.Add(gap);
                }

                run = 0;
            }
        }

        private static bool Between(int a, int b, int first, int second)
        {
            return (a == first && b == second) || (a == second && b == first);
        }

        private static List<WallSpan> AllWalls(Room a, Room b)
        {
            List<WallSpan> walls = new List<WallSpan>();

            foreach (Area partA in a.Parts)
                foreach (Area partB in b.Parts)
                {
                    WallSpan wall = Adjacency.Wall(partA, partB);

                    if (wall.Length > 0)
                        walls.Add(wall);
                }

            return walls;
        }

        private static bool IsLivingHall(FloorPlan plan, RoomGraph.Edge edge)
        {
            RoomType a = plan.Rooms[edge.A].Type;
            RoomType b = plan.Rooms[edge.B].Type;

            return (a == RoomType.Living && b == RoomType.Hall)
                || (a == RoomType.Hall && b == RoomType.Living);
        }

        private static int Priority(FloorPlan plan, RoomGraph.Edge edge)
        {
            Room a = plan.Rooms[edge.A];
            Room b = plan.Rooms[edge.B];

            int score = edge.Wall.Length;

            bool toHall = a.Type == RoomType.Hall || b.Type == RoomType.Hall;

            if ((a.IsPrivate || b.IsPrivate) && toHall)
                score += HallBonus;
            else if (a.IsCirculation || b.IsCirculation)
                score += CirculationBonus;

            return score;
        }

        private static bool IsFull(FloorPlan plan, int[] used, int index)
        {
            int limit = DoorLimit(plan.Rooms[index].Type);

            return limit >= 0 && used[index] >= limit;
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
    }
}