using System.Collections.Generic;

namespace PCG.Generation
{
    // corta a casa em retangulos com BSP recursivo
    // nao decide tipo de comodo ainda, so geometria
    public static class BspSplitter
    {
        // devolve os retangulos da casa, sem tipo definido
        public static List<Area> Split(Area region, int targetCount, int minSize,
                                       System.Random rng)
        {
            List<Area> areas = new List<Area> { region };

            while (areas.Count < targetCount)
            {
                int index = BiggestSplittable(areas, minSize);

                if (index < 0)
                    break;

                Area target = areas[index];

                // corta o lado mais comprido, pra nao gerar comodo estreito
                bool fitsX = target.Width >= minSize * 2;
                bool fitsY = target.Height >= minSize * 2;
                bool alongX = fitsX && fitsY ? target.Width >= target.Height : fitsX;

                // sorteia onde cortar sem deixar nenhum lado abaixo do minimo
                int length = alongX ? target.Width : target.Height;
                int cut = rng.Next(minSize, length - minSize + 1);

                target.Split(alongX, cut, out Area first, out Area second);

                areas.RemoveAt(index);
                areas.Add(first);
                areas.Add(second);
            }

            return areas;
        }

        private static int BiggestSplittable(List<Area> areas, int minSize)
        {
            int best = -1;

            for (int i = 0; i < areas.Count; i++)
            {
                bool splittable = areas[i].Width >= minSize * 2
                               || areas[i].Height >= minSize * 2;

                if (!splittable)
                    continue;

                if (best < 0 || areas[i].Size > areas[best].Size)
                    best = i;
            }

            return best;
        }
    }
}