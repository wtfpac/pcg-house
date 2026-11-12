using System.Collections.Generic;
using System.Diagnostics;

namespace PCG.Generation
{
    // o gerador em si monta uma planta em etapas: corredor, BSP, tipos,
    // ajustes, e testa contra as regras. se a planta for reprovada, tenta
    // outra. chamamos de generate-and-test.
    public static class HousePlanner
    {
        // versao do algoritmo. subir sempre que a geracao mudar de comportamento,
        // pra saber qual codigo produziu cada print e cada tabela de resultados
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

                // cada tentativa usa uma semente derivada da original. assim a
                // mesma semente de entrada sempre chega na mesma casa
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

        // monta uma planta candidata. devolve null se o corredor nao couber
        private static FloorPlan BuildCandidate(HouseSettings s, int seed, int trySeed)
        {
            // um unico gerador aleatorio por tentativa, usado por todas as etapas
            System.Random rng = new System.Random(trySeed);
            Area bounds = new Area(0, 0, s.Width, s.Height);

            // 1. reserva a faixa do corredor, que divide a casa em duas regioes
            if (!PlaceCorridor(bounds, s, rng, out Area corridor,
                               out Area regionA, out Area regionB))
                return null;

            // 2. corta as duas regioes com BSP. cada uma recebe uma
            //    quantidade de comodos proporcional a sua area.
            int totalRooms = rng.Next(s.MinRooms, s.MaxRooms + 1);
            int roomsA = ShareOf(regionA, regionB, totalRooms);
            int roomsB = totalRooms - roomsA;

            List<Area> areas = new List<Area>();
            areas.AddRange(BspSplitter.Split(regionA, roomsA, s.MinRoomSize, rng));
            areas.AddRange(BspSplitter.Split(regionB, roomsB, s.MinRoomSize, rng));

            // 3. decide o tipo de cada comodo. o corredor ja entra marcado.
            FloorPlan plan = new FloorPlan(bounds, seed);
            plan.Rooms.Add(new Room(corridor, RoomType.Hall));
            plan.Rooms.AddRange(RoomLabeler.Label(areas, s));

            // 4. ajustes de proporcao: corredor mais curto, banheiro menor
            CorridorTrimmer.Trim(plan, s, rng);
            BathroomShrinker.Shrink(plan, s);

            return plan;
        }

        // sorteia a posicao da faixa do corredor e separa a casa em
        // regiao A | corredor | regiao B. devolve false se nao houver espaco.
        private static bool PlaceCorridor(Area bounds, HouseSettings s, System.Random rng,
                                          out Area corridor, out Area regionA, out Area regionB)
        {
            corridor = regionA = regionB = bounds;

            // alongX = o corredor corre no eixo X. o normal e ele seguir o lado
            // mais comprido da casa. as vezes vai no outro sentido, desde que o
            // lado comprido comporte o corredor mais um comodo de cada lado.
            bool alongX = bounds.Width >= bounds.Height;
            int longSide = alongX ? bounds.Width : bounds.Height;

            if (longSide >= s.CorridorWidth + s.MinRoomSize * 2 && rng.Next(FlipOdds) == 0)
                alongX = !alongX;

            // "depth" e o lado que a faixa divide. precisa sobrar
            // um comodo minimo de cada lado dela.
            int depth = alongX ? bounds.Height : bounds.Width;
            int minPos = s.MinRoomSize;
            int maxPos = depth - s.CorridorWidth - s.MinRoomSize;

            if (maxPos < minPos)
                return false;

            int pos = rng.Next(minPos, maxPos + 1);

            // corredor no eixo X significa cortes no eixo Y, e vice-versa
            bounds.Split(!alongX, pos, out regionA, out Area rest);
            rest.Split(!alongX, s.CorridorWidth, out corridor, out regionB);

            return true;
        }

        // quantos comodos ficam na regiao A, proporcional a area dela.
        // cada regiao recebe pelo menos um
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
