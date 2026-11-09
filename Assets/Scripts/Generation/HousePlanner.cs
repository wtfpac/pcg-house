using System.Collections.Generic;

namespace PCG.Generation
{
    // monta uma planta completa: reserva o corredor, corta o resto,
    // nomeia e valida. tenta de novo se a planta nao passar nas regras.
    public static class HousePlanner
    {
        public const int MaxAttempts = 60;

        public static int LastAttempts { get; private set; }
        public static PlanValidator.Failure LastFailure { get; private set; }

        public static FloorPlan Build(Area bounds, int seed, int minRoomSize,
                                      int minRooms, int maxRooms, int minShared,
                                      int corridorWidth)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                // semente derivada, pra mesma entrada dar sempre o mesmo resultado
                int trySeed = seed + attempt * 7919;
                System.Random rng = new System.Random(trySeed);

                FloorPlan plan = TryBuild(bounds, seed, trySeed, rng, minRoomSize,
                                          minRooms, maxRooms, corridorWidth);

                if (plan == null)
                    continue;   // a faixa nao coube nesta tentativa

                PlanValidator.Failure result = PlanValidator.Check(plan, minShared);

                if (result == PlanValidator.Failure.None)
                {
                    LastAttempts = attempt + 1;
                    return plan;
                }

                LastFailure = result;
            }

            LastAttempts = MaxAttempts;
            return null;
        }

        private static FloorPlan TryBuild(Area bounds, int seed, int trySeed,
                                          System.Random rng, int minRoomSize,
                                          int minRooms, int maxRooms, int corridorWidth)
        {
            // orienta a faixa pelo lado mais comprido, mas as vezes inverte.
            // so inverte se o outro sentido tambem comportar comodos dos dois lados.
            bool horizontal = bounds.Width >= bounds.Height;

            int otherSpan = horizontal ? bounds.Width : bounds.Height;

            if (otherSpan >= corridorWidth + minRoomSize * 2 && rng.Next(3) == 0)
                horizontal = !horizontal;
                
            int span = horizontal ? bounds.Height : bounds.Width;

            // precisa sobrar comodo dos dois lados da faixa
            int minPos = minRoomSize;
            int maxPos = span - corridorWidth - minRoomSize;

            if (maxPos < minPos)
                return null;   // casa estreita demais pra ter corredor

            int pos = rng.Next(minPos, maxPos + 1);

            Area corridor;
            Area regionA;
            Area regionB;

            if (horizontal)
            {
                corridor = new Area(bounds.X, bounds.Y + pos, bounds.Width, corridorWidth);
                regionA = new Area(bounds.X, bounds.Y, bounds.Width, pos);
                regionB = new Area(bounds.X, corridor.Bottom,
                                   bounds.Width, bounds.Bottom - corridor.Bottom);
            }
            else
            {
                corridor = new Area(bounds.X + pos, bounds.Y, corridorWidth, bounds.Height);
                regionA = new Area(bounds.X, bounds.Y, pos, bounds.Height);
                regionB = new Area(corridor.Right, bounds.Y,
                                   bounds.Right - corridor.Right, bounds.Height);
            }

            // corta as duas regioes, proporcional ao tamanho de cada uma
            int totalRooms = rng.Next(minRooms, maxRooms + 1);
            int roomsA = AreaShare(regionA, regionB, totalRooms);
            int roomsB = totalRooms - roomsA;

            HouseGenerator genA = new HouseGenerator(minRoomSize, roomsA, trySeed);
            HouseGenerator genB = new HouseGenerator(minRoomSize, roomsB, trySeed + 1);

            List<Area> areas = new List<Area>();
            areas.AddRange(genA.SplitSpace(regionA));
            areas.AddRange(genB.SplitSpace(regionB));

            List<Room> rooms = RoomLabeler.Label(areas, 2);

            FloorPlan plan = new FloorPlan(bounds, seed);
            plan.Rooms.Add(new Room(corridor, RoomType.Hall));
            plan.Rooms.AddRange(rooms);

            // tenta encurtar o corredor numa das pontas
            CorridorTrimmer.Trim(plan.Rooms, rng, minRoomSize);

            return plan;
        }

        // divide os comodos entre as duas regioes conforme a area de cada uma
        private static int AreaShare(Area a, Area b, int total)
        {
            int sum = a.Size + b.Size;

            if (sum <= 0)
                return 1;

            int share = (int)System.Math.Round((double)total * a.Size / sum);

            if (share < 1) share = 1;
            if (share > total - 1) share = total - 1;

            return share;
        }
    }
}