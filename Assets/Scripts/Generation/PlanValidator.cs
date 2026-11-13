using System.Collections.Generic;

namespace PCG.Generation
{
    // REGRAS ESPACIAIS
    public static class PlanValidator
    {
        public enum Failure
        {
            None,
            Gap,
            Overlap,
            Facade,
            Circulation,
            CirculationSplit
        }

        private static bool IsCirculation(RoomType type)
        {
            return type == RoomType.Living || type == RoomType.Hall;
        }

        // regra 1: nenhuma celula dentro da casa pode ficar sem comodo
        public static bool NoGaps(FloorPlan plan)
        {
            CellMap map = new CellMap(plan);
            Area b = plan.Bounds;

            for (int x = 0; x < b.Width; x++)
                for (int y = 0; y < b.Height; y++)
                    if (map.At(x, y) == CellMap.Outside)
                        return false;

            return true;
        }

        // regra 2: nenhuma parte de um comodo invade a de outro
        public static bool NoOverlap(FloorPlan plan)
        {
            for (int i = 0; i < plan.Rooms.Count; i++)
            {
                for (int j = i + 1; j < plan.Rooms.Count; j++)
                {
                    foreach (Area a in plan.Rooms[i].Parts)
                    {
                        foreach (Area b in plan.Rooms[j].Parts)
                        {
                            // dois retangulos se cruzam quando ha sobreposicao
                            // nos dois eixos ao mesmo tempo
                            bool crossX = Adjacency.Overlap(a.X, a.Right, b.X, b.Right) > 0;
                            bool crossY = Adjacency.Overlap(a.Y, a.Bottom, b.Y, b.Bottom) > 0;

                            if (crossX && crossY)
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        // regra 3: todo comodo encosta em parede externa, pra poder ter janela.
        // o hall e excecao: corredor nao precisa de janela.
        public static bool EveryRoomTouchesFacade(FloorPlan plan)
        {
            Area b = plan.Bounds;

            foreach (Room room in plan.Rooms)
            {
                if (room.Type == RoomType.Hall)
                    continue;

                bool touches = false;

                // basta uma das partes encostar na fachada
                foreach (Area a in room.Parts)
                {
                    if (a.X == b.X || a.Right == b.Right
                        || a.Y == b.Y || a.Bottom == b.Bottom)
                    {
                        touches = true;
                        break;
                    }
                }

                if (!touches)
                    return false;
            }

            return true;
        }

        // regra 4: todo comodo encosta em algum comodo de circulacao
        public static bool EveryRoomTouchesCirculation(FloorPlan plan, int minShared)
        {
            for (int i = 0; i < plan.Rooms.Count; i++)
            {
                if (IsCirculation(plan.Rooms[i].Type))
                    continue;

                if (!TouchesAnyCirculation(plan, i, minShared))
                    return false;
            }

            return true;
        }

        private static bool TouchesAnyCirculation(FloorPlan plan, int index, int minShared)
        {
            for (int j = 0; j < plan.Rooms.Count; j++)
            {
                if (j == index || !IsCirculation(plan.Rooms[j].Type))
                    continue;

                if (Adjacency.AreNeighbours(plan.Rooms[index], plan.Rooms[j], minShared))
                    return true;
            }

            return false;
        }

        // regra 5: os comodos de circulacao estao ligados entre si
        public static bool CirculationIsConnected(FloorPlan plan, int minShared)
        {
            List<int> circulation = new List<int>();

            for (int i = 0; i < plan.Rooms.Count; i++)
                if (IsCirculation(plan.Rooms[i].Type))
                    circulation.Add(i);

            if (circulation.Count <= 1)
                return true;

            foreach (int i in circulation)
            {
                bool linked = false;

                foreach (int j in circulation)
                {
                    if (i == j)
                        continue;

                    if (Adjacency.AreNeighbours(plan.Rooms[i], plan.Rooms[j], minShared))
                    {
                        linked = true;
                        break;
                    }
                }

                if (!linked)
                    return false;
            }

            return true;
        }

        // roda todas as regras e diz qual falhou primeiro
        public static Failure Check(FloorPlan plan, int minShared)
        {
            if (!NoGaps(plan))
                return Failure.Gap;

            if (!NoOverlap(plan))
                return Failure.Overlap;

            if (!EveryRoomTouchesFacade(plan))
                return Failure.Facade;

            if (!EveryRoomTouchesCirculation(plan, minShared))
                return Failure.Circulation;

            if (!CirculationIsConnected(plan, minShared))
                return Failure.CirculationSplit;

            return Failure.None;
        }
    }
}