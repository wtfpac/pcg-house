using System;
using PCG.Generation;
using PCG.Preview;

// visualizador de console. usa os mesmos arquivos da pasta Generation do projeto unity.
//   dotnet run                 -> uma casa com semente sorteada
//   dotnet run 42              -> a casa da semente 42
//   dotnet run batch           -> bateria de 1000 casas, gera um csv em results/
//   dotnet run batch 5000      -> bateria de 5000 casas
//   dotnet run batch 1000 28x20 -> bateria com outro tamanho de casa (em celulas)
class Program
{
    static void Main(string[] args)
    {
        HouseSettings settings = new HouseSettings();

        if (args.Length > 0 && args[0] == "batch")
        {
            RunBatch(args, settings);
            return;
        }

        int seed;

        if (args.Length == 0 || !int.TryParse(args[0], out seed))
            seed = new Random().Next(0, 999999);

        GenerationResult result = HousePlanner.Generate(settings, seed);

        Console.WriteLine($"v{HousePlanner.Version} | semente {seed} | "
                          + $"{result.Attempts} tentativas | {result.Milliseconds:F2} ms");
        Console.WriteLine($"falhas no caminho: {result.FailureSummary()}");

        if (!result.Success)
        {
            Console.WriteLine("nenhuma planta valida");
            return;
        }

        FloorPlan plan = result.Plan;

        Console.WriteLine($"{plan.RoomCount} comodos | {plan.Edges.Count} conexoes | "
                          + $"{plan.Doors.Count} portas");
        Console.WriteLine($"quartos: {plan.CountOf(RoomType.Bedroom)} | "
                          + $"banheiros: {plan.CountOf(RoomType.Bathroom)}");
        Console.WriteLine($"entrada: {PlanInfo.EntranceSide(plan)}");
        Console.WriteLine();
        Console.WriteLine(PlanRenderer.Render(plan));
    }

    // le "batch [quantidade] [largura x altura]" e roda a bateria
    static void RunBatch(string[] args, HouseSettings settings)
    {
        int count = 1000;

        if (args.Length > 1 && !int.TryParse(args[1], out count))
        {
            Console.WriteLine("quantidade invalida: " + args[1]);
            return;
        }

        if (args.Length > 2)
        {
            string[] size = args[2].ToLower().Split('x');
            int width, height;

            if (size.Length != 2 || !int.TryParse(size[0], out width)
                || !int.TryParse(size[1], out height))
            {
                Console.WriteLine("tamanho invalido, use o formato 28x20: " + args[2]);
                return;
            }

            settings.Width = width;
            settings.Height = height;
        }

        BatchRunner.Run(count, settings);
    }
}
