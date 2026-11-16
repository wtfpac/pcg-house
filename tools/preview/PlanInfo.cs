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
