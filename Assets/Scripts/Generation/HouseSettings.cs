namespace PCG.Generation
{
    [System.Serializable]
    public class HouseSettings
    {
        // tamanho da casa
        public int MinWidth = 20;
        public int MaxWidth = 26;
        public int MinHeight = 16;
        public int MaxHeight = 20;

        // comodos
        public int MinRoomSize = 5;
        public int MinRooms = 5;
        public int MaxRooms = 10;
        public int CellsPerRoom = 55;
        public int MaxRoomCells = 120;
        public int MinSharedWall = 2;
        public int DoorWidth = 2;
        public float OpenLivingHallChance = 0.5f;

        // corredor
        public int CorridorWidth = 2;
        public int MinCorridorCut = 3;
        public int MinCorridorRooms = 3;

        // banheiro
        public int MaxBathroomArea = 20;
        public int MinBathroomSide = 4;
        public int SecondBathroomAt = 6;

        public int MaxAttempts = 60;

        public int DoorWall => System.Math.Max(MinSharedWall, DoorWidth);
    }
}