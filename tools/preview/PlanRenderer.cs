using System.Text;
using PCG.Generation;

namespace PCG.Preview
{
    // desenha a planta em texto. usa a mesma regra de parede da unity:
    // ha parede onde duas celulas vizinhas sao de comodos diferentes,
    // menos onde foi aberta uma porta.
    public static class PlanRenderer
    {
        public static string Render(FloorPlan plan)
        {
            CellMap map = new CellMap(plan);
            int width = plan.Bounds.Width;
            int height = plan.Bounds.Height;

            StringBuilder sb = new StringBuilder();

            for (int y = 0; y <= height; y++)
            {
                // linha das paredes horizontais, entre a fileira y-1 e a fileira y
                for (int x = 0; x < width; x++)
                {
                    bool wall = map.At(x, y - 1) != map.At(x, y)
                             && !plan.Doors.Contains(new WallKey(false, x, y));

                    sb.Append('+');
                    sb.Append(wall ? "--" : "  ");
                }

                sb.Append("+\n");

                if (y == height)
                    break;   // depois da ultima fileira so vem a parede de baixo

                // linha das celulas, com as paredes verticais entre elas.
                // vai ate x = width pra desenhar tambem a borda direita.
                for (int x = 0; x <= width; x++)
                {
                    bool wall = map.At(x - 1, y) != map.At(x, y)
                             && !plan.Doors.Contains(new WallKey(true, x, y));

                    sb.Append(wall ? '|' : ' ');

                    // cada celula ocupa 2 caracteres, porque letra
                    // de terminal e mais alta do que larga
                    if (x < width)
                        sb.Append(Letter(plan, map.At(x, y)), 2);
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }

        private static char Letter(FloorPlan plan, int roomIndex)
        {
            if (roomIndex == CellMap.Outside)
                return ' ';

            switch (plan.Rooms[roomIndex].Type)
            {
                case RoomType.Living:   return 'S';   // sala
                case RoomType.Kitchen:  return 'C';   // cozinha
                case RoomType.Bathroom: return 'B';   // banheiro
                case RoomType.Hall:     return 'H';   // corredor
                default:                return 'Q';   // quarto
            }
        }
    }
}
