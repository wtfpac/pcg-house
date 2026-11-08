using UnityEngine;
using PCG.Generation;
using System.Collections.Generic;

// le a platna gerada e constroi o chao de cada comodo na cena
public class HouseBuilder : MonoBehaviour
{
    public int seed = 42;
    public int houseWidth = 12;
    public int houseHeight = 9;
    public int minRoomSize = 3;
    public float wallHeight = 2.8f;
    public float wallThickness = 0.15f;
    private HashSet<DoorPlacer.WallKey> doors;

    void Start()
    {
        Build();
    }
    void Update()
    {
        // R gera uma casa nova com semente random
        if (Input.GetKeyDown(KeyCode.R))
        {
            seed = Random.Range(0, 999999);
            Build();
        }

        // P salva um print na pasta do projeto
        if (Input.GetKeyDown(KeyCode.P))
        {
            string file = "print_seed" + seed + ".png";
            ScreenCapture.CaptureScreenshot(file);
            Debug.Log("print salvo: " + file);
        }
    }

    void Build()
    {
        // apaga a casa anterior pra montar uma nova caso aperte R
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        
        HouseGenerator generator = new HouseGenerator(minRoomSize, seed);
        FloorPlan plan = generator.Generate(new Area(0, 0, houseWidth, houseHeight), seed);

        Debug.Log("comodos gerados: " + plan.RoomCount);

        var edges = RoomGraph.Build(plan.Rooms, 2);
        Debug.Log("conexoes entre comodos: " + edges.Count);

        doors = DoorPlacer.Place(plan, edges);
        Debug.Log("portas abertas: " + doors.Count);

        foreach (Room room in plan.Rooms)
            CreateFloor(room);

        BuildWalls(plan);
    }
    // cria uma laje fina no lugar do comodo, com cor confome o tipo
    void CreateFloor(Room room)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = room.Type.ToString();
        go.transform.parent = transform;

        // a Area guarda o canto, a unity posiciona pelo centro
        // entao somamos metade da largura e da altura
        float x = room.Area.X + room.Area.Width / 2f;
        float z = room.Area.Y + room.Area.Height / 2f;

        go.transform.position = new Vector3(x, 0f, z);
        go.transform.localScale = new Vector3(room.Area.Width, 0.1f, room.Area.Height);

        go.GetComponent<Renderer>().material.color = ColorFor(room.Type);     
    }

    // varre a grade e poe parede onde duas celulas vizinhas
    // pertencem a comodos diferentes, ou onde faz borda com a rua
    void BuildWalls(FloorPlan plan)
    {
        CellMap map = new CellMap(plan);
        Area b = plan.Bounds;

        for (int x = 0; x < b.Width; x++)
        {
            for (int y = 0; y < b.Height; y++)
            {
                int here = map.At(x, y);

                if (here == CellMap.Outside)
                    continue;

                // vizinho da direita
                if (map.At(x + 1, y) != here && !IsDoor(true, x + 1, y))
                    CreateWall(x + 1, y + 0.5f, wallThickness, 1f);

                // vizinho de baixo
                if (map.At(x, y + 1) != here && !IsDoor(false, x, y + 1))
                    CreateWall(x + 0.5f, y + 1, 1f, wallThickness);

                // borda esquerda e de cima so existem na primeira
                // coluna e primeira linha, onde nao ha celula anterior
                if (x == 0)
                    CreateWall(x, y + 0.5f, wallThickness, 1f);

                if (y == 0)
                    CreateWall(x + 0.5f, y, 1f, wallThickness);
            }
        }
    }

    // essa celula de parede virou porta?
    bool IsDoor(bool vertical, int x, int y)
    {
        return doors.Contains(new DoorPlacer.WallKey(vertical, x, y));
    }

    // cria um blodco d parede na posiçao e tamanho informados
    void CreateWall(float x, float z, float sizeX, float sizeZ)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Wall";
        go.transform.parent = transform;

        go.transform.position = new Vector3(x , wallHeight / 2f, z);
        go.transform.localScale = new Vector3(sizeX, wallHeight, sizeZ);

        go.GetComponent<Renderer>().material.color = new Color(0.90f, 0.88f, 0.84f);
    }

    // uma cor p cada tipo
    Color ColorFor(RoomType type)
    {
        if (type == RoomType.Living) return new Color(0.78f, 0.42f, 0.20f);    // laranja
        if (type == RoomType.Kitchen) return new Color(0.30f, 0.60f, 0.32f);   // verde
        if (type == RoomType.Bathroom) return new Color(0.22f, 0.48f, 0.78f);  // azul

        return new Color(0.58f, 0.42f, 0.75f);   // quarto, roxo
    }
}