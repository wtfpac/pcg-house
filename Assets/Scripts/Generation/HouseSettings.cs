namespace PCG.Generation
{
    [System.Serializable]
    public class HouseSettings
    {
        // tamanho da casa
        public int MinWidth = 18;
        public int MaxWidth = 24;
        public int MinHeight = 14;
        public int MaxHeight = 18;

        // comodos
        public int MinRoomSize = 5;
        public int MinRooms = 5;
        public int MaxRooms = 7;
        public int MinSharedWall = 2;
        public int DoorWidth = 3;

        // corredor
        public int CorridorWidth = 2;
        public int MinCorridorCut = 3;

        // banheiro
        public int MaxBathroomArea = 20;
        public int MinBathroomSide = 4;
        public int SecondBathroomAt = 6;

        public int MaxAttempts = 60;

        public int DoorWall => System.Math.Max(MinSharedWall, DoorWidth);
    }
}