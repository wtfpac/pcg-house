using System;
using PCG.Generation;
using PCG.Preview;

// visualizador de console: gera uma casa e desenha a planta em texto.
// usa os mesmos arquivos da pasta Generation do projeto unity.
//   dotnet run        -> semente sorteada
//   dotnet run 42     -> semente fixa, sempre a mesma casa
class Program
{
    static void Main(string[] args)
    {
        int seed;

        if (args.Length == 0 || !int.TryParse(args[0], out seed))
            seed = new Random().Next(0, 999999);

        HouseSettings settings = new HouseSettings();
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
        Console.WriteLine($"entrada: {EntranceSide(plan)}");
        Console.WriteLine();
        Console.WriteLine(PlanRenderer.Render(plan));
    }

    // em qual fachada a porta da rua ficou
    static string EntranceSide(FloorPlan plan)
    {
        if (!plan.Entrance.HasValue)
            return "NAO ENCONTRADA";

        WallKey door = plan.Entrance.Value;
        Area b = plan.Bounds;

        if (door.Vertical && door.X == b.X) return "oeste";
        if (door.Vertical && door.X == b.Right) return "leste";
        if (!door.Vertical && door.Y == b.Y) return "norte";
        if (!door.Vertical && door.Y == b.Bottom) return "sul";

        return "interna (erro)";
    }
}
