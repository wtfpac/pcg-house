using System;
using System.Collections.Generic;
using PCG.Generation;
using PCG.Preview;

class Program
{
    static void Main(string[] args)
    {
        int seed;

        if (args.Length == 0 || !int.TryParse(args[0], out seed))
            seed = new Random().Next(0, 999999);

        Area bounds = new Area(0, 0, 20, 16);
        FloorPlan plan = HousePlanner.Build(bounds, seed, 5, 5, 7, 2, 2);

        if (plan == null)
        {
            Console.WriteLine($"semente {seed}: nenhuma planta valida");
            Console.WriteLine($"falhas: {HousePlanner.FailureSummary()}");
            return;
        }

        var edges = RoomGraph.Build(plan.Rooms, 2);
        var doors = DoorPlacer.Place(plan, edges);
        var entrance = EntrancePlacer.Place(plan);

        if (entrance.HasValue)
            doors.Add(entrance.Value);

        Console.WriteLine($"semente {seed} | {plan.RoomCount} comodos | "
                          + $"{edges.Count} conexoes | {doors.Count} portas | "
                          + $"{HousePlanner.LastAttempts} tentativas");

        Console.WriteLine($"composicao: {Composition(plan)}");
        Console.WriteLine($"entrada: {EntranceSide(plan, entrance)}");
        Console.WriteLine($"falhas no caminho: {HousePlanner.FailureSummary()}");
        Console.WriteLine();
        Console.WriteLine(PlanRenderer.Render(plan, doors));
    }

    // conta quantos comodos de cada tipo a casa tem
    static string Composition(FloorPlan plan)
    {
        Dictionary<RoomType, int> count = new Dictionary<RoomType, int>();

        foreach (Room room in plan.Rooms)
        {
            if (!count.ContainsKey(room.Type))
                count[room.Type] = 0;

            count[room.Type]++;
        }

        List<string> parts = new List<string>();

        foreach (var pair in count)
            parts.Add($"{pair.Key}:{pair.Value}");

        return string.Join("  ", parts);
    }

    // em qual fachada a porta da rua ficou
    static string EntranceSide(FloorPlan plan, DoorPlacer.WallKey? entrance)
    {
        if (!entrance.HasValue)
            return "NAO ENCONTRADA";

        DoorPlacer.WallKey k = entrance.Value;
        Area b = plan.Bounds;

        if (k.Vertical && k.X == b.X) return "oeste";
        if (k.Vertical && k.X == b.Right) return "leste";
        if (!k.Vertical && k.Y == b.Y) return "norte";
        if (!k.Vertical && k.Y == b.Bottom) return "sul";

        return "interna (erro)";
    }
}