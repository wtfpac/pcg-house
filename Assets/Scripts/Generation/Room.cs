using System.Collections.Generic;

namespace PCG.Generation
{
    // um comodo da casa. pode ser composto por mais de um retangulo,
    // o que permite plantas em L alem das retangulares.
    public class Room
    {
        public List<Area> Parts;
        public RoomType Type;

        // construtor simples, pro caso comum de um retangulo so
        public Room(Area area, RoomType type)
        {
            Parts = new List<Area> { area };
            Type = type;
        }

        public Room(List<Area> parts, RoomType type)
        {
            Parts = parts;
            Type = type;
        }

        // atalho pro primeiro retangulo. usado onde o comodo
        // sabidamente tem uma parte so.
        public Area Area
        {
            get { return Parts[0]; }
            set { Parts[0] = value; }
        }

        // soma da area de todas as partes
        public int Size
        {
            get
            {
                int total = 0;

                foreach (Area a in Parts)
                    total += a.Size;

                return total;
            }
        }

        // menor retangulo que cobre o comodo inteiro
        public Area Bounds
        {
            get
            {
                int x0 = Parts[0].X;
                int y0 = Parts[0].Y;
                int x1 = Parts[0].Right;
                int y1 = Parts[0].Bottom;

                foreach (Area a in Parts)
                {
                    if (a.X < x0) x0 = a.X;
                    if (a.Y < y0) y0 = a.Y;
                    if (a.Right > x1) x1 = a.Right;
                    if (a.Bottom > y1) y1 = a.Bottom;
                }

                return new Area(x0, y0, x1 - x0, y1 - y0);
            }
        }
    }
}