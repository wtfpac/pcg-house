using System.Collections.Generic;

namespace PCG.Generation
{
    // escolhe onde fica a porta da rua. sempre na sala,
    // numa parede externa, no meio do trecho disponivel.
    public static class EntrancePlacer
    {
        // devolve a celula de parede que vira entrada,
        // ou null se a sala nao tocar nenhuma fachada
        public static DoorPlacer.WallKey? Place(FloorPlan plan)
        {
            int living = FindLiving(plan);

            if (living < 0)
                return null;

            Area a = plan.Rooms[living].Area;
            Area b = plan.Bounds;

            // junta todas as celulas de parede externa da sala
            List<DoorPlacer.WallKey> options = new List<DoorPlacer.WallKey>();

            if (a.X == b.X)
                for (int y = a.Y; y < a.Bottom; y++)
                    options.Add(new DoorPlacer.WallKey(true, a.X, y));

            if (a.Right == b.Right)
                for (int y = a.Y; y < a.Bottom; y++)
                    options.Add(new DoorPlacer.WallKey(true, a.Right, y));

            if (a.Y == b.Y)
                for (int x = a.X; x < a.Right; x++)
                    options.Add(new DoorPlacer.WallKey(false, x, a.Y));

            if (a.Bottom == b.Bottom)
                for (int x = a.X; x < a.Right; x++)
                    options.Add(new DoorPlacer.WallKey(false, x, a.Bottom));

            if (options.Count == 0)
                return null;

            // pega a do meio da lista, pra nao cair no canto
            return options[options.Count / 2];
        }

        private static int FindLiving(FloorPlan plan)
        {
            for (int i = 0; i < plan.Rooms.Count; i++)
                if (plan.Rooms[i].Type == RoomType.Living)
                    return i;

            return -1;
        }
    }
}