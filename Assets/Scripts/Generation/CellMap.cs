namespace PCG.Generation
{
    // grade que diz, pra cada celula, a qual comodo ela pertence.
    // usamos pra achar onde vai parede: entre comodos diferentes.
    public class CellMap
    {
        public const int Outside = -1;

        private readonly int[,] cells;
        private readonly Area bounds;

        public CellMap(FloorPlan plan)
        {
            bounds = plan.Bounds;
            cells = new int[bounds.Width, bounds.Height];

            // tudo comeca como lado de fora
            for (int x = 0; x < bounds.Width; x++)
                for (int y = 0; y < bounds.Height; y++)
                    cells[x, y] = Outside;

            // cada comodo pinta sua area com o proprio indice
            for (int i = 0; i < plan.Rooms.Count; i++)
            {
                Area a = plan.Rooms[i].Area;

                for (int x = a.X; x < a.Right; x++)
                    for (int y = a.Y; y < a.Bottom; y++)
                        cells[x, y] = i;
            }
        }

        // devolve o comodo da celula, ou Outside se estiver fora da casa
        public int At(int x, int y)
        {
            if (x < 0 || y < 0 || x >= bounds.Width || y >= bounds.Height)
                return Outside;

            return cells[x, y];
        }
    }
}