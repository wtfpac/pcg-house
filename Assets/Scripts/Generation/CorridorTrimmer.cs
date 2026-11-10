using System.Collections.Generic;

namespace PCG.Generation
{
    // encurta o corredor nas pontas e doa o espaco liberado ao comodo
    // vizinho. o comodo pode ficar em L, por isso guardamos em Parts.
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

                TrimOneEnd(rooms, hall, rng, minLength);
            }
        }

        private static int FindHall(List<Room> rooms)
        {
            for (int i = 0; i < rooms.Count; i++)
                if (rooms[i].Type == RoomType.Hall)
                    return i;

            return -1;
        }

        private static void TrimOneEnd(List<Room> rooms, int hall,
                                       System.Random rng, int minLength)
        {
            Area h = rooms[hall].Area;
            bool horizontal = h.Width >= h.Height;
            int length = horizontal ? h.Width : h.Height;

            if (length <= minLength)
                return;

            // quanto da ponta cortar
            // corte minimo de 3 celulas, senao a sobra e inutil
            int minCut = 3;

            if (length - minLength < minCut)
                return;   // nao da pra cortar um pedaco util

            int cut = rng.Next(minCut, length - minLength + 1);            bool fromEnd = rng.Next(2) == 0;

            Area strip = CutStrip(h, cut, fromEnd, horizontal);

            // quem recebe o pedaco: um comodo encostado nele
            int target = FindReceiver(rooms, hall, strip, horizontal);

            if (target < 0)
                return;

            // encurta o corredor
            rooms[hall].Area = Shorten(h, cut, fromEnd, horizontal);

            // o pedaco vira uma parte nova do comodo vizinho
            rooms[target].Parts.Add(strip);
        }

        // o retangulo que vai ser doado
        private static Area CutStrip(Area h, int cut, bool fromEnd, bool horizontal)
        {
            if (horizontal)
            {
                int x = fromEnd ? h.Right - cut : h.X;
                return new Area(x, h.Y, cut, h.Height);
            }

            int y = fromEnd ? h.Bottom - cut : h.Y;
            return new Area(h.X, y, h.Width, cut);
        }

        // o corredor depois do corte
        private static Area Shorten(Area h, int cut, bool fromEnd, bool horizontal)
        {
            if (horizontal)
            {
                return fromEnd
                    ? new Area(h.X, h.Y, h.Width - cut, h.Height)
                    : new Area(h.X + cut, h.Y, h.Width - cut, h.Height);
            }

            return fromEnd
                ? new Area(h.X, h.Y, h.Width, h.Height - cut)
                : new Area(h.X, h.Y + cut, h.Width, h.Height - cut);
        }

        // acha um comodo que encosta na faixa doada ao longo de todo
        // o lado dela. assim o pedaco se cola sem deixar buraco.
        private static int FindReceiver(List<Room> rooms, int hall, Area strip,
                                        bool horizontal)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (i == hall || rooms[i].Type == RoomType.Hall)
                    continue;

                foreach (Area a in rooms[i].Parts)
                {
                    if (horizontal)
                    {
                        // precisa estar acima ou abaixo, cobrindo a largura toda
                        bool stacked = a.Bottom == strip.Y || a.Y == strip.Bottom;
                        bool covers = a.X <= strip.X && a.Right >= strip.Right;

                        if (stacked && covers)
                            return i;
                    }
                    else
                    {
                        bool sideways = a.Right == strip.X || a.X == strip.Right;
                        bool covers = a.Y <= strip.Y && a.Bottom >= strip.Bottom;

                        if (sideways && covers)
                            return i;
                    }
                }
            }

            return -1;
        }
    }
}