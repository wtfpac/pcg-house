using UnityEngine;
using PCG.Generation;

// le a platna gerada e constroi o chao de cada comodo na cena
public class HouseBuilder : MonoBehaviour
{
    public int seed = 42;
    public int houseWidth = 12;
    public int houseHeight = 9;
    public int minRoomSize = 3;
    public int targetRooms = 5;

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
    }

    void Build()
    {
        // apaga a casa anterior pra montar uma nova caso aperte R
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        
        HouseGenerator generator = new HouseGenerator(minRoomSize, targetRooms, seed);
        FloorPlan plan = generator.Generate(new Area(0, 0, houseWidth, houseHeight), seed);

        Debug.Log("comodos gerados: " + plan.RoomCount);

        foreach (Room room in plan.Rooms)
            CreateFloor(room);
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

    // uma cor p cada tipo
    Color ColorFor(RoomType type)
    {
        if (type == RoomType.Living) return new Color(0.78f, 0.42f, 0.20f);    // laranja
        if (type == RoomType.Kitchen) return new Color(0.30f, 0.60f, 0.32f);   // verde
        if (type == RoomType.Bathroom) return new Color(0.22f, 0.48f, 0.78f);  // azul

        return new Color(0.58f, 0.42f, 0.75f);   // quarto, roxo
    }
}