namespace PCG.Generation
{
    // lista d motivos q uma planta foi descartada
    public enum Failure
    {
        None,
        Geometry,           // o corredor nao coube na casa
        Gap,                // sobrou celula sem comodo
        Overlap,            // dois comodos ocupam a mesma celula
        Facade,             // comodo sem parede externa, ou seja, sem janela
        Circulation,        // comodo que nao encosta nem na sala nem no corredor
        CirculationSplit    // sala e corredor nao se encostam
    }
}