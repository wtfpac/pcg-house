namespace PCG.Generation
{
    // grade que diz, pra cada celula, a qual comodo ela pertence
    // usamos pra achar onde vai parede: entre comodos diferentes
    public class CellMap
    {
        public const int Outside = -1;

        private readonly int[,] cells;
        private readonly int width;
        private readonly int height;

        public CellMap(FloorPlan plan)
        {
            width = plan.Bounds.Width;
            height = plan.Bounds.Height;
            cells = new int[width, height];

            // tudo comeca como lado de fora
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    cells[x, y] = Outside;

            // cada comodo pinta todas as suas partes com o proprio indice
            for (int i = 0; i < plan.Rooms.Count; i++)
                foreach (Area part in plan.Rooms[i].Parts)
                    for (int x = part.X; x < part.Right; x++)
                        for (int y = part.Y; y < part.Bottom; y++)
                            if (Inside(x, y))
                                cells[x, y] = i;
        }

        // devolve o comodo da celula, ou Outside se estiver fora da casa
        public int At(int x, int y)
        {
            return Inside(x, y) ? cells[x, y] : Outside;
        }

        private bool Inside(int x, int y)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }
    }
}
