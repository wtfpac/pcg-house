using System.Collections.Generic;

namespace PCG.Generation
{
    // encurta o corredor nas pontas e estende os comodos vizinhos
    // pra ocupar o espaco liberado. todo mundo continua retangular.
    public static class CorridorTrimmer
    {
        public static void Trim(List<Room> rooms, System.Random rng, int minLength)
        {
            int hall = FindHall(rooms);

            if (hall < 0)
                return;

            // tenta as duas pontas. de vez em quando deixa uma inteira,
            // pra nem toda casa ficar igual.
            for (int side = 0; side < 2; side++)
            {
                if (rng.Next(4) == 0)
                    continue;

                TrimOneEnd(rooms, hall, minLength);
            }
        }

        // encurta uma ponta, se achar um comodo que encaixe
        private static void TrimOneEnd(List<Room> rooms, int hall, int minLength)
        {
            Area h = rooms[hall].Area;
            bool horizontal = h.Width >= h.Height;

            int target = FindEndRoom(rooms, hall, horizontal, minLength);

            if (target < 0)
                return;

            Area t = rooms[target].Area;

            if (horizontal)
            {
                bool atRight = t.Right == h.Right;

                rooms[hall].Area = atRight
                    ? new Area(h.X, h.Y, h.Width - t.Width, h.Height)
                    : new Area(h.X + t.Width, h.Y, h.Width - t.Width, h.Height);

                // o comodo cresce na direcao do corredor
                rooms[target].Area = t.Bottom == h.Y
                    ? new Area(t.X, t.Y, t.Width, t.Height + h.Height)
                    : new Area(t.X, t.Y - h.Height, t.Width, t.Height + h.Height);
            }
            else
            {
                bool atBottom = t.Bottom == h.Bottom;

                rooms[hall].Area = atBottom
                    ? new Area(h.X, h.Y, h.Width, h.Height - t.Height)
                    : new Area(h.X, h.Y + t.Height, h.Width, h.Height - t.Height);

                rooms[target].Area = t.Right == h.X
                    ? new Area(t.X, t.Y, t.Width + h.Width, t.Height)
                    : new Area(t.X - h.Width, t.Y, t.Width + h.Width, t.Height);
            }
        }

        private static int FindHall(List<Room> rooms)
        {
            for (int i = 0; i < rooms.Count; i++)
                if (rooms[i].Type == RoomType.Hall)
                    return i;

            return -1;
        }

        // acha um comodo alinhado com uma das pontas do corredor,
        // que possa crescer sem deixar o corredor curto demais
        private static int FindEndRoom(List<Room> rooms, int hall, bool horizontal,
                                       int minLength)
        {
            Area h = rooms[hall].Area;

            for (int i = 0; i < rooms.Count; i++)
            {
                if (i == hall || rooms[i].Type == RoomType.Hall)
                    continue;

                Area a = rooms[i].Area;

                if (horizontal)
                {
                    bool adjacent = a.Bottom == h.Y || a.Y == h.Bottom;
                    bool atEnd = a.X == h.X || a.Right == h.Right;

                    if (adjacent && atEnd && h.Width - a.Width >= minLength)
                        return i;
                }
                else
                {
                    bool adjacent = a.Right == h.X || a.X == h.Right;
                    bool atEnd = a.Y == h.Y || a.Bottom == h.Bottom;

                    if (adjacent && atEnd && h.Height - a.Height >= minLength)
                        return i;
                }
            }

            return -1;
        }
    }
}