namespace PCG.Generation
{
    // grade que diz, pra cada celula, a qual comodo ela pertence
    // usamos pra achar onde vai parede: entre comodos diferentes
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

            // cada comodo pinta todas as suas partes com o proprio indice
            for (int i = 0; i < plan.Rooms.Count; i++)
                foreach (Area part in plan.Rooms[i].Parts)
                    Paint(part, i);
        }

        private void Paint(Area a, int id)
        {
            for (int x = a.X; x < a.Right; x++)
                for (int y = a.Y; y < a.Bottom; y++)
                    if (Inside(x, y))
                        cells[x, y] = id;
        }

        private bool Inside(int x, int y)
        {
            return x >= 0 && y >= 0 && x < bounds.Width && y < bounds.Height;
        }

        // devolve o comodo da celula, ou Outside se estiver fora da casa
        public int At(int x, int y)
        {
            if (!Inside(x, y))
                return Outside;

            return cells[x, y];
        }
    }
}