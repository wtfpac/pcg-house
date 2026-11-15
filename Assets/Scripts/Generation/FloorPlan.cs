using System.Collections.Generic;

namespace PCG.Generation
{
    public class FloorPlan
    {
        public Area Bounds;     // retangulo externo da casa
        public int Seed;

        public List<Room> Rooms = new List<Room>();

        // grafo de adjacencia: cada aresta liga dois comodos vizinhos
        public List<RoomGraph.Edge> Edges = new List<RoomGraph.Edge>();

        // pedacos de parede que ficam abertos. inclui a porta da rua.
        public List<WallSpan> Doors = new List<WallSpan>();

        // a porta da rua, ou null se nenhum comodo de circulacao tocar a fachada
        public WallSpan? Entrance;

        public FloorPlan(Area bounds, int seed)
        {
            Bounds = bounds;
            Seed = seed;
        }

        public int RoomCount => Rooms.Count;

        public HashSet<WallKey> OpenWalls()
        {
            HashSet<WallKey> open = new HashSet<WallKey>();

            foreach (WallSpan door in Doors)
                foreach (WallKey key in door.Keys)
                    open.Add(key);

            return open;
        }

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
