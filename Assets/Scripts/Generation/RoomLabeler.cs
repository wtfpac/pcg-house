using System.Collections.Generic;

namespace PCG.Generation
{
    // decide o tipo dos comodos. o hall ja vem marcado pelo planner,
    // entao aqui so nomeamos sala, cozinha, banheiro e quartos.
    public static class RoomLabeler
    {
        public static List<Room> Label(List<Area> areas, HouseSettings s)
        {
            // todo mundo comeca como quarto. os outros tipos sao escolhidos
            // um a um, sempre entre os que ainda sao quarto.
            List<Room> rooms = new List<Room>();

            foreach (Area area in areas)
                rooms.Add(new Room(area, RoomType.Bedroom));

            // sala: a mais conectada. empate vai pra maior.
            Room living = PickLiving(rooms, s.MinSharedWall);
            living.Type = RoomType.Living;

            // banheiro: o menor dos que sobraram
            Room bathroom = SmallestBedroom(rooms, null, s.MinSharedWall);

            if (bathroom != null)
                bathroom.Type = RoomType.Bathroom;

            // cozinha: a maior das que sobraram, de preferencia vizinha da sala
            Room kitchen = PickKitchen(rooms, living, s.MinSharedWall);

            if (kitchen != null)
                kitchen.Type = RoomType.Kitchen;

            // segundo banheiro so em casa grande, e nunca colado no primeiro
            if (rooms.Count >= s.SecondBathroomAt)
            {
                Room second = SmallestBedroom(rooms, bathroom, s.MinSharedWall);

                if (second != null)
                    second.Type = RoomType.Bathroom;
            }

            return rooms;
        }

        private static Room PickLiving(List<Room> rooms, int minShared)
        {
            Room best = rooms[0];
            int bestCount = CountNeighbours(rooms, best, minShared);

            foreach (Room room in rooms)
            {
                int count = CountNeighbours(rooms, room, minShared);

                if (count > bestCount || (count == bestCount && room.Size > best.Size))
                {
                    best = room;
                    bestCount = count;
                }
            }

            return best;
        }

        private static Room PickKitchen(List<Room> rooms, Room living, int minShared)
        {
            Room best = null;
            bool bestTouches = false;

            foreach (Room room in rooms)
            {
                if (room.Type != RoomType.Bedroom)
                    continue;

                bool touches = Adjacency.AreNeighbours(room, living, minShared);

                // vizinha da sala ganha; entre duas vizinhas, a maior
                bool better = best == null
                           || (touches && !bestTouches)
                           || (touches == bestTouches && room.Size > best.Size);

                if (better)
                {
                    best = room;
                    bestTouches = touches;
                }
            }

            return best;
        }

        // o menor comodo que ainda e quarto. se "awayFrom" for informado,
        // descarta quem encosta nele. devolve null se nao sobrar ninguem.
        private static Room SmallestBedroom(List<Room> rooms, Room awayFrom, int minShared)
        {
            Room best = null;

            foreach (Room room in rooms)
            {
                if (room.Type != RoomType.Bedroom)
                    continue;

                if (awayFrom != null && Adjacency.AreNeighbours(room, awayFrom, minShared))
                    continue;

                if (best == null || room.Size < best.Size)
                    best = room;
            }

            return best;
        }

        private static int CountNeighbours(List<Room> rooms, Room room, int minShared)
        {
            int count = 0;

            foreach (Room other in rooms)
                if (other != room && Adjacency.AreNeighbours(room, other, minShared))
                    count++;

            return count;
        }
    }
}
