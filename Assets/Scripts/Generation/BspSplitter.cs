using System.Collections.Generic;

namespace PCG.Generation
{
    // BSP (binary space partitioning): corta um retangulo em dois, depois corta
    // o maior pedaco de novo, e repete ate chegar na quantidade pedida.
    // como cada corte separa o espaco em duas partes exatas, o resultado
    // nunca tem sobreposicao nem buraco.
    public static class BspSplitter
    {
        // devolve os retangulos. pode vir menos que targetCount se nao couber mais corte.
        public static List<Area> Split(Area region, int targetCount, int minSize,
                                       System.Random rng)
        {
            List<Area> areas = new List<Area> { region };

            while (areas.Count < targetCount)
            {
                int index = BiggestSplittable(areas, minSize);

                if (index < 0)
                    break;   // ninguem mais pode ser cortado

                Area target = areas[index];

                // corta o lado mais comprido, pra nao gerar comodo estreito.
                // se so um sentido comporta dois comodos minimos, usa ele.
                bool fitsX = target.Width >= minSize * 2;
                bool fitsY = target.Height >= minSize * 2;
                bool alongX = fitsX && fitsY ? target.Width >= target.Height : fitsX;

                // sorteia onde cortar, sem deixar nenhum lado abaixo do minimo
                int length = alongX ? target.Width : target.Height;
                int cut = rng.Next(minSize, length - minSize + 1);

                target.Split(alongX, cut, out Area first, out Area second);

                areas.RemoveAt(index);
                areas.Add(first);
                areas.Add(second);
            }

            return areas;
        }

        // o maior retangulo que ainda aceita corte. -1 se nenhum aceita.
        // escolher sempre o maior e o que mantem os comodos com tamanhos parecidos.
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
