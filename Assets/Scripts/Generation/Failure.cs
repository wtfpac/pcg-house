namespace PCG.Generation
{
    // motivo pelo qual uma tentativa de planta foi descartada
    public enum Failure
    {
        None,
        Geometry,
        Gap,
        Overlap,
        Facade,
        Circulation,
        CirculationSplit,
        RoomSize
    }
}
