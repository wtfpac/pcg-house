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

    [Header("Escala e me88888888887777y didas")]
    public float unitSize = 0.5f;        // quantos metros vale uma celula
    public float wallHeight = 2.7f;
    public float wallThickness = 0.1f;

    private HashSet<DoorPlacer.WallKey> doors;
    private bool showOverlay = true;

    // dados da ultima geracao, mostrados na tela e no console
    private int lastRooms;
    private int lastEdges;
    private int lastDoors;
    private int lastAttempts;
    private float lastMillis;
    private string lastFailure = "-";

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

    // painel de dados no canto superior esquerdo
    void OnGUI()
    {
        if (!showOverlay)
            return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 15;
        style.normal.textColor = Color.white;

        string[] lines =
        {
            "semente: " + seed,
            "comodos: " + lastRooms,
            "portas: " + lastDoors,
            "tentativas: " + lastAttempts,
            "tempo: " + lastMillis.ToString("F2") + " ms",
            "ultima falha: " + lastFailure,
            "v" + version
        };

        // fundo escuro, pra ler em cima de qualquer cor
        GUI.Box(new Rect(8, 8, 220, lines.Length * 19 + 10), "");

        int y = 12;

        foreach (string line in lines)
        {
            GUI.Label(new Rect(14, y, 400, 20), line, style);
            y += 19;
        }
    }

    // espera o frame terminar de desenhar antes de capturar,
    // senao o overlay pode nao aparecer no print
    System.Collections.IEnumerator TakeScreenshot()
    {
        showOverlay = true;

        yield return new WaitForEndOfFrame();

        string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string file = "print_v" + version + "_seed" + seed + "_" + stamp + ".png";

        ScreenCapture.CaptureScreenshot(file);
        Debug.Log("print salvo: " + file);
    }

    void Build()
    {
        // apaga a casa anterior pra montar uma nova qnd aperta R
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        Area bounds = new Area(0, 0, houseWidth, houseHeight);

        // cronometra so a geracao, sem contar a construcao da cena
        var watch = System.Diagnostics.Stopwatch.StartNew();
        FloorPlan plan = HousePlanner.Build(bounds, seed, minRoomSize, 5, 7, 2, 2);
        watch.Stop();

        lastMillis = (float)watch.Elapsed.TotalMilliseconds;

        if (plan == null)
        {
            lastRooms = 0;
            lastEdges = 0;
            lastDoors = 0;
            lastAttempts = HousePlanner.MaxAttempts;
            lastFailure = HousePlanner.FailureSummary();

            Debug.LogWarning("semente " + seed + ": nenhuma planta valida em "
                             + HousePlanner.MaxAttempts + " tentativas");
            return;
        }

        var edges = RoomGraph.Build(plan.Rooms, 2);
        doors = DoorPlacer.Place(plan, edges);

        // porta da rua
        var entrance = EntrancePlacer.Place(plan);

        if (entrance.HasValue)
            doors.Add(entrance.Value);
        else
            Debug.LogWarning("sem entrada: nem sala nem corredor tocam a fachada");

        lastRooms = plan.RoomCount;
        lastEdges = edges.Count;
        lastDoors = doors.Count;
        lastAttempts = HousePlanner.LastAttempts;
        lastFailure = HousePlanner.FailureSummary();

        Debug.Log($"semente {seed} | {lastRooms} comodos | {lastEdges} conexoes | "
                  + $"{lastDoors} portas | {lastAttempts} tentativas | "
                  + $"{lastMillis:F2} ms | ultima falha: {lastFailure}");

        // comodo pode ter mais de uma parte (formato em L)
        foreach (Room room in plan.Rooms)
            foreach (Area part in room.Parts)
                CreateFloor(part, room.Type);

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

                // borda esquerda e de cima so existem na primeira coluna
                // e primeira linha. consultam IsDoor igual as outras,
                // senao a porta de entrada some quando cai nesses lados.
                if (x == 0 && !IsDoor(true, x, y))
                    CreateWall(x, y + 0.5f, 0f, 1f);

                if (y == 0 && !IsDoor(false, x, y))
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