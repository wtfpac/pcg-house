using System.Collections.Generic;
using System.Diagnostics;

namespace PCG.Generation
{
    // monta uma planta completa: reserva o corredor, corta o resto,
    // nomeia e valida. tenta de novo se a planta nao passar nas regras
    public static class HousePlanner
    {
        // versao do algoritmo. subir sempre que a geracao mudar de comportamento,
        // pra saber qual codigo produziu cada print e cada tabela de resultados.
        public const string Version = "0.5";

        // 1 em cada 3 vezes o corredor vai no sentido mais curto da casa
        private const int FlipOdds = 3;

        public static GenerationResult Generate(HouseSettings s, int seed)
        {
            GenerationResult result = new GenerationResult(seed);
            Stopwatch watch = Stopwatch.StartNew();

            for (int attempt = 0; attempt < s.MaxAttempts; attempt++)
            {
                result.Attempts = attempt + 1;

                // semente derivada, mesma entrada = msm resultado
                int trySeed = seed + attempt * 7919;

                FloorPlan plan = BuildCandidate(s, seed, trySeed);

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

        private static FloorPlan BuildCandidate(HouseSettings s, int seed, int trySeed)
        {
            // um unico gerador aleatorio por tentativa, usado por todas as etapas
            System.Random rng = new System.Random(trySeed);
            Area bounds = new Area(0, 0, s.Width, s.Height);

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

        // sorteia a posicao da faixa do corredor e separa a casa em
        // regiao A | corredor | regiao B. devolve false se nao houver espaco.
        private static bool PlaceCorridor(Area bounds, HouseSettings s, System.Random rng,
                                          out Area corridor, out Area regionA, out Area regionB)
        {
            corridor = regionA = regionB = bounds;

            // orienta a faixa pelo lado mais comprido, mas as vezes inverte.
            // so inverte se o outro sentido tambem comportar comodos dos dois lados.
            bool alongX = bounds.Width >= bounds.Height;
            int longSide = alongX ? bounds.Width : bounds.Height;

            if (longSide >= s.CorridorWidth + s.MinRoomSize * 2 && rng.Next(FlipOdds) == 0)
                alongX = !alongX;

            // precisa sobrar comodo dos dois lados da faixa
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
            plan.Edges = RoomGraph.Build(plan.Rooms, s.MinSharedWall);
            plan.Doors = DoorPlacer.Place(plan);
            plan.Entrance = EntrancePlacer.Place(plan);

            if (plan.Entrance.HasValue)
                plan.Doors.Add(plan.Entrance.Value);
        }
    }
}
