namespace PCG.Generation
{
    // um comodo da casa: onde fica e o que eh
    public class Room
    {
        public Area Area;
        public RoomType Type;

        public Room(Area area, RoomType type)
        {
            Area = area;
            Type = type;
        }
    }
}