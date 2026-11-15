namespace PCG.Generation
{
    // REGRAS ESPACIAIS
    public static class PlanValidator
    {
        // roda todas as regras e diz qual falhou primeiro
        public static Failure Check(FloorPlan plan, HouseSettings s)
        {
            if (!NoGaps(plan))
                return Failure.Gap;

            if (!NoOverlap(plan))
                return Failure.Overlap;

            if (!EveryRoomTouchesFacade(plan))
                return Failure.Facade;

            if (!EveryRoomTouchesCirculation(plan, s.DoorWall))
                return Failure.Circulation;

            if (!CirculationIsConnected(plan, s.DoorWall))
                return Failure.CirculationSplit;

            return Failure.None;
        }

        // regra 1: nenhuma celula dentro da casa pode ficar sem comodo
        public static bool NoGaps(FloorPlan plan)
        {
            CellMap map = new CellMap(plan);

            for (int x = 0; x < plan.Bounds.Width; x++)
                for (int y = 0; y < plan.Bounds.Height; y++)
                    if (map.At(x, y) == CellMap.Outside)
                        return false;

            return true;
        }

        // regra 2: nenhuma parte de um comodo invade a de outro
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

        // regra 3: todo comodo encosta em parede externa, pra poder ter janela.
        // o hall e excecao: corredor nao precisa de janela.
        public static bool EveryRoomTouchesFacade(FloorPlan plan)
        {
            foreach (Room room in plan.Rooms)
                if (room.Type != RoomType.Hall && !TouchesFacade(plan, room))
                    return false;

            return true;
        }

        // regra 4: todo comodo encosta em algum comodo de circulacao
        public static bool EveryRoomTouchesCirculation(FloorPlan plan, int minShared)
        {
            foreach (Room room in plan.Rooms)
                if (!room.IsCirculation && !TouchesCirculation(plan, room, minShared))
                    return false;

            return true;
        }

        // regra 5: os comodos de circulacao estao ligados entre si
        public static bool CirculationIsConnected(FloorPlan plan, int minShared)
        {
            int count = plan.CountOf(RoomType.Living) + plan.CountOf(RoomType.Hall);

            if (count <= 1)
                return true;

            foreach (Room room in plan.Rooms)
                if (room.IsCirculation && !TouchesCirculation(plan, room, minShared))
                    return false;

            return true;
        }

        // basta uma das partes encostar na fachada
        public static bool TouchesFacade(FloorPlan plan, Room room)
        {
            foreach (Area part in room.Parts)
                if (part.TouchesEdgeOf(plan.Bounds))
                    return true;

            return false;
        }

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
