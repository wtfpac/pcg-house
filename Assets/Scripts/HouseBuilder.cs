using UnityEngine;
using PCG.Generation;
using System.Collections.Generic;

// le a planta gerada e constroi a casa na cena
public class HouseBuilder : MonoBehaviour
{
    public string version = "0.4";
    public int seed = 42;

    [Header("Tamanho em celulas")]
    public int houseWidth = 20;
    public int houseHeight = 16;
    public int minRoomSize = 5;

    [Header("Escala e medidas")]
    public float unitSize = 0.5f;        // quantos metros vale uma celula
    public float wallHeight = 2.7f;
    public float wallThickness = 0.1f;

    private HashSet<DoorPlacer.WallKey> doors;
    private bool showOverlay = true;

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

        // P salva um print com a versao marcada no canto
        if (Input.GetKeyDown(KeyCode.P))
            StartCoroutine(TakeScreenshot());
    }

    // desenha a versao no canto inferior esquerdo da tela
    void OnGUI()
    {
        if (!showOverlay)
            return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;

        string text = "v" + version + "  |  seed " + seed;

        GUI.Label(new Rect(12, Screen.height - 28, 400, 24), text, style);
    }

    // espera o frame terminar de desenhar antes de capturar,
    // senao o overlay pode nao aparecer no print
    System.Collections.IEnumerator TakeScreenshot()
    {
        showOverlay = true;

        yield return new WaitForEndOfFrame();

        string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string file = "print_v" + version + "_" + stamp + ".png";

        ScreenCapture.CaptureScreenshot(file);
        Debug.Log("print salvo: " + file);
    }

    void Build()
    {
        // apaga a casa anterior pra montar uma nova qnd aperta R
        // comodo pode ter mais de uma parte (formato em L)
        foreach (Room room in plan.Rooms)
            foreach (Area part in room.Parts)
                CreateFloor(part, room.Type);

        Area bounds = new Area(0, 0, houseWidth, houseHeight);

        // o planner corta, nomeia e valida. tenta ate dar certo.
        FloorPlan plan = HousePlanner.Build(bounds, seed, minRoomSize, 5, 7, 2, 2);

        if (plan == null)
        {
            Debug.LogWarning("nenhuma planta valida com a semente " + seed);
            return;
        }

        Debug.Log("comodos: " + plan.RoomCount
                  + " | tentativas: " + HousePlanner.LastAttempts
                  + " | ultima falha: " + HousePlanner.LastFailure);

        var edges = RoomGraph.Build(plan.Rooms, 2);
        doors = DoorPlacer.Place(plan, edges);

        Debug.Log("conexoes: " + edges.Count + " | portas: " + doors.Count);

        foreach (Room room in plan.Rooms)
            CreateFloor(room);

        BuildWalls(plan);
    }

    // cria uma laje fina no lugar de uma parte do comodo. medidas em celulas.
    void CreateFloor(Area area, RoomType type)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = type.ToString();
        go.transform.parent = transform;

        // a Area guarda o canto, a unity posiciona pelo centro
        float cx = area.X + area.Width / 2f;
        float cz = area.Y + area.Height / 2f;

        go.transform.position = new Vector3(cx * unitSize, 0f, cz * unitSize);
        go.transform.localScale = new Vector3(area.Width * unitSize, 0.1f,
                                              area.Height * unitSize);

        go.GetComponent<Renderer>().material.color = ColorFor(type);
    }

    // varre a grade e poe parede onde duas celulas vizinhas
    // pertencem a comodos diferentes, ou onde faz borda com a rua.
    // todas as medidas aqui estao em celulas.
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

                // vizinho da direita: parede vertical, 1 celula de comprimento
                if (map.At(x + 1, y) != here && !IsDoor(true, x + 1, y))
                    CreateWall(x + 1, y + 0.5f, 0f, 1f);

                // vizinho de baixo: parede horizontal
                if (map.At(x, y + 1) != here && !IsDoor(false, x, y + 1))
                    CreateWall(x + 0.5f, y + 1, 1f, 0f);

                // borda esquerda e de cima so existem na primeira
                // coluna e primeira linha, onde nao ha celula anterior
                if (x == 0)
                    CreateWall(x, y + 0.5f, 0f, 1f);

                if (y == 0)
                    CreateWall(x + 0.5f, y, 1f, 0f);
            }
        }
    }

    // essa celula de parede virou porta?
    bool IsDoor(bool vertical, int x, int y)
    {
        return doors.Contains(new DoorPlacer.WallKey(vertical, x, y));
    }

    // cria um pedaco de parede. recebe tudo em celulas e converte
    // pra metros aqui dentro, num lugar so.
    void CreateWall(float x, float z, float sizeX, float sizeZ)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Wall";
        go.transform.parent = transform;

        // espessura da parede e fixa em metros, nao escala com a celula.
        // por isso cada eixo usa o maior entre o tamanho convertido
        // e a espessura minima.
        float worldX = Mathf.Max(sizeX * unitSize, wallThickness);
        float worldZ = Mathf.Max(sizeZ * unitSize, wallThickness);

        go.transform.position = new Vector3(x * unitSize, wallHeight / 2f, z * unitSize);
        go.transform.localScale = new Vector3(worldX, wallHeight, worldZ);

        go.GetComponent<Renderer>().material.color = new Color(0.90f, 0.88f, 0.84f);
    }

    // uma cor pra cada tipo
    Color ColorFor(RoomType type)
    {
        if (type == RoomType.Living) return new Color(0.78f, 0.42f, 0.20f);    // laranja
        if (type == RoomType.Kitchen) return new Color(0.30f, 0.60f, 0.32f);   // verde
        if (type == RoomType.Bathroom) return new Color(0.22f, 0.48f, 0.78f);  // azul
        if (type == RoomType.Hall) return new Color(0.85f, 0.80f, 0.35f);      // amarelo

        return new Color(0.58f, 0.42f, 0.75f);                                  // quarto, roxo
    }
}