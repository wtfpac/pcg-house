using System;
using System.Collections.Generic;

namespace PCG.Generation
{
    // um comodo da casa. nasce como um retangulo so, mas pode ganhar
    // pedacos dos vizinhos e ficar em L. por isso guarda uma lista de partes
    public class Room
    {
        public RoomType Type;
        public List<Area> Parts;

        public Room(Area area, RoomType type)
        {
            Type = type;
            Parts = new List<Area> { area };
        }

        // sala e corredor formam a circulacao: e por onde se anda pela casa
        public bool IsCirculation => Type == RoomType.Living || Type == RoomType.Hall;

        // quarto e banheiro sao os comodos privativos
        public bool IsPrivate => Type == RoomType.Bedroom || Type == RoomType.Bathroom;

        // junta um pedaco de espaco ao comodo. se ele completar um retangulo
        // com uma parte que ja existe, as duas viram uma so. senao entra como parte nova, e o comodo fica em L
        public void Absorb(Area piece)
        {
            int partner = FindRectanglePartner(piece);

            if (partner < 0)
            {
                Parts.Add(piece);
                return;
            }

            Area part = Parts[partner];
            int x = Math.Min(part.X, piece.X);
            int y = Math.Min(part.Y, piece.Y);
            int right = Math.Max(part.Right, piece.Right);
            int bottom = Math.Max(part.Bottom, piece.Bottom);

            Parts[partner] = new Area(x, y, right - x, bottom - y);
        }

        // o pedaco, junto com alguma parte do comodo, forma um retangulo perfeito?
        public bool FormsRectangleWith(Area piece)
        {
            return FindRectanglePartner(piece) >= 0;
        }

        // posicao da parte que forma um retangulo com o pedaco, ou -1.
        // isso acontece quando a parede entre os dois cobre um lado inteiro de cada um
        private int FindRectanglePartner(Area piece)
        {
            for (int i = 0; i < Parts.Count; i++)
            {
                WallSpan wall = Adjacency.Wall(Parts[i], piece);

                if (wall.CoversSideOf(Parts[i]) && wall.CoversSideOf(piece))
                    return i;
            }

            return -1;
        }

        // area total em celulas, somando todas as partes
        public int Size
        {
            get
            {
                int total = 0;

                foreach (Area part in Parts)
                    total += part.Size;

                return total;
            }
        }
    }
}