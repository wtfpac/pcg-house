using System;
using System.Collections.Generic;

namespace PCG.Generation
{
    // gera a planta de uma casa cortando o espaco ao meio repetidas vezes
    public class HouseGenerator
    {
        private readonly int minRoomSize;   // menor lado aceitavel de um comodo
        private readonly int targetRooms;   // quantos comodos queremos
        private readonly System.Random random;

        public HouseGenerator(int minRoomSize, int targetRooms, int seed)
        {
            this.minRoomSize = minRoomSize;
            this.targetRooms = targetRooms;
            this.random = new System.Random(seed);
        }

        // ponto de entrada; corta o espaço e da um tipo pra cada comodo
        public FloorPlan Generate(Area bounds, int seed)
        {
            FloorPlan plan = new FloorPlan(bounds, seed);

            List<Area> areas = SplitSpace(bounds);
            AssingTypes(plan, areas);

            return plan;
        }

        // devolve a lista de retangulos. ainda sem tipo de comodo.
        public List<Area> SplitSpace(Area bounds)
        {
            List<Area> areas = new List<Area>();
            areas.Add(bounds);   // comeca com a casa inteira

            // corta ate chegar no numero de comodos desejado
            while (areas.Count < targetRooms)
            {
                int index = PickBiggest(areas);
                Area target = areas[index];

                if (!TrySplit(target, out Area a, out Area b))
                    break;   // nenhum corte possivel, para por aqui

                areas.RemoveAt(index);
                areas.Add(a);
                areas.Add(b);
            }

            return areas;
        }

        // regra de atribuiçao, maior vira sala, menor vira banheiro, 
        // o maior dos q sobrou vira a cozinha, resto vira quartos
        private void AssingTypes(FloorPlan plan, List<Area> areas)
        {
            List<Area> pending = new List<Area>(areas);

            // sala = maior d tods
            int index = PickBiggest(pending);
            plan.Rooms.Add(new Room(pending[index], RoomType.Living));
            pending.RemoveAt(index);
            // banheiro: o menor dos q sobrou
            if (pending.Count > 0)
            {
                index = PickSmallest(pending);
                plan.Rooms.Add(new Room(pending[index], RoomType.Bathroom));
                pending.RemoveAt(index);
            }

            //cozinha: maior dos q sobrou
            if (pending.Count > 0)
            {
                index = PickBiggest(pending);
                plan.Rooms.Add(new Room(pending[index], RoomType.Kitchen));
                pending.RemoveAt(index);
            }

            // todo o resto vira quarto
            foreach (Area a in pending)
                plan.Rooms.Add(new Room(a, RoomType.Bedroom));
        }

        // acha o indice do maior retangulo da lista
        private int PickBiggest(List<Area> areas)
        {
            int best = 0;

            for (int i = 1; i < areas.Count; i++)
            {
                if (areas[i].Size > areas[best].Size)
                    best = i;
            }

            return best;
        }

        // acha o indice do menor retangulo da lsita
        private int PickSmallest(List<Area> areas)
        {
            int best = 0;

            for (int i = 1; i < areas.Count; i++)
            {
                if (areas[i].Size < areas[best].Size)
                    best = i;
            }

            return best;
        }

        // tenta cortar um retangulo em dois. devolve false se nao couber.
        private bool TrySplit(Area area, out Area first, out Area second)
        {
            first = area;
            second = area;

            // so da pra cortar se couberem dois comodos do tamanho minimo
            bool canCutVertical = area.Width >= minRoomSize * 2;
            bool canCutHorizontal = area.Height >= minRoomSize * 2;

            if (!canCutVertical && !canCutHorizontal)
                return false;

            // corta no sentido que deixa os comodos menos compridos.
            // se so um sentido cabe, usa ele.
            bool cutVertical;

            if (canCutVertical && canCutHorizontal)
                cutVertical = area.Width >= area.Height;
            else
                cutVertical = canCutVertical;

            if (cutVertical)
            {
                int cut = PickCut(area.Width);
                first = new Area(area.X, area.Y, cut, area.Height);
                second = new Area(area.X + cut, area.Y, area.Width - cut, area.Height);
            }
            else
            {
                int cut = PickCut(area.Height);
                first = new Area(area.X, area.Y, area.Width, cut);
                second = new Area(area.X, area.Y + cut, area.Width, area.Height - cut);
            }

            return true;
        }

        // sorteia onde cortar, sem deixar nenhum lado menor que o minimo
        private int PickCut(int length)
        {
            int min = minRoomSize;
            int max = length - minRoomSize;

            return random.Next(min, max + 1);
        }
    }
}