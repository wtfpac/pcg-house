using System.Collections.Generic;
using System.Linq;

namespace PCG.Generation
{
    public static class DoorPlacer
    {
        // pesos da ordenacao. a parede em comum soma no final, como desempate.
        private const int HallBonus = 3000;
        private const int CirculationBonus = 1000;

        public static List<WallSpan> Place(FloorPlan plan, HouseSettings s, bool openLivingHall)
        {
            List<WallSpan> doors = new List<WallSpan>();
            plan.Openings = new List<WallSpan>();
            int[] used = new int[plan.Rooms.Count];

            // comodos privados tem preferencia d porta no corredor
            // depois vem qualquer ligacao com circulacao
            foreach (RoomGraph.Edge edge in plan.Edges.OrderByDescending(e => Priority(plan, e)))
            {
                if (IsFull(plan, used, edge.A) || IsFull(plan, used, edge.B))
                    continue;

                if (openLivingHall && IsLivingHall(plan, edge) && edge.Wall.Length > s.DoorWidth)
                    plan.Openings.AddRange(AllWalls(plan.Rooms[edge.A], plan.Rooms[edge.B]));
                else
                    doors.Add(edge.Wall.Centered(s.DoorWidth));

                used[edge.A]++;
                used[edge.B]++;
            }

            return doors;
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
