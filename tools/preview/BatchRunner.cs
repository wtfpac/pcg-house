using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
            Failure.Facade, Failure.Circulation, Failure.CirculationSplit
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

            // aquecimento: as primeiras geracoes sao lentas porque o c# ainda
            // esta compilando o codigo. elas nao entram na medicao.
            for (int i = 0; i < 50; i++)
                HousePlanner.Generate(settings, i);

            Summary summary = new Summary();
            Stopwatch total = Stopwatch.StartNew();

            while (usedSeeds.Count < count)
            {
                int seed = picker.Next(0, int.MaxValue);

                if (!usedSeeds.Add(seed))
                    continue;   // ja saiu antes, sorteia outra

                GenerationResult result = HousePlanner.Generate(settings, seed);

                bool repeated = false;

                if (result.Success)
                    repeated = !seenPlans.Add(PlanInfo.Signature(result.Plan));

                csv.AppendLine(Row(result, settings, repeated));
                summary.Add(result, repeated);
            }

            total.Stop();

            string file = Save(csv, count, settings);
            summary.Print(count, settings, total.Elapsed.TotalSeconds, file);
        }

        private static string Header()
        {
            List<string> cols = new List<string>
            {
                "versao", "largura", "altura", "semente", "valida", "tentativas", "tempo_ms"
            };

            foreach (Failure rule in Rules)
                cols.Add("rejeicoes_" + rule);

            cols.AddRange(new[]
            {
                "comodos", "quartos", "banheiros", "maior_banheiro_celulas",
                "portas", "conexoes", "entrada", "repetida"
            });

            return string.Join(";", cols);
        }

        // uma linha do csv. ponto e virgula e virgula decimal, que e o
        // formato que o excel em portugues abre direto
        private static string Row(GenerationResult r, HouseSettings s, bool repeated)
        {
            CultureInfo br = CultureInfo.GetCultureInfo("pt-BR");
            List<string> cols = new List<string>
            {
                HousePlanner.Version, s.Width.ToString(), s.Height.ToString(),
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
                cols.Add(p.RoomCount.ToString());
                cols.Add(p.CountOf(RoomType.Bedroom).ToString());
                cols.Add(p.CountOf(RoomType.Bathroom).ToString());
                cols.Add(PlanInfo.LargestBathroom(p).ToString());
                cols.Add(p.Doors.Count.ToString());
                cols.Add(p.Edges.Count.ToString());
                cols.Add(PlanInfo.EntranceSide(p));
                cols.Add(repeated ? "1" : "0");
            }
            else
            {
                // planta invalida: as colunas da casa ficam vazias
                cols.AddRange(new[] { "", "", "", "", "", "", "", "" });
            }

            return string.Join(";", cols);
        }

        // grava em results/, com versao, tamanho e horario no nome
        private static string Save(StringBuilder csv, int count, HouseSettings s)
        {
            Directory.CreateDirectory("results");

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string file = Path.Combine("results",
                $"bateria_v{HousePlanner.Version}_{s.Width}x{s.Height}_{count}_{stamp}.csv");

            // utf-8 com BOM, senao o excel embaralha os acentos
            File.WriteAllText(file, csv.ToString(), new UTF8Encoding(true));

            return file;
        }

        // acumula os numeros pro resumo do console
        private class Summary
        {
            private int valid;
            private int repeated;
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

                Bump(roomCounts, r.Plan.RoomCount, 1);
                Bump(bedrooms, r.Plan.CountOf(RoomType.Bedroom), 1);
                Bump(bathrooms, r.Plan.CountOf(RoomType.Bathroom), 1);
                Bump(entrances, PlanInfo.EntranceSide(r.Plan), 1);
            }

            public void Print(int count, HouseSettings s, double seconds, string file)
            {
                Console.WriteLine($"=== bateria v{HousePlanner.Version} | casa {s.Width}x{s.Height} | {count} sementes sorteadas ===");
                Console.WriteLine($"validas: {valid} ({Pct(valid, count)})");

                if (valid > 0)
                {
                    Console.WriteLine($"tentativas por planta valida: media {(double)attempts / valid:F2}, maxima {maxAttempts}");
                    Console.WriteLine($"plantas distintas: {valid - repeated} de {valid} ({Pct(valid - repeated, valid)})");
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
