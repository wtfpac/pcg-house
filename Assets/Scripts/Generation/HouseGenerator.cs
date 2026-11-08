using System;
using System.Collections.Generic;

namespace PCG.Generation
{
    // gera a planta de uma casa cortando o espaco ao meio repetidas vezes
    public class HouseGenerator
    {
        private readonly int minRoomSize;   // menor lado aceitavel de um comodo
        private readonly System.Random random;

        public HouseGenerator(int minRoomSize, int seed)
        {
            this.minRoomSize = minRoomSize;
            this.random = new System.Random(seed);
        }

        // ponto de entrada. parte a casa em faixa da sala e faixa dos comodos.
        public FloorPlan Generate(Area bounds, int seed)
        {
            FloorPlan plan = new FloorPlan(bounds, seed);

            // a sala ocupa entre 35% e 45% da casa, encostada numa borda
            bool socialAtBottom = random.Next(2) == 0;
            int socialDepth = PickSocialDepth(bounds.Height);

            Area social;   // faixa da sala e cozinha
            Area rest;     // faixa dos quartos e banheiro

            if (socialAtBottom)
            {
                rest = new Area(bounds.X, bounds.Y, bounds.Width, bounds.Height - socialDepth);
                social = new Area(bounds.X, bounds.Y + rest.Height, bounds.Width, socialDepth);
            }
            else
            {
                social = new Area(bounds.X, bounds.Y, bounds.Width, socialDepth);
                rest = new Area(bounds.X, bounds.Y + socialDepth, bounds.Width, bounds.Height - socialDepth);
            }

            BuildSocial(plan, social);
            BuildPrivate(plan, rest);

            return plan;
        }


        // profundidade da faixa social, entre 35% e 45% da altura
        private int PickSocialDepth(int totalHeight)
        {
            int min = (int)(totalHeight * 0.35f);
            int max = (int)(totalHeight * 0.45f);

            if (min < minRoomSize)
                min = minRoomSize;

            if (max < min)
                max = min;

            return random.Next(min, max + 1);
        }

        // a faixa social vira sala mais cozinha, com um corte so
        private void BuildSocial(FloorPlan plan, Area social)
        {
            // se nao couber dividir, a faixa inteira vira sala
            if (social.Width < minRoomSize * 2)
            {
                plan.Rooms.Add(new Room(social, RoomType.Living));
                return;
            }

            // a cozinha fica num dos cantos, ocupando entre 30% e 40%
            int kitchenWidth = PickPart(social.Width, 0.30f, 0.40f);
            bool kitchenOnRight = random.Next(2) == 0;

            Area living;
            Area kitchen;

            if (kitchenOnRight)
            {
                living = new Area(social.X, social.Y, social.Width - kitchenWidth, social.Height);
                kitchen = new Area(social.Right - kitchenWidth, social.Y, kitchenWidth, social.Height);
            }
            else
            {
                kitchen = new Area(social.X, social.Y, kitchenWidth, social.Height);
                living = new Area(social.X + kitchenWidth, social.Y, social.Width - kitchenWidth, social.Height);
            }

            plan.Rooms.Add(new Room(living, RoomType.Living));
            plan.Rooms.Add(new Room(kitchen, RoomType.Kitchen));
        }

        // a faixa intima e fatiada em colunas, entao todo comodo
        // encosta na faixa social que esta logo acima ou abaixo
        private void BuildPrivate(FloorPlan plan, Area rest)
        {
                        // largura confortavel pra um quarto
            int comfortableWidth = minRoomSize + 2;
            int bathroomWidth = minRoomSize;

            // quantos quartos cabem, reservando ja um banheiro
            int maxBedrooms = (rest.Width - bathroomWidth) / comfortableWidth;

            if (maxBedrooms < 1)
                maxBedrooms = 1;

            if (maxBedrooms > 3)
                maxBedrooms = 3;

            int bedroomCount = random.Next(1, maxBedrooms + 1);

            // regra: a partir de 3 quartos, a casa ganha um segundo banheiro
            int bathroomCount = bedroomCount >= 3 ? 2 : 1;

            // so cabe o segundo banheiro se ainda sobrar espaco
            if (rest.Width - bathroomCount * bathroomWidth < bedroomCount * minRoomSize)
                bathroomCount = 1;

            int count = bedroomCount + bathroomCount;

            int bedroomSpace = rest.Width - bathroomCount * bathroomWidth;
            int baseWidth = bedroomSpace / bedroomCount;
            int leftover = bedroomSpace - baseWidth * bedroomCount;

            // sorteia em quais posicoes da fileira entram os banheiros
            HashSet<int> bathroomSlots = new HashSet<int>();

            while (bathroomSlots.Count < bathroomCount)
                bathroomSlots.Add(random.Next(count));

            int offset = 0;
            int bedroomsDone = 0;

            for (int i = 0; i < count; i++)
            {
                int width;
                RoomType type;

                if (bathroomSlots.Contains(i))
                {
                    width = bathroomWidth;
                    type = RoomType.Bathroom;
                }
                else
                {
                    width = baseWidth + (bedroomsDone < leftover ? 1 : 0);
                    type = RoomType.Bedroom;
                    bedroomsDone++;
                }

                plan.Rooms.Add(new Room(new Area(rest.X + offset, rest.Y, width, rest.Height), type));

                offset += width;
            }
        }

        // sorteia um pedaco de um comprimento, dentro de uma faixa percentual,
        // sem deixar nenhum lado menor que o minimo
        private int PickPart(int total, float minPercent, float maxPercent)
        {
            int min = (int)(total * minPercent);
            int max = (int)(total * maxPercent);

            if (min < minRoomSize)
                min = minRoomSize;

            if (max > total - minRoomSize)
                max = total - minRoomSize;

            if (max < min)
                max = min;

            return random.Next(min, max + 1);
        }
    }
}