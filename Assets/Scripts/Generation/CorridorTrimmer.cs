namespace PCG.Generation
{
    // o corredor nasce atravessando a casa de ponta a ponta. aqui ele e
    // encurtado: o pedaco cortado da ponta e doado a um comodo vizinho, permitindo ele ficar em L
    public static class CorridorTrimmer
    {
        // 1 em cada 4 vezes a ponta fica inteira, evita repetiçao e casas parecidas
        private const int KeepEndOdds = 4;

        public static void Trim(FloorPlan plan, HouseSettings s, System.Random rng)
        {
            Room hall = plan.Find(RoomType.Hall);

            if (hall == null)
                return;

            // duas tentativas de corte, uma pra cada ponta
            for (int i = 0; i < 2; i++)
            {
                if (rng.Next(KeepEndOdds) == 0)
                    continue;

                TrimOneEnd(plan, hall, s, rng);
            }
        }

        private static void TrimOneEnd(FloorPlan plan, Room hall, HouseSettings s,
                                       System.Random rng)
        {
            Area corridor = hall.Parts[0];   // o corredor e sempre um retangulo so
            bool alongX = corridor.Width >= corridor.Height;
            int length = alongX ? corridor.Width : corridor.Height;

            // o corredor nunca fica mais curto que um comodo minimo, e o
            // pedaco cortado precisa ter um tamanho que sirva pra alguma coisa
            int maxCut = length - s.MinRoomSize;

            if (maxCut < s.MinCorridorCut)
                return;

            int cut = rng.Next(s.MinCorridorCut, maxCut + 1);
            bool fromEnd = rng.Next(2) == 0;

            Area strip;
            Area rest;

            if (fromEnd)
                corridor.Split(alongX, length - cut, out rest, out strip);
            else
                corridor.Split(alongX, cut, out strip, out rest);

            Room receiver = FindReceiver(plan, strip, alongX);

            if (receiver == null)
                return;   // ninguem pode receber o pedaco, o corredor fica como esta

            hall.Parts[0] = rest;
            receiver.Absorb(strip);
        }

        // quem recebe o pedaco: um comodo que encosta nele ao longo de todo
        // o comprimento, de um dos lados do corredor. assim nao sobra buraco
        private static Room FindReceiver(FloorPlan plan, Area strip, bool alongX)
        {
            foreach (Room room in plan.Rooms)
            {
                // banheiro nao recebe: ele precisa ficar pequeno
                if (room.Type == RoomType.Hall || room.Type == RoomType.Bathroom)
                    continue;

                foreach (Area part in room.Parts)
                {
                    WallSpan wall = Adjacency.Wall(part, strip);

                    // corredor deitado (alongX) tem os vizinhos em cima e embaixo,
                    // entao a parede em comum precisa ser horizontal. e vice-versa
                    if (wall.Vertical != alongX && wall.CoversSideOf(strip))
                        return room;
                }
            }

            return null;
        }
    }
}