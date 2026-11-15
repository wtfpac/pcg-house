namespace PCG.Generation
{
    // encurta o corredor nas pontas e doa o espaco liberado ao comodo
    // vizinho. o comodo pode ficar em L, por isso guardamos em Parts.
    public static class CorridorTrimmer
    {
        // 1 em cada 4 vezes a ponta fica inteira, pra nem toda casa sair igual
        private const int KeepEndOdds = 4;

        public static void Trim(FloorPlan plan, HouseSettings s, System.Random rng)
        {
            Room hall = plan.Find(RoomType.Hall);

            if (hall == null)
                return;

            // tenta as duas pontas. de vez em quando deixa uma inteira, pra nem toda casa ficar igual
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
            Area corridor = hall.Parts[0];
            bool alongX = corridor.Width >= corridor.Height;
            int length = alongX ? corridor.Width : corridor.Height;

            // quanto da ponta cortar, corte minimo de 3 celulas, senao a sobra é inutil
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

            // um comodo encostado recebe essa piece
            Room receiver = FindReceiver(plan, strip, alongX);

            if (receiver == null)
                return;

            // encurta o corredor
            hall.Parts[0] = rest;

            // o pedaco vira uma parte nova do comodo vizinho
            receiver.Absorb(strip);
        }

        // acha um comodo que encosta na faixa doada inteira ao lado dela, dai o pedaço se cola sem deixar buraco
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
                    // entao a parede em comum precisa ser horizontal. e vice-versa.
                    if (wall.Vertical != alongX && wall.CoversSideOf(strip))
                        return room;
                }
            }

            return null;
        }
    }
}
