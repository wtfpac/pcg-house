using System.Collections.Generic;

namespace PCG.Generation
{
    // banheiro nao precisa ser do tamanho de um quarto. doa fileiras
    // dele pros vizinhos ate caber na area alvo.
    public static class BathroomShrinker
    {
        public static void Shrink(List<Room> rooms, int maxArea, int minSide, int minShared)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].Type != RoomType.Bathroom)
                    continue;

                if (rooms[i].Parts.Count != 1)
                    continue;

                // doa uma fileira por vez ate caber, ou ate travar
                for (int pass = 0; pass < 12; pass++)
                {
                    if (rooms[i].Area.Size <= maxArea)
                        break;

                    if (!DonateOneRow(rooms, i, minSide, minShared))
                        break;
                }
            }
        }

        // tenta doar uma fileira de 1 celula de espessura.
        // devolve false se nenhum lado aceitou.
        private static bool DonateOneRow(List<Room> rooms, int index, int minSide, int minShared)
        {
            int keepSide = FindCirculationSide(rooms, index, minShared);

            for (int side = 0; side < 4; side++)
            {
                if (side == keepSide)
                    continue;

                if (TryDonateSide(rooms, index, side, minSide))
                    return true;
            }

            return false;
        }

        private static bool TryDonateSide(List<Room> rooms, int index, int side, int minSide)
        {
            Area a = rooms[index].Area;

            bool vertical = side == 0 || side == 2;

            // nao encolhe abaixo do lado minimo
            if (vertical && a.Height - 1 < minSide) return false;
            if (!vertical && a.Width - 1 < minSide) return false;

            // a fileira que esta na borda escolhida
            Area row;

            if (side == 0)      row = new Area(a.X, a.Y, a.Width, 1);
            else if (side == 2) row = new Area(a.X, a.Bottom - 1, a.Width, 1);
            else if (side == 1) row = new Area(a.X, a.Y, 1, a.Height);
            else                row = new Area(a.Right - 1, a.Y, 1, a.Height);

            // quem encosta nessa fileira, e em qual trecho
            int target = FindPartialReceiver(rooms, index, row, vertical, out Area piece);

            if (target < 0)
                return false;

            // o banheiro perde so o pedaco doado
            rooms[index].Parts.Clear();
            rooms[index].Parts.AddRange(Subtract(a, piece));

            rooms[target].Parts.Add(piece);

            return true;
        }

        // acha um vizinho que encosta em parte da fileira.
        // devolve em "piece" so o trecho que esse vizinho cobre.
        private static int FindPartialReceiver(List<Room> rooms, int skip, Area row,
                                               bool vertical, out Area piece)
        {
            piece = row;

            for (int i = 0; i < rooms.Count; i++)
            {
                if (i == skip)
                    continue;

                RoomType t = rooms[i].Type;

                if (t == RoomType.Hall || t == RoomType.Bathroom)
                    continue;

                foreach (Area p in rooms[i].Parts)
                {
                    if (vertical)
                    {
                        bool stacked = p.Bottom == row.Y || p.Y == row.Bottom;

                        if (!stacked)
                            continue;

                        int start = p.X > row.X ? p.X : row.X;
                        int end = p.Right < row.Right ? p.Right : row.Right;

                        if (end - start <= 0)
                            continue;

                        piece = new Area(start, row.Y, end - start, 1);
                        return i;
                    }
                    else
                    {
                        bool sideways = p.Right == row.X || p.X == row.Right;

                        if (!sideways)
                            continue;

                        int start = p.Y > row.Y ? p.Y : row.Y;
                        int end = p.Bottom < row.Bottom ? p.Bottom : row.Bottom;

                        if (end - start <= 0)
                            continue;

                        piece = new Area(row.X, start, 1, end - start);
                        return i;
                    }
                }
            }

            return -1;
        }

        // o que sobra do retangulo a depois de tirar o pedaco.
        // devolve ate tres retangulos, cobrindo o resto sem buraco.
        private static List<Area> Subtract(Area a, Area cut)
        {
            List<Area> rest = new List<Area>();

            // faixa acima do corte
            if (cut.Y > a.Y)
                rest.Add(new Area(a.X, a.Y, a.Width, cut.Y - a.Y));

            // faixa abaixo do corte
            if (cut.Bottom < a.Bottom)
                rest.Add(new Area(a.X, cut.Bottom, a.Width, a.Bottom - cut.Bottom));

            // esquerda e direita, na altura do corte
            if (cut.X > a.X)
                rest.Add(new Area(a.X, cut.Y, cut.X - a.X, cut.Height));

            if (cut.Right < a.Right)
                rest.Add(new Area(cut.Right, cut.Y, a.Right - cut.Right, cut.Height));

            return rest;
        }

        // de que lado esta a circulacao, pra nao cortar ali.
        // 0 = cima, 1 = esquerda, 2 = baixo, 3 = direita, -1 = nenhum
        private static int FindCirculationSide(List<Room> rooms, int index, int minShared)
        {
            Area a = rooms[index].Area;

            for (int i = 0; i < rooms.Count; i++)
            {
                if (i == index)
                    continue;

                RoomType t = rooms[i].Type;

                if (t != RoomType.Hall && t != RoomType.Living)
                    continue;

                foreach (Area p in rooms[i].Parts)
                {
                    if (Adjacency.SharedLength(a, p) < minShared)
                        continue;

                    if (p.Bottom == a.Y) return 0;
                    if (p.Right == a.X) return 1;
                    if (p.Y == a.Bottom) return 2;
                    if (p.X == a.Right) return 3;
                }
            }

            return -1;
        }
    }
}