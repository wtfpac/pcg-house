using System.Collections.Generic;
using System.Text;
using PCG.Generation;

namespace PCG.Preview
{
    // desenha a planta em texto, com os vaos de porta marcados
    public static class PlanRenderer
    {
        public static string Render(FloorPlan plan, HashSet<DoorPlacer.WallKey> doors)
        {
            CellMap map = new CellMap(plan);
            Area b = plan.Bounds;

            StringBuilder sb = new StringBuilder();

            for (int y = 0; y < b.Height; y++)
            {
                sb.Append(HorizontalLine(map, b, y, doors));
                sb.Append('\n');

                for (int x = 0; x < b.Width; x++)
                {
                    bool wall = x == 0 || map.At(x, y) != map.At(x - 1, y);
                    bool door = doors.Contains(new DoorPlacer.WallKey(true, x, y));

                    sb.Append(wall && !door ? '|' : ' ');

                    char c = Letter(plan, map.At(x, y));
                    sb.Append(c);
                    sb.Append(c);
                }

                // borda direita da casa, pode ter entrada
                bool rightDoor = doors.Contains(new DoorPlacer.WallKey(true, b.Width, y));
                sb.Append(rightDoor ? ' ' : '|');
                sb.Append('\n');
            }

            sb.Append(HorizontalLine(map, b, b.Height, doors));

            return sb.ToString();
        }

        private static string HorizontalLine(CellMap map, Area b, int y,
                                             HashSet<DoorPlacer.WallKey> doors)
        {
            StringBuilder sb = new StringBuilder();

            for (int x = 0; x < b.Width; x++)
            {
                bool wall = y == 0 || y == b.Height || map.At(x, y) != map.At(x, y - 1);
                bool door = doors.Contains(new DoorPlacer.WallKey(false, x, y));

                sb.Append('+');
                sb.Append(wall && !door ? "--" : "  ");
            }

            sb.Append('+');

            return sb.ToString();
        }

        private static char Letter(FloorPlan plan, int index)
        {
            if (index == CellMap.Outside)
                return ' ';

            switch (plan.Rooms[index].Type)
            {
                case RoomType.Living:   return 'S';
                case RoomType.Kitchen:  return 'C';
                case RoomType.Bathroom: return 'B';
                case RoomType.Hall:     return 'H';
                default:                return 'Q';
            }
        }
    }
}