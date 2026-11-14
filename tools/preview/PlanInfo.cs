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

            WallKey door = plan.Entrance.Value;
            Area b = plan.Bounds;

            if (door.Vertical && door.X == b.X) return "oeste";
            if (door.Vertical && door.X == b.Right) return "leste";
            if (!door.Vertical && door.Y == b.Y) return "norte";
            if (!door.Vertical && door.Y == b.Bottom) return "sul";

            return "interna";
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

            for (int y = 0; y < plan.Bounds.Height; y++)
                for (int x = 0; x < plan.Bounds.Width; x++)
                    sb.Append((int)plan.Rooms[map.At(x, y)].Type);

            return sb.ToString();
        }
    }
}
