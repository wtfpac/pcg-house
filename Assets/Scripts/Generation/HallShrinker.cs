using System.Collections.Generic;

namespace PCG.Generation
{
    // reduz o hall pra largura de corredor e devolve o espaco liberado ao comodo vizinho
    public static class HallShrinker
    {
        public static void Shrink(List<Room> rooms, int corridorWidth, int minShared)
        {
            int hall = FindHall(rooms);

            if (hall < 0)
                return;

            Area a = rooms[hall].Area;

            // encolhe no sentido do lado mais comprido
            bool shrinkWidth = a.Width >= a.Height;

            if (shrinkWidth && a.Width <= corridorWidth)
                return;

            if (!shrinkWidth && a.Height <= corridorWidth)
                return;

            // sorteio nao: encolhe pro lado que tem vizinho pra receber
            if (shrinkWidth)
                ShrinkHorizontal(rooms, hall, corridorWidth, minShared);
            else
                ShrinkVertical(rooms, hall, corridorWidth, minShared);
        }

        private static int FindHall(List<Room> rooms)
        {
            for (int i = 0; i < rooms.Count; i++)
                if (rooms[i].Type == RoomType.Hall)
                    return i;

            return -1;
        }

        private static void ShrinkHorizontal(List<Room> rooms, int hall,
                                             int corridorWidth, int minShared)
        {
            Area a = rooms[hall].Area;
            int freed = a.Width - corridorWidth;

            List<int> right = FindNeighboursAt(rooms, hall, a.Right, true, minShared);

            if (right.Count > 0)
            {
                rooms[hall].Area = new Area(a.X, a.Y, corridorWidth, a.Height);

                foreach (int i in right)
                {
                    Area n = rooms[i].Area;
                    rooms[i].Area = new Area(n.X - freed, n.Y, n.Width + freed, n.Height);
                }

                return;
            }

            List<int> left = FindNeighboursAt(rooms, hall, a.X, true, minShared);

            if (left.Count > 0)
            {
                rooms[hall].Area = new Area(a.Right - corridorWidth, a.Y, corridorWidth, a.Height);

                foreach (int i in left)
                {
                    Area n = rooms[i].Area;
                    rooms[i].Area = new Area(n.X, n.Y, n.Width + freed, n.Height);
                }
            }
        }

        private static void ShrinkVertical(List<Room> rooms, int hall, int corridorWidth, int minShared)
        {
            Area a = rooms[hall].Area;
            int freed = a.Height - corridorWidth;

            List<int> below = FindNeighboursAt(rooms, hall, a.Bottom, false, minShared);

            if (below.Count > 0)
            {
                rooms[hall].Area = new Area(a.X, a.Y, a.Width, corridorWidth);

                foreach (int i in below)
                {
                    Area n = rooms[i].Area;
                    rooms[i].Area = new Area(n.X, n.Y - freed, n.Width, n.Height + freed);
                }

                return;
            }

            List<int> above = FindNeighboursAt(rooms, hall, a.Y, false, minShared);

            if (above.Count > 0)
            {
                rooms[hall].Area = new Area(a.X, a.Bottom - corridorWidth, a.Width, corridorWidth);

                foreach (int i in above)
                {
                    Area n = rooms[i].Area;
                    rooms[i].Area = new Area(n.X, n.Y, n.Width, n.Height + freed);
                }
            }
        }

        // todos os comodos que encostam na linha
        private static List<int> FindNeighboursAt(List<Room> rooms, int skip, int line,
                                                  bool vertical, int minShared)
        {
            List<int> found = new List<int>();
            Area h = rooms[skip].Area;

            for (int i = 0; i < rooms.Count; i++)
            {
                if (i == skip)
                    continue;

                Area n = rooms[i].Area;

                if (vertical)
                {
                    if ((n.X == line || n.Right == line)
                        && Adjacency.Overlap(h.Y, h.Bottom, n.Y, n.Bottom) >= minShared)
                        found.Add(i);
                }
                else
                {
                    if ((n.Y == line || n.Bottom == line)
                        && Adjacency.Overlap(h.X, h.Right, n.X, n.Right) >= minShared)
                        found.Add(i);
                }
            }

            return found;
        }
    }
}