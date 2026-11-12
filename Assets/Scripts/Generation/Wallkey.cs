using System;

namespace PCG.Generation
{
    // identifica um pedaco de parede de 1 celula de comprimento.
    // parede vertical: fica na coluna X, entre as celulas (X-1, Y) e (X, Y)
    // parede horizontal: fica na linha Y, entre as celulas (X, Y-1) e (X, Y)
    public struct WallKey : IEquatable<WallKey>
    {
        public bool Vertical;
        public int X;
        public int Y;

        public WallKey(bool vertical, int x, int y)
        {
            Vertical = vertical;
            X = x;
            Y = y;
        }

        // igualdade e hash escritos na mao porque a struct e usada como chave
        // de HashSet. sem isso funciona, mas o c# compara por reflexao, bem mais lento
        public bool Equals(WallKey other)
        {
            return Vertical == other.Vertical && X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is WallKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (X * 397 ^ Y) * 2 + (Vertical ? 1 : 0);
        }
    }
}