using System.Collections.Generic;

namespace PCG.Generation
{
    // corta a casa em retangulos com BSP recursivo.
    // nao decide tipo de comodo ainda, so geometria.
    public class HouseGenerator
    {
        private readonly int minRoomSize;
        private readonly int targetRooms;
        private readonly System.Random random;

        public HouseGenerator(int minRoomSize, int targetRooms, int seed)
        {
            this.minRoomSize = minRoomSize;
            this.targetRooms = targetRooms;
            this.random = new System.Random(seed);
        }

        // devolve os retangulos da casa, sem tipo definido
        public List<Area> SplitSpace(Area bounds)
        {
            List<Area> areas = new List<Area>();
            areas.Add(bounds);

            while (areas.Count < targetRooms)
            {
                int index = PickBiggestSplittable(areas);

                if (index < 0)
                    break;   // ninguem mais pode ser cortado

                Area target = areas[index];
                Split(target, out Area a, out Area b);

                areas.RemoveAt(index);
                areas.Add(a);
                areas.Add(b);
            }

            return areas;
        }

        // o maior retangulo que ainda aceita corte. -1 se nenhum aceita.
        private int PickBiggestSplittable(List<Area> areas)
        {
            int best = -1;

            for (int i = 0; i < areas.Count; i++)
            {
                if (!CanSplit(areas[i]))
                    continue;

                if (best < 0 || areas[i].Size > areas[best].Size)
                    best = i;
            }

            return best;
        }

        private bool CanSplit(Area area)
        {
            return area.Width >= minRoomSize * 2 || area.Height >= minRoomSize * 2;
        }

        // corta o retangulo em dois. so e chamado quando CanSplit deu true.
        private void Split(Area area, out Area first, out Area second)
        {
            bool canVertical = area.Width >= minRoomSize * 2;
            bool canHorizontal = area.Height >= minRoomSize * 2;

            // corta o lado mais comprido, pra nao gerar comodo estreito
            bool vertical;

            if (canVertical && canHorizontal)
                vertical = area.Width >= area.Height;
            else
                vertical = canVertical;

            if (vertical)
            {
                int cut = PickCut(area.Width);
                first = new Area(area.X, area.Y, cut, area.Height);
                second = new Area(area.X + cut, area.Y, area.Width - cut, area.Height);
            }
            else
            {
                int cut = PickCut(area.Height);
                first = new Area(area.X, area.Y, area.Width, cut);
                second = new Area(area.X, area.Y + cut, area.Width, area.Height - cut);
            }
        }

        // sorteia onde cortar sem deixar nenhum lado abaixo do minimo
        private int PickCut(int length)
        {
            return random.Next(minRoomSize, length - minRoomSize + 1);
        }
    }
}