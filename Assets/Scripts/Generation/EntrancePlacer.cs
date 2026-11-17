namespace PCG.Generation
{
    // escolhe onde fica a porta da rua. prefere a sala; (analisar se vamos continuar com ese trecho d poder ser no corredor)
    // se ela nao tocar fachada, tenta o corredor
    public static class EntrancePlacer
    {
        public static WallSpan? Place(FloorPlan plan, int doorWidth)
        {
            // primeiro tenta a sala, que e o normal em casa terrea
            WallSpan facade = LongestFacade(plan, plan.Find(RoomType.Living));

            // sem sala na fachada, cai pro corredor
            if (facade.Length == 0)
                facade = LongestFacade(plan, plan.Find(RoomType.Hall));

            if (facade.Length == 0)
                return null;

            // pega a do meio, pra nao cair bem no canto
            return facade.Centered(doorWidth);
        }

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