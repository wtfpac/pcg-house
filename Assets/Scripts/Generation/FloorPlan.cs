using System.Collections.Generic;

namespace PCG.Generation
{
    public class FloorPlan
    {
        public Area Bounds;     // retangulo externo da casa
        public int Seed;
        public List<Room> Rooms;

        public FloorPlan(Area bounds, int seed)
        {
            Bounds = bounds;
            Seed = seed;
            Rooms = new List<Room>();
        }

        public int RoomCount => Rooms.Count;
    }
}