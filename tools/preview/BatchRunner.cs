using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PCG.Generation;

namespace PCG.Preview
{
    // bateria de testes: gera muitas casas com sementes sorteadas,
    // mostra um resumo no console e grava tudo num csv.
    public static class BatchRunner
    {
        // regras na ordem em que viram colunas do csv
        private static readonly Failure[] Rules =
        {
            Failure.Geometry, Failure.Gap, Failure.Overlap,
            Failure.Facade, Failure.Circulation, Failure.CirculationSplit, Failure.RoomSize
        };

        public static void Run(int count, HouseSettings settings)
        {
            // sementes sorteadas, sem repetir nenhuma
            Random picker = new Random();
            HashSet<int> usedSeeds = new HashSet<int>();

            // assinaturas das plantas ja vistas, pra achar casas repetidas
            HashSet<string> seenPlans = new HashSet<string>();

            StringBuilder csv = new StringBuilder();
            csv.AppendLine(Header());

            StringBuilder roomsCsv = new StringBuilder();
            roomsCsv.AppendLine(RoomHeader());

            // aquecimento: as primeiras geracoes sao lentas porque o c# ainda
            // esta compilando o codigo. elas nao entram na medicao.
            for (int i = 0; i < 50; i++)
                HousePlanner.Generate(settings, i);

            Summary summary = new Summary();
            Stopwatch total = Stopwatch.StartNew();

            while (usedSeeds.Count < count)
            {
                // o limite de cima do Next nao entra no sorteio, por isso o + 1
                int seed = picker.Next(HousePlanner.MinSeed, HousePlanner.MaxSeed + 1);

                if (!usedSeeds.Add(seed))
                    continue;   // ja saiu antes, sorteia outra

                GenerationResult result = HousePlanner.Generate(settings, seed);

                bool repeated = false;

                if (result.Success)
                    repeated = !seenPlans.Add(PlanInfo.Signature(result.Plan));

                csv.AppendLine(Row(result, settings, repeated));
                AppendRoomRows(roomsCsv, result);
                summary.Add(result, repeated);
            }

            total.Stop();

            string file = Save(csv, count, settings, "casas");
            string roomsFile = Save(roomsCsv, count, settings, "comodos");
            summary.Print(count, settings, total.Elapsed.TotalSeconds, file);
            Console.WriteLine("csv dos comodos salvo em: " + Path.GetFullPath(roomsFile));
        }

        private static string Header()
        {
            List<string> cols = new List<string>
            {
                "versao", "largura", "altura", "semente", "valida", "tentativas", "tempo_ms"
            };

            foreach (Failure rule in Rules)
                cols.Add("rejeicoes_" + rule);

            cols.AddRange(HouseColumns);

            return string.Join(";", cols);
        }

        // uma linha do csv. ponto e virgula e virgula decimal, que e o
        // formato que o excel em portugues abre direto
        private static string Row(GenerationResult r, HouseSettings s, bool repeated)
        {
            CultureInfo br = CultureInfo.GetCultureInfo("pt-BR");
            List<string> cols = new List<string>
            {
                HousePlanner.Version, r.Width.ToString(), r.Height.ToString(),
                r.Seed.ToString(), r.Success ? "1" : "0", r.Attempts.ToString(),
                r.Milliseconds.ToString("F4", br)
            };

            foreach (Failure rule in Rules)
            {
                int n;
                r.Failures.TryGetValue(rule, out n);
                cols.Add(n.ToString());
            }

            if (r.Success)
            {
                FloorPlan p = r.Plan;

                List<Room> closed = p.Rooms.Where(x => !x.IsCirculation).ToList();
                List<Room> bedrooms = p.Rooms.Where(x => x.Type == RoomType.Bedroom).ToList();
                List<Room> bathrooms = p.Rooms.Where(x => x.Type == RoomType.Bathroom).ToList();
                int[] depths = PlanInfo.Depths(p);

                cols.Add(M2(p.Bounds.Size, br));
                cols.Add(p.RoomCount.ToString());
                cols.Add(bedrooms.Count.ToString());
                cols.Add(bathrooms.Count.ToString());
                cols.Add(p.CountOf(RoomType.Hall).ToString());
                cols.Add(p.LivingOpen ? "1" : "0");
                cols.Add(M2(SizeOf(p, RoomType.Living), br));
                cols.Add(M2(SizeOf(p, RoomType.Kitchen), br));
                cols.Add(M2(SizeOf(p, RoomType.Hall), br));
                cols.Add(bedrooms.Count > 0 ? M2(bedrooms.Min(x => x.Size), br) : "");
                cols.Add(bedrooms.Count > 0 ? M2(bedrooms.Average(x => x.Size), br) : "");
                cols.Add(bedrooms.Count > 0 ? M2(bedrooms.Max(x => x.Size), br) : "");
                cols.Add(bathrooms.Count > 0 ? M2(bathrooms.Min(x => x.Size), br) : "");
                cols.Add(bathrooms.Count > 0 ? M2(bathrooms.Max(x => x.Size), br) : "");
                cols.Add(M2(closed.Max(x => x.Size), br));
                cols.Add(M2(closed.Min(x => x.Size), br));
                cols.Add(p.Rooms.Count(x => x.Parts.Count > 1).ToString());
                cols.Add(p.Rooms.Where(x => x.Type != RoomType.Hall).Max(x => PlanInfo.Proportion(x)).ToString("F2", br));
                cols.Add(p.Doors.Count.ToString());
                cols.Add(p.Openings.Count.ToString());
                cols.Add(p.Edges.Count.ToString());
                cols.Add(PlanInfo.EntranceSide(p));
                cols.Add(depths.Max().ToString());
                cols.Add(depths.Average().ToString("F2", br));
                cols.Add(repeated ? "1" : "0");
            }
            else
            {
                // planta invalida: as colunas da casa ficam vazias
                cols.AddRange(HouseColumns.Select(x => ""));
            }

            return string.Join(";", cols);
        }

        private static readonly string[] HouseColumns =
        {
            "area_m2", "comodos", "quartos", "banheiros", "corredor", "sala_aberta",
            "sala_m2", "cozinha_m2", "corredor_m2",
            "quarto_min_m2", "quarto_media_m2", "quarto_max_m2",
            "banheiro_min_m2", "banheiro_max_m2",
            "maior_comodo_fechado_m2", "menor_comodo_fechado_m2",
            "comodos_em_l", "maior_proporcao",
            "portas", "aberturas", "conexoes", "entrada",
            "profundidade_max", "profundidade_media", "repetida"
        };

        private static string RoomHeader()
        {
            return string.Join(";", new[]
            {
                "versao", "semente", "largura_casa", "altura_casa", "comodo", "tipo", "area_m2",
                "partes", "caixa_largura_m", "caixa_altura_m", "proporcao",
                "portas", "vizinhos", "fachada", "profundidade"
            });
        }

        private static void AppendRoomRows(StringBuilder csv, GenerationResult r)
        {
            if (!r.Success)
                return;

            CultureInfo br = CultureInfo.GetCultureInfo("pt-BR");
            FloorPlan p = r.Plan;

            int[] doors = PlanInfo.DoorsPerRoom(p);
            int[] neighbours = PlanInfo.NeighboursPerRoom(p);
            int[] depths = PlanInfo.Depths(p);

            for (int i = 0; i < p.Rooms.Count; i++)
            {
                Room room = p.Rooms[i];
                Area box = PlanInfo.Box(room);

                csv.AppendLine(string.Join(";", new[]
                {
                    HousePlanner.Version, r.Seed.ToString(), r.Width.ToString(), r.Height.ToString(),
                    i.ToString(), PlanInfo.TypeName(room.Type), M2(room.Size, br),
                    room.Parts.Count.ToString(),
                    (box.Width * PlanInfo.CellSize).ToString("F1", br),
                    (box.Height * PlanInfo.CellSize).ToString("F1", br),
                    PlanInfo.Proportion(room).ToString("F2", br),
                    doors[i].ToString(), neighbours[i].ToString(),
                    PlanValidator.TouchesFacade(p, room) ? "1" : "0",
                    depths[i].ToString()
                }));
            }
        }

        private static int SizeOf(FloorPlan plan, RoomType type)
        {
            Room room = plan.Find(type);

            return room == null ? 0 : room.Size;
        }

        private static string M2(double cells, CultureInfo br)
        {
            return (cells * PlanInfo.CellSize * PlanInfo.CellSize).ToString("F2", br);
        }

        // grava em results/, com versao, tamanho e horario no nome
        private static string Save(StringBuilder csv, int count, HouseSettings s, string kind)
        {
            Directory.CreateDirectory("results");

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string file = Path.Combine("results",
                $"bateria_v{HousePlanner.Version}_{SizeLabel(s)}_{count}_{stamp}_{kind}.csv");

            // utf-8 com BOM, senao o excel embaralha os acentos
            File.WriteAllText(file, csv.ToString(), new UTF8Encoding(true));

            return file;
        }

        private static string SizeLabel(HouseSettings s)
        {
            string width = s.MinWidth == s.MaxWidth ? s.MinWidth.ToString() : s.MinWidth + "-" + s.MaxWidth;
            string height = s.MinHeight == s.MaxHeight ? s.MinHeight.ToString() : s.MinHeight + "-" + s.MaxHeight;

            return width + "x" + height;
        }

        // acumula os numeros pro resumo do console
        private class Summary
        {
            private int valid;
            private int repeated;
            private int withHall;
            private int livingOpen;
            private double area;
            private double bedroomArea;
            private int bedroomCount;
            private double depthMax;
            private int attempts;
            private int maxAttempts;
            private double millis;
            private double worstMillis;
            private readonly Dictionary<Failure, int> rejections = new Dictionary<Failure, int>();
            private readonly SortedDictionary<int, int> roomCounts = new SortedDictionary<int, int>();
            private readonly SortedDictionary<int, int> bedrooms = new SortedDictionary<int, int>();
            private readonly SortedDictionary<int, int> bathrooms = new SortedDictionary<int, int>();
            private readonly SortedDictionary<string, int> entrances = new SortedDictionary<string, int>();

            public void Add(GenerationResult r, bool isRepeated)
            {
                millis += r.Milliseconds;
                worstMillis = Math.Max(worstMillis, r.Milliseconds);

                foreach (KeyValuePair<Failure, int> pair in r.Failures)
                    Bump(rejections, pair.Key, pair.Value);

                if (!r.Success)
                    return;

                valid++;
                attempts += r.Attempts;
                maxAttempts = Math.Max(maxAttempts, r.Attempts);

                if (isRepeated)
                    repeated++;

                if (r.Plan.CountOf(RoomType.Hall) > 0)
                    withHall++;

                if (r.Plan.LivingOpen)
                    livingOpen++;

                area += PlanInfo.SquareMeters(r.Plan.Bounds.Size);
                depthMax += PlanInfo.Depths(r.Plan).Max();

                foreach (Room room in r.Plan.Rooms)
                    if (room.Type == RoomType.Bedroom)
                    {
                        bedroomArea += PlanInfo.SquareMeters(room.Size);
                        bedroomCount++;
                    }

                Bump(roomCounts, r.Plan.RoomCount, 1);
                Bump(bedrooms, r.Plan.CountOf(RoomType.Bedroom), 1);
                Bump(bathrooms, r.Plan.CountOf(RoomType.Bathroom), 1);
                Bump(entrances, PlanInfo.EntranceSide(r.Plan), 1);
            }

            public void Print(int count, HouseSettings s, double seconds, string file)
            {
                Console.WriteLine($"=== bateria v{HousePlanner.Version} | casa {SizeLabel(s)} | {count} sementes sorteadas ===");
                Console.WriteLine($"validas: {valid} ({Pct(valid, count)})");

                if (valid > 0)
                {
                    Console.WriteLine($"tentativas por planta valida: media {(double)attempts / valid:F2}, maxima {maxAttempts}");
                    Console.WriteLine($"plantas distintas: {valid - repeated} de {valid} ({Pct(valid - repeated, valid)})");
                    Console.WriteLine($"casas com corredor: {withHall} de {valid} ({Pct(withHall, valid)})");
                    Console.WriteLine($"sala aberta pro corredor: {livingOpen} de {valid} ({Pct(livingOpen, valid)})");
                    Console.WriteLine($"area media da casa: {area / valid:F1} m2 | quarto medio: {(bedroomCount > 0 ? bedroomArea / bedroomCount : 0):F1} m2");
                    Console.WriteLine($"profundidade maxima media: {depthMax / valid:F2} portas a partir da rua");
                }

                Console.WriteLine($"tempo por casa: media {millis / count:F3} ms, pior {worstMillis:F3} ms");
                Console.WriteLine($"tempo total: {seconds:F1} s");
                Console.WriteLine();

                Console.WriteLine("tentativas rejeitadas, por regra:");

                if (rejections.Count == 0)
                    Console.WriteLine("  nenhuma");

                foreach (KeyValuePair<Failure, int> pair in rejections)
                    Console.WriteLine($"  {pair.Key,-18} {pair.Value}");

                Console.WriteLine();
                PrintTable("comodos por casa (com o corredor):", roomCounts, valid);
                PrintTable("quartos por casa:", bedrooms, valid);
                PrintTable("banheiros por casa:", bathrooms, valid);
                PrintTable("lado da entrada:", entrances, valid);

                Console.WriteLine("csv salvo em: " + Path.GetFullPath(file));
            }

            private static void PrintTable<T>(string title, SortedDictionary<T, int> table, int total)
            {
                Console.WriteLine(title);

                foreach (KeyValuePair<T, int> pair in table)
                    Console.WriteLine($"  {pair.Key,-8} {pair.Value,6}  ({Pct(pair.Value, total)})");

                Console.WriteLine();
            }

            private static void Bump<T>(IDictionary<T, int> map, T key, int amount)
            {
                int current;
                map.TryGetValue(key, out current);
                map[key] = current + amount;
            }

            private static string Pct(int part, int total)
            {
                return total == 0 ? "0%" : (100.0 * part / total).ToString("F1") + "%";
            }
        }
    }
}
