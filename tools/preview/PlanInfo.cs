using System;
using System.Collections.Generic;
using System.Text;
using PCG.Generation;

namespace PCG.Preview
{
    // informacoes derivadas de uma planta, usadas pelo console e pela bateria
    public static class PlanInfo
    {
        // em qual fachada a porta da rua ficou
        public static string EntranceSide(FloorPlan plan)
        {
            if (!plan.Entrance.HasValue)
                return "nenhuma";

            WallSpan door = plan.Entrance.Value;
            Area b = plan.Bounds;

            if (door.Vertical && door.Line == b.X) return "oeste";
            if (door.Vertical && door.Line == b.Right) return "leste";
            if (!door.Vertical && door.Line == b.Y) return "norte";
            if (!door.Vertical && door.Line == b.Bottom) return "sul";

            return "interna";
        }

        public const double CellSize = 0.5;

        public static double SquareMeters(int cells)
        {
            return cells * CellSize * CellSize;
        }

        public static string TypeName(RoomType type)
        {
            switch (type)
            {
                case RoomType.Living:   return "sala";
                case RoomType.Kitchen:  return "cozinha";
                case RoomType.Bathroom: return "banheiro";
                case RoomType.Hall:     return "corredor";
                default:                return "quarto";
            }
        }

        public static Area Box(Room room)
        {
            int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;

            foreach (Area part in room.Parts)
            {
                left = Math.Min(left, part.X);
                top = Math.Min(top, part.Y);
                right = Math.Max(right, part.Right);
                bottom = Math.Max(bottom, part.Bottom);
            }

            return new Area(left, top, right - left, bottom - top);
        }

        public static double Proportion(Room room)
        {
            Area box = Box(room);

            return (double)Math.Max(box.Width, box.Height) / Math.Min(box.Width, box.Height);
        }

        public static int[] DoorsPerRoom(FloorPlan plan)
        {
            CellMap map = new CellMap(plan);
            int[] doors = new int[plan.Rooms.Count];

            foreach (WallSpan door in plan.Doors)
            {
                Sides(map, door, out int a, out int b);

                if (a >= 0) doors[a]++;
                if (b >= 0) doors[b]++;
            }

            return doors;
        }

        public static int[] NeighboursPerRoom(FloorPlan plan)
        {
            int[] neighbours = new int[plan.Rooms.Count];

            foreach (RoomGraph.Edge edge in plan.Edges)
            {
                neighbours[edge.A]++;
                neighbours[edge.B]++;
            }

            return neighbours;
        }

        public static int[] Depths(FloorPlan plan)
        {
            CellMap map = new CellMap(plan);
            int count = plan.Rooms.Count;

            List<int>[] doorLinks = new List<int>[count];
            List<int>[] openLinks = new List<int>[count];

            for (int i = 0; i < count; i++)
            {
                doorLinks[i] = new List<int>();
                openLinks[i] = new List<int>();
            }

            int[] depth = new int[count];

            for (int i = 0; i < count; i++)
                depth[i] = -1;

            LinkedList<int> queue = new LinkedList<int>();

            foreach (WallSpan door in plan.Doors)
            {
                Sides(map, door, out int a, out int b);

                if (a >= 0 && b >= 0)
                {
                    doorLinks[a].Add(b);
                    doorLinks[b].Add(a);
                }
                else if (a >= 0 || b >= 0)
                {
                    int inside = a >= 0 ? a : b;

                    if (depth[inside] < 0)
                    {
                        depth[inside] = 1;
                        queue.AddLast(inside);
                    }
                }
            }

            foreach (WallSpan opening in plan.Openings)
            {
                Sides(map, opening, out int a, out int b);

                if (a >= 0 && b >= 0)
                {
                    openLinks[a].Add(b);
                    openLinks[b].Add(a);
                }
            }

            while (queue.Count > 0)
            {
                int room = queue.First.Value;
                queue.RemoveFirst();

                foreach (int next in openLinks[room])
                    if (depth[next] < 0 || depth[next] > depth[room])
                    {
                        depth[next] = depth[room];
                        queue.AddFirst(next);
                    }

                foreach (int next in doorLinks[room])
                    if (depth[next] < 0 || depth[next] > depth[room] + 1)
                    {
                        depth[next] = depth[room] + 1;
                        queue.AddLast(next);
                    }
            }

            return depth;
        }

        private static void Sides(CellMap map, WallSpan span, out int a, out int b)
        {
            WallKey key = span.Vertical
                ? new WallKey(true, span.Line, span.Start)
                : new WallKey(false, span.Start, span.Line);

            a = key.Vertical ? map.At(key.X - 1, key.Y) : map.At(key.X, key.Y - 1);
            b = map.At(key.X, key.Y);
        }

        // area do maior banheiro, em celulas
        public static int LargestBathroom(FloorPlan plan)
        {
            int largest = 0;

            foreach (Room room in plan.Rooms)
                if (room.Type == RoomType.Bathroom && room.Size > largest)
                    largest = room.Size;

            return largest;
        }

        // texto que identifica a planta pelo conteudo de cada celula.
        // duas plantas com os mesmos comodos nos mesmos lugares dao o
        // mesmo texto, mesmo que tenham vindo de sementes diferentes.
        public static string Signature(FloorPlan plan)
        {
            CellMap map = new CellMap(plan);
            StringBuilder sb = new StringBuilder();
            sb.Append(plan.Bounds.Width).Append('x').Append(plan.Bounds.Height).Append(':');

            for (int y = 0; y < plan.Bounds.Height; y++)
                for (int x = 0; x < plan.Bounds.Width; x++)
                    sb.Append((int)plan.Rooms[map.At(x, y)].Type);

            return sb.ToString();
        }
    }
}
