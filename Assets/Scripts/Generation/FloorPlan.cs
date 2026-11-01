using System.Collections.Generic;

namespace PCG.Generation
{
    // resultado de uma geraçao, a casa inteira
    public class FloorPlan
    {
        public Area Bounds;     // retangulo externo dacasa
        public int Seed;        // semente q gerou a casa
        public List<Room> Rooms;

        public FloorPlan(Area bounds, int seed)
        {
            Bounds = bounds;
            Seed = seed;
            Rooms = new List<Room>();   // começa vazia, o gerador preenche]
        }

        public int RoomCount => Rooms.Count;
    }
}