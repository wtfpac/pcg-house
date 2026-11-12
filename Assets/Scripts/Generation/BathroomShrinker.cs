using System;

namespace PCG.Generation
{
    // o BSP corta comodos de tamanho parecido, entao o banheiro nasce do
    // tamanho de um quarto. aqui ele e reduzido: uma fatia inteira e cortada
    // de um dos lados e doada ao vizinho. o banheiro continua retangular;
    // quem recebe a fatia pode ficar em L
    public static class BathroomShrinker
    {
        public static void Shrink(FloorPlan plan, HouseSettings s)
        {
            foreach (Room room in plan.Rooms)
            {
                if (room.Type != RoomType.Bathroom)
                    continue;

                // corta um lado por vez, ate caber na area alvo
                // ou ate nenhum lado aceitar mais corte
                while (room.Size > s.MaxBathroomArea && CutOneSide(plan, room, s))
                {
                }
            }
        }

        // tenta os quatro lados e aplica o primeiro corte que der certo
        // lados: 0 esquerda, 1 direita, 2 cima, 3 baixo
        private static bool CutOneSide(FloorPlan plan, Room bathroom, HouseSettings s)
        {
            Area before = bathroom.Parts[0];

            for (int side = 0; side < 4; side++)
            {
                // comeca pelo corte ideal e vai diminuindo ate achar um que sirva
                for (int thickness = IdealCut(before, side, s); thickness > 0; thickness--)
                {
                    Slice(before, side, thickness, out Area slab, out Area rest);

                    Room receiver = FindReceiver(plan, bathroom, slab);

                    if (receiver == null)
                        continue;

                    // o banheiro menor ainda precisa obedecer as regras da casa
                    // testa com o retangulo novo e desfaz se nao servir
                    bathroom.Parts[0] = rest;

                    if (StillValid(plan, bathroom, s))
                    {
                        receiver.Absorb(slab);
                        return true;
                    }

                    bathroom.Parts[0] = before;
                }
            }

            return false;
        }

        // quanto tirar desse lado pra area fechar no alvo,
        // sem deixar o banheiro mais estreito que o lado minimo
        private static int IdealCut(Area area, int side, HouseSettings s)
        {
            bool alongX = side < 2;
            int length = alongX ? area.Width : area.Height;
            int other = alongX ? area.Height : area.Width;

            int target = Math.Max(s.MinBathroomSide, s.MaxBathroomArea / other);

            return length - target;
        }

        // separa uma fatia do lado pedido. "rest" e o que sobra pro banheiro
        private static void Slice(Area area, int side, int thickness,
                                  out Area slab, out Area rest)
        {
            switch (side)
            {
                case 0:  area.Split(true, thickness, out slab, out rest); break;
                case 1:  area.Split(true, area.Width - thickness, out rest, out slab); break;
                case 2:  area.Split(false, thickness, out slab, out rest); break;
                default: area.Split(false, area.Height - thickness, out rest, out slab); break;
            }
        }

        // quem recebe a fatia é um comodo que encosta num lado inteiro dela, dai se encaixa sem deixar buraco
        private static Room FindReceiver(FloorPlan plan, Room bathroom, Area slab)
        {
            bool thin = Math.Min(slab.Width, slab.Height) < 2;

            foreach (Room room in plan.Rooms)
            {
                // corredor e banheiro nao recebem espaco
                if (room == bathroom || room.Type == RoomType.Hall
                    || room.Type == RoomType.Bathroom)
                    continue;

                // fatia de 1 celula so serve se completar um retangulo com
                // o vizinho. solta, viraria um degrau inutil na parede dele
                if (thin)
                {
                    if (room.FormsRectangleWith(slab))
                        return room;

                    continue;
                }

                foreach (Area part in room.Parts)
                    if (FitsAgainst(slab, part))
                        return room;
            }

            return null;
        }

        // a fatia encaixa nessa parte do vizinho?
        private static bool FitsAgainst(Area slab, Area part)
        {
            WallSpan wall = Adjacency.Wall(part, slab);

            if (!wall.CoversSideOf(slab))
                return false;

            // "depth" e o quanto a fatia se afasta da parede em comum. se for
            // mais que o dobro da abertura, vira um braco comprido e estreito.
            int depth = wall.Vertical ? slab.Width : slab.Height;

            return depth <= wall.Length * 2;
        }

        // as mesmas regras que o validador cobra de qualquer comodo:
        // acesso pela circulacao e parede externa pra janela
        private static bool StillValid(FloorPlan plan, Room bathroom, HouseSettings s)
        {
            return PlanValidator.TouchesCirculation(plan, bathroom, s.MinSharedWall)
                && PlanValidator.TouchesFacade(plan, bathroom);
        }
    }
}