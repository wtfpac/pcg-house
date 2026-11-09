using System.Collections.Generic;

namespace PCG.Generation
{
    // decide o tipo dos comodos. o hall ja vem marcado pelo planner,
    // entao aqui so nomeamos sala, cozinha, banheiro e quartos.
    public static class RoomLabeler
    {
        public static List<Room> Label(List<Area> areas, int minShared)
        {
            int[] neighbours = CountNeighbours(areas, minShared);

            // sala: a mais conectada. empate vai pra maior.
            int living = 0;

            for (int i = 1; i < areas.Count; i++)
            {
                bool moreConnected = neighbours[i] > neighbours[living];
                bool tiedButBigger = neighbours[i] == neighbours[living]
                                     && areas[i].Size > areas[living].Size;

                if (moreConnected || tiedButBigger)
                    living = i;
            }

            // banheiro: o menor dos que sobraram
            int bathroom = PickSmallest(areas, living, -1, -1);

            // cozinha: a maior das que sobraram, de preferencia vizinha da sala
            int kitchen = PickKitchen(areas, living, bathroom, minShared);

            // segundo banheiro so em casa grande
            int bathroom2 = -1;

            if (areas.Count >= 6)
                bathroom2 = PickSmallest(areas, living, bathroom, kitchen);

            List<Room> rooms = new List<Room>();

            for (int i = 0; i < areas.Count; i++)
            {
                RoomType type = RoomType.Bedroom;

                if (i == living) type = RoomType.Living;
                else if (i == bathroom || i == bathroom2) type = RoomType.Bathroom;
                else if (i == kitchen) type = RoomType.Kitchen;

                rooms.Add(new Room(areas[i], type));
            }

            return rooms;
        }

        // menor retangulo ignorando os indices ja usados
        private static int PickSmallest(List<Area> areas, int skipA, int skipB, int skipC)
        {
            int best = -1;

            for (int i = 0; i < areas.Count; i++)
            {
                if (i == skipA || i == skipB || i == skipC)
                    continue;

                if (best < 0 || areas[i].Size < areas[best].Size)
                    best = i;
            }

            return best;
        }

        private static int PickKitchen(List<Area> areas, int living, int bathroom, int minShared)
        {
            int best = -1;

            for (int i = 0; i < areas.Count; i++)
            {
                if (i == living || i == bathroom)
                    continue;

                if (best < 0)
                {
                    best = i;
                    continue;
                }

                bool isNeighbour = Adjacency.AreNeighbours(areas[i], areas[living], minShared);
                bool bestIsNeighbour = Adjacency.AreNeighbours(areas[best], areas[living], minShared);

                // vizinha da sala ganha; entre duas vizinhas, a maior
                if (isNeighbour && !bestIsNeighbour)
                    best = i;
                else if (isNeighbour == bestIsNeighbour && areas[i].Size > areas[best].Size)
                    best = i;
            }

            return best;
        }

        private static int[] CountNeighbours(List<Area> areas, int minShared)
        {
            int[] count = new int[areas.Count];

            for (int i = 0; i < areas.Count; i++)
                for (int j = i + 1; j < areas.Count; j++)
                    if (Adjacency.AreNeighbours(areas[i], areas[j], minShared))
                    {
                        count[i]++;
                        count[j]++;
                    }

            return count;
        }
    }
}