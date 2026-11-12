namespace PCG.Generation
{
    // todos os parametros do gerador num lugar so. as medidas sao em celulas
    // da malha; quem converte celula em metro e a camada da unity.
    [System.Serializable]
    public class HouseSettings
    {
        // tamanho da casa
        public int Width = 20;
        public int Height = 16;

        // comodos
        public int MinRoomSize = 5;         // menor lado que um comodo pode ter
        public int MinRooms = 5;            // quantidade de comodos, sem contar o corredor
        public int MaxRooms = 7;
        public int MinSharedWall = 2;       // parede em comum minima pra caber uma porta

        // corredor
        public int CorridorWidth = 2;
        public int MinCorridorCut = 3;      // menor pedaco que vale a pena tirar da ponta

        // banheiro
        public int MaxBathroomArea = 20;    // area alvo
        public int MinBathroomSide = 4;     // menor lado que ele pode ter depois de encolher
        public int SecondBathroomAt = 6;    // com essa quantidade de comodos, a casa ganha o segundo

        // gerar e testar: quantas plantas tentar antes de desistir da semente
        public int MaxAttempts = 60;
    }
}