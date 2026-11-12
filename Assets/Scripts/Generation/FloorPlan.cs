using System.Collections.Generic;

namespace PCG.Generation
{
    // a planta de uma casa: os comodos, quem encosta em quem e onde ficam as portas.
    // e o que o gerador devolve e o que a unity le pra construir a cena
    public class FloorPlan
    {
        public Area Bounds;     // contorno externo da casa
        public int Seed;        // semente que gerou esta planta

        public List<Room> Rooms = new List<Room>();

        // grafo de adjacencia: cada aresta liga dois comodos vizinhos
        public List<RoomGraph.Edge> Edges = new List<RoomGraph.Edge>();

        // pedacos de parede que ficam abertos. inclui a porta da rua
        public HashSet<WallKey> Doors = new HashSet<WallKey>();

        // a porta da rua, ou null se nenhum comodo de circulacao tocar a fachada
        public WallKey? Entrance;

        public FloorPlan(Area bounds, int seed)
        {
            Bounds = bounds;
            Seed = seed;
        }

        public int RoomCount => Rooms.Count;

        // quantos comodos de um tipo a casa tem
        public int CountOf(RoomType type)
        {
            int count = 0;

            foreach (Room room in Rooms)
                if (room.Type == type)
                    count++;

            return count;
        }

        // o primeiro comodo desse tipo, ou null se nao houver
        public Room Find(RoomType type)
        {
            foreach (Room room in Rooms)
                if (room.Type == type)
                    return room;

            return null;
        }
    }
}