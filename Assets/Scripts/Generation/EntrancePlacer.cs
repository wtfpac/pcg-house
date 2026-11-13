using System.Collections.Generic;

namespace PCG.Generation
{
    // escolhe onde fica a porta da rua. prefere a sala; (analisar se vamos continuar com ese trecho d poder ser no corredor)
    // se ela nao tocar fachada, tenta o corredor
    public static class EntrancePlacer
    {
        public static DoorPlacer.WallKey? Place(FloorPlan plan)
        {
            // primeiro tenta a sala, que e o normal em casa terrea
            DoorPlacer.WallKey? door = TryRoom(plan, RoomType.Living);

            if (door.HasValue)
                return door;

            // sem sala na fachada, cai pro corredor
            return TryRoom(plan, RoomType.Hall);
        }

        private static DoorPlacer.WallKey? TryRoom(FloorPlan plan, RoomType type)
        {
            int index = FindByType(plan, type);

            if (index < 0)
                return null;

            Area b = plan.Bounds;
            List<DoorPlacer.WallKey> options = new List<DoorPlacer.WallKey>();

            // junta as celulas de fachada de todas as partes do comodo
            foreach (Area a in plan.Rooms[index].Parts)
            {
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
            }

            if (options.Count == 0)
                return null;

            // pega a do meio, pra nao cair bem no canto
            return options[options.Count / 2];
        }

        private static int FindByType(FloorPlan plan, RoomType type)
        {
            for (int i = 0; i < plan.Rooms.Count; i++)
                if (plan.Rooms[i].Type == type)
                    return i;

            return -1;
        }
    }
}