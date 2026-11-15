using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using PCG.Generation;

public class HouseBuilder : MonoBehaviour
{
    public int seed = 123456789;
    public HouseSettings settings = new HouseSettings();

    [Header("Escala e medidas, em metros")]
    public float unitSize = 0.5f;        // qts metros vale uma celula
    public float wallHeight = 2.5f;
    public float wallThickness = 0.1f;
    public float doorHeight = 2.1f;
    public float ceilingThickness = 0.1f;

    [Header("Porta e teto")]
    [Range(1, 2)]
    public int doorWidthCells = 1;
    public bool showCeiling = true;      // T Liga e desliga teto

    private static readonly Color WallColor = new Color(0.90f, 0.88f, 0.84f);
    private static readonly Color CeilingColor = new Color(0.96f, 0.96f, 0.94f);

    private GenerationResult last;

    private HashSet<WallKey> openings = new HashSet<WallKey>();

    private GameObject ceiling;

    void Start()
    {
        Build();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        // sem teclado conectado nao tem o que ler
        if (keyboard == null)
            return;

        if (keyboard.rKey.wasPressedThisFrame)
        {
            seed = Random.Range(HousePlanner.MinSeed, HousePlanner.MaxSeed + 1);
            Build();
        }

        if (keyboard.tKey.wasPressedThisFrame)
        {
            showCeiling = !showCeiling;

            if (ceiling != null)
                ceiling.SetActive(showCeiling);
        }

        // P salva um print com a versao marcada no canto
        if (keyboard.pKey.wasPressedThisFrame)
            StartCoroutine(TakeScreenshot());
    }

    void Build()
    {
        // apaga a casa anterior pra montar uma nova qnd aperta R
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        ceiling = null;

        last = HousePlanner.Generate(settings, seed);

        if (!last.Success)
        {
            Debug.LogWarning($"semente {seed}: nenhuma planta valida em {last.Attempts} "
                             + $"tentativas | falhas: {last.FailureSummary()}");
            return;
        }

        FloorPlan plan = last.Plan;

        Debug.Log($"semente {seed} | {plan.RoomCount} comodos | {plan.Edges.Count} conexoes | "
                  + $"{plan.Doors.Count} portas | {last.Attempts} tentativas | "
                  + $"{last.Milliseconds:F2} ms | falhas: {last.FailureSummary()}");

        if (!plan.Entrance.HasValue)
            Debug.LogWarning("sem entrada: nem sala nem corredor tocam a fachada");

        openings = OpenDoors(plan);

        // comodo pode ter mais de uma parte (formato em L)
        foreach (Room room in plan.Rooms)
            foreach (Area part in room.Parts)
                CreateFloor(part, room.Type);

        BuildWalls(plan);
        BuildCeiling(plan);
    }

    HashSet<WallKey> OpenDoors(FloorPlan plan)
    {
        HashSet<WallKey> open = new HashSet<WallKey>(plan.Doors);

        if (doorWidthCells < 2)
            return open;

        foreach (WallKey door in plan.Doors)
        {
            if (door.Vertical)
                open.Add(new WallKey(true, door.X, door.Y - 1));
            else
                open.Add(new WallKey(false, door.X - 1, door.Y));
        }

        return open;
    }

    void CreateFloor(Area area, RoomType type)
    {
        float centerX = (area.X + area.Width / 2f) * unitSize;
        float centerZ = (area.Y + area.Height / 2f) * unitSize;

        CreateBox(type.ToString(),
                  new Vector3(centerX, 0f, centerZ),
                  new Vector3(area.Width * unitSize, 0.1f, area.Height * unitSize),
                  ColorFor(type));
    }

    // varre a grade e poe parede onde duas celulas vizinhas pertencem a comodos diferentes, ou onde faz borda com a rua
    void BuildWalls(FloorPlan plan)
    {
        CellMap map = new CellMap(plan);
        int width = plan.Bounds.Width;
        int height = plan.Bounds.Height;

        for (int x = 0; x <= width; x++)
            for (int y = 0; y < height; y++)
                if (map.At(x - 1, y) != map.At(x, y))
                    CreateWall(new WallKey(true, x, y));

        for (int x = 0; x < width; x++)
            for (int y = 0; y <= height; y++)
                if (map.At(x, y - 1) != map.At(x, y))
                    CreateWall(new WallKey(false, x, y));
    }

    // cria um pedaco de parede. recebe tudo em celulas e converte pra metros aqui dentro, num lugar so
    void CreateWall(WallKey key)
    {
        // a chave marca o inicio do pedaco. o centro fica meia celula adiante, no sentido em que a parede corre
        float centerX = (key.Vertical ? key.X : key.X + 0.5f) * unitSize;
        float centerZ = (key.Vertical ? key.Y + 0.5f : key.Y) * unitSize;

        float sizeX = key.Vertical ? wallThickness : unitSize;
        float sizeZ = key.Vertical ? unitSize : wallThickness;

        if (openings.Contains(key))
        {
            float lintel = wallHeight - doorHeight;

            if (lintel > 0f)
                CreateBox("Lintel",
                          new Vector3(centerX, doorHeight + lintel / 2f, centerZ),
                          new Vector3(sizeX, lintel, sizeZ),
                          WallColor);

            return;
        }

        CreateBox("Wall",
                  new Vector3(centerX, wallHeight / 2f, centerZ),
                  new Vector3(sizeX, wallHeight, sizeZ),
                  WallColor);
    }

    void BuildCeiling(FloorPlan plan)
    {
        Area b = plan.Bounds;

        float centerX = (b.X + b.Width / 2f) * unitSize;
        float centerZ = (b.Y + b.Height / 2f) * unitSize;

        ceiling = CreateBox("Ceiling",
                            new Vector3(centerX, wallHeight + ceilingThickness / 2f, centerZ),
                            new Vector3(b.Width * unitSize + wallThickness, ceilingThickness,
                                        b.Height * unitSize + wallThickness),
                            CeilingColor);

        ceiling.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        ceiling.SetActive(showCeiling);
    }

    GameObject CreateBox(string name, Vector3 center, Vector3 size, Color color)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.parent = transform;
        box.transform.position = center;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().material.color = color;

        return box;
    }

    // uma cor pra cada tipo
    Color ColorFor(RoomType type)
    {
        switch (type)
        {
            case RoomType.Living:   return new Color(0.78f, 0.42f, 0.20f);   // laranja
            case RoomType.Kitchen:  return new Color(0.30f, 0.60f, 0.32f);   // verde
            case RoomType.Bathroom: return new Color(0.22f, 0.48f, 0.78f);   // azul
            case RoomType.Hall:     return new Color(0.85f, 0.80f, 0.35f);   // amarelo
            default:                return new Color(0.58f, 0.42f, 0.75f);   // quarto, roxo
        }
    }

    void OnGUI()
    {
        if (last == null)
            return;

        List<string> lines = new List<string>();
        lines.Add("semente: " + last.Seed);

        if (last.Success)
        {
            FloorPlan plan = last.Plan;

            lines.Add("comodos: " + plan.RoomCount);
            lines.Add("quartos: " + plan.CountOf(RoomType.Bedroom)
                      + "   banheiros: " + plan.CountOf(RoomType.Bathroom));
            lines.Add("portas: " + plan.Doors.Count);
        }
        else
        {
            lines.Add("nenhuma planta valida");
        }

        lines.Add("tentativas: " + last.Attempts);
        lines.Add("tempo: " + last.Milliseconds.ToString("F2") + " ms");
        lines.Add("falhas: " + last.FailureSummary());
        lines.Add("teto: " + (showCeiling ? "ligado" : "desligado") + " (T)");
        lines.Add("v" + HousePlanner.Version);

        GUIStyle style = new GUIStyle();
        style.fontSize = 15;
        style.normal.textColor = Color.white;

        GUI.Box(new Rect(8, 8, 320, lines.Count * 19 + 10), "");

        for (int i = 0; i < lines.Count; i++)
            GUI.Label(new Rect(14, 12 + i * 19, 400, 20), lines[i], style);
    }

    System.Collections.IEnumerator TakeScreenshot()
    {
        yield return new WaitForEndOfFrame();

        string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string file = $"print_v{HousePlanner.Version}_seed{last.Seed}_{stamp}.png";

        ScreenCapture.CaptureScreenshot(file);
        Debug.Log("print salvo: " + file);
    }
}