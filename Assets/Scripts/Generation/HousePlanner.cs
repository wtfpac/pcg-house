using System.Collections.Generic;
using System.Diagnostics;

namespace PCG.Generation
{
    // monta uma planta completa: reserva o corredor, corta o resto nomeia e valida
    public static class HousePlanner
    {
        public const string Version = "0.6";

        // faixa das sementes sorteadas 900 milhoes
        public const int MinSeed = 100000000;
        public const int MaxSeed = 999999999;

        private const int FlipOdds = 3;

        public static GenerationResult Generate(HouseSettings s, int seed)
        {
            GenerationResult result = new GenerationResult(seed);
            Stopwatch watch = Stopwatch.StartNew();

            System.Random sizeRng = new System.Random(unchecked(seed * 486187739 + 7));
            result.Width = sizeRng.Next(s.MinWidth, s.MaxWidth + 1);
            result.Height = sizeRng.Next(s.MinHeight, s.MaxHeight + 1);
            Area bounds = new Area(0, 0, result.Width, result.Height);

            for (int attempt = 0; attempt < s.MaxAttempts; attempt++)
            {
                result.Attempts = attempt + 1;

                int trySeed = seed + attempt * 7919;

                FloorPlan plan = BuildCandidate(s, bounds, seed, trySeed);

                Failure failure = plan == null
                    ? Failure.Geometry
                    : PlanValidator.Check(plan, s);

                if (failure == Failure.None)
                {
                    PlaceDoors(plan, s);
                    result.Plan = plan;
                    break;
                }

                result.Count(failure);
            }

            watch.Stop();
            result.Milliseconds = watch.Elapsed.TotalMilliseconds;

            return result;
        }

        private static FloorPlan BuildCandidate(HouseSettings s, Area bounds, int seed, int trySeed)
        {
            System.Random rng = new System.Random(trySeed);

            if (!PlaceCorridor(bounds, s, rng, out Area corridor,
                               out Area regionA, out Area regionB))
                return null;

            // corta as duas regioes, proporcional ao tamanho de cada uma
            int totalRooms = rng.Next(s.MinRooms, s.MaxRooms + 1);
            int roomsA = ShareOf(regionA, regionB, totalRooms);
            int roomsB = totalRooms - roomsA;

            List<Area> areas = new List<Area>();
            areas.AddRange(BspSplitter.Split(regionA, roomsA, s.MinRoomSize, rng));
            areas.AddRange(BspSplitter.Split(regionB, roomsB, s.MinRoomSize, rng));

            FloorPlan plan = new FloorPlan(bounds, seed);
            plan.Rooms.Add(new Room(corridor, RoomType.Hall));
            plan.Rooms.AddRange(RoomLabeler.Label(areas, s));

            // tenta encurtar o corredor nas pontas
            CorridorTrimmer.Trim(plan, s, rng);

            // banheiro de no maximo 20 celulas (5 m2), lado minimo de 4 (2 m)
            BathroomShrinker.Shrink(plan, s);

            return plan;
        }

        // sorteia a posicao da faixa do corredor
        private static bool PlaceCorridor(Area bounds, HouseSettings s, System.Random rng,
                                          out Area corridor, out Area regionA, out Area regionB)
        {
            corridor = regionA = regionB = bounds;

            bool alongX = bounds.Width >= bounds.Height;
            int longSide = alongX ? bounds.Width : bounds.Height;

            if (longSide >= s.CorridorWidth + s.MinRoomSize * 2 && rng.Next(FlipOdds) == 0)
                alongX = !alongX;

            int depth = alongX ? bounds.Height : bounds.Width;
            int minPos = s.MinRoomSize;
            int maxPos = depth - s.CorridorWidth - s.MinRoomSize;

            if (maxPos < minPos)
                return false;   // casa estreita demais pra ter corredor

            int pos = rng.Next(minPos, maxPos + 1);

            // corredor no eixo X significa cortes no eixo Y, e vice-versa
            bounds.Split(!alongX, pos, out regionA, out Area rest);
            rest.Split(!alongX, s.CorridorWidth, out corridor, out regionB);

            return true;
        }

        // divide os comodos entre as duas regioes conforme a area de cada uma
        private static int ShareOf(Area a, Area b, int total)
        {
            int share = (int)System.Math.Round((double)total * a.Size / (a.Size + b.Size));

            if (share < 1) share = 1;
            if (share > total - 1) share = total - 1;

            return share;
        }

        // com a planta aprovada: monta o grafo e abre as portas
        private static void PlaceDoors(FloorPlan plan, HouseSettings s)
        {
            plan.Edges = RoomGraph.Build(plan.Rooms, s.DoorWall);
            plan.Doors = DoorPlacer.Place(plan, s.DoorWidth);
            plan.Entrance = EntrancePlacer.Place(plan, s.DoorWidth);

            if (plan.Entrance.HasValue)
                plan.Doors.Add(plan.Entrance.Value);
        }
    }
}