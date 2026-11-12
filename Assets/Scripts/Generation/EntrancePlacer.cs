namespace PCG.Generation
{
    // escolhe onde fica a porta da rua: no meio do maior trecho de fachada da sala
    // se a sala nao tocar a fachada, tenta o corredor
    public static class EntrancePlacer
    {
        public static WallKey? Place(FloorPlan plan)
        {
            WallSpan facade = LongestFacade(plan, plan.Find(RoomType.Living));

            if (facade.Length == 0)
                facade = LongestFacade(plan, plan.Find(RoomType.Hall));

            if (facade.Length == 0)
                return null;

            return facade.Middle;
        }

        // o maior trecho de parede externa do comodo. Length zero se nao houver
        private static WallSpan LongestFacade(FloorPlan plan, Room room)
        {
            WallSpan best = new WallSpan();

            if (room == null)
                return best;

            Area b = plan.Bounds;

            // cada parte do comodo pode tocar a borda da casa em ate quatro lados
            foreach (Area a in room.Parts)
            {
                if (a.X == b.X)
                    best = Longer(best, new WallSpan(true, a.X, a.Y, a.Height));

                if (a.Right == b.Right)
                    best = Longer(best, new WallSpan(true, a.Right, a.Y, a.Height));

                if (a.Y == b.Y)
                    best = Longer(best, new WallSpan(false, a.Y, a.X, a.Width));

                if (a.Bottom == b.Bottom)
                    best = Longer(best, new WallSpan(false, a.Bottom, a.X, a.Width));
            }

            return best;
        }

        private static WallSpan Longer(WallSpan a, WallSpan b)
        {
            return b.Length > a.Length ? b : a;
        }
    }
}