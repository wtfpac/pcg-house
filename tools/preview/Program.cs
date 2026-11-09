using System;
using PCG.Generation;
using PCG.Preview;

class Program
{
    static void Main(string[] args)
    {
        // aceita a semente como argumento: dotnet run 123
        // sem argumento, sorteia uma
        int seed;

        if (args.Length == 0 || !int.TryParse(args[0], out seed))
            seed = new Random().Next(0, 999999);

        Area bounds = new Area(0, 0, 20, 16);
        FloorPlan plan = HousePlanner.Build(bounds, seed, 5, 5, 7, 2, 2);

        if (plan == null)
        {
            Console.WriteLine($"semente {seed}: nenhuma planta valida");
            return;
        }

        Console.WriteLine($"semente {seed} | comodos: {plan.RoomCount} | tentativas: {HousePlanner.LastAttempts}");

        var edges = RoomGraph.Build(plan.Rooms, 2);
        var doors = DoorPlacer.Place(plan, edges);

        var entrance = EntrancePlacer.Place(plan);

        if (entrance.HasValue)
        {
            doors.Add(entrance.Value);
            Console.WriteLine("entrada posicionada");
        }
        else
        {
            Console.WriteLine("sem entrada: a sala nao toca fachada");
        }
    

        Console.WriteLine($"conexoes: {edges.Count} | portas: {doors.Count}");
        Console.WriteLine();
        Console.WriteLine(PlanRenderer.Render(plan, doors));
    }
}