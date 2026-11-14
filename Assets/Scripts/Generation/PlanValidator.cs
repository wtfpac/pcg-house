namespace PCG.Generation
{
    // as regras espaciais do projeto. cada metodo publico e uma regra que pode
    // ser testada sozinha; o Check roda todas e diz qual reprovou a planta.
    public static class PlanValidator
    {
        // devolve a primeira regra violada, ou Failure.None se a planta e valida
        public static Failure Check(FloorPlan plan, HouseSettings s)
        {
            if (!NoGaps(plan))
                return Failure.Gap;

            if (!NoOverlap(plan))
                return Failure.Overlap;

            if (!EveryRoomTouchesFacade(plan))
                return Failure.Facade;

            if (!EveryRoomTouchesCirculation(plan, s.MinSharedWall))
                return Failure.Circulation;

            if (!CirculationIsConnected(plan, s.MinSharedWall))
                return Failure.CirculationSplit;

            return Failure.None;
        }

        // regra 1: nenhuma celula dentro da casa fica sem comodo
        public static bool NoGaps(FloorPlan plan)
        {
            CellMap map = new CellMap(plan);

            for (int x = 0; x < plan.Bounds.Width; x++)
                for (int y = 0; y < plan.Bounds.Height; y++)
                    if (map.At(x, y) == CellMap.Outside)
                        return false;

            return true;
        }

        // regra 2: nenhum comodo invade o espaco de outro
        public static bool NoOverlap(FloorPlan plan)
        {
            for (int i = 0; i < plan.Rooms.Count; i++)
                for (int j = i + 1; j < plan.Rooms.Count; j++)
                    foreach (Area a in plan.Rooms[i].Parts)
                        foreach (Area b in plan.Rooms[j].Parts)
                            if (a.Overlaps(b))
                                return false;

            return true;
        }

        // regra 3: todo comodo encosta na fachada, pra poder ter janela.
        // o corredor e a excecao.
        public static bool EveryRoomTouchesFacade(FloorPlan plan)
        {
            foreach (Room room in plan.Rooms)
                if (room.Type != RoomType.Hall && !TouchesFacade(plan, room))
                    return false;

            return true;
        }

        // regra 4: todo comodo encosta na sala ou no corredor.
        // e o que garante acesso sem atravessar outro comodo.
        public static bool EveryRoomTouchesCirculation(FloorPlan plan, int minShared)
        {
            foreach (Room room in plan.Rooms)
                if (!room.IsCirculation && !TouchesCirculation(plan, room, minShared))
                    return false;

            return true;
        }

        // regra 5: sala e corredor se encostam. sem isso a casa
        // ficaria partida em duas metades sem ligacao.
        public static bool CirculationIsConnected(FloorPlan plan, int minShared)
        {
            int count = plan.CountOf(RoomType.Living) + plan.CountOf(RoomType.Hall);

            if (count <= 1)
                return true;   // com um comodo de circulacao so, nao ha o que ligar

            foreach (Room room in plan.Rooms)
                if (room.IsCirculation && !TouchesCirculation(plan, room, minShared))
                    return false;

            return true;
        }

        // o comodo tem parede externa? basta uma das partes estar na borda da casa.
        public static bool TouchesFacade(FloorPlan plan, Room room)
        {
            foreach (Area part in room.Parts)
                if (part.TouchesEdgeOf(plan.Bounds))
                    return true;

            return false;
        }

        // o comodo e vizinho de algum outro comodo de circulacao?
        public static bool TouchesCirculation(FloorPlan plan, Room room, int minShared)
        {
            foreach (Room other in plan.Rooms)
                if (other != room && other.IsCirculation
                    && Adjacency.AreNeighbours(room, other, minShared))
                    return true;

            return false;
        }
    }
}
