using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using PCG.Generation;

public class HouseBuilder : MonoBehaviour
{
    public int seed = 123456789;
    public bool fixedSeed = false;
    public HouseSettings settings = new HouseSettings();

    [Header("Escala e medidas, em metros")]
    public float unitSize = 0.5f;        // qts metros vale uma celula
    public float wallHeight = 2.5f;
    public float wallThickness = 0.1f;
    public float doorHeight = 2.0f;
    public float ceilingThickness = 0.1f;

    private bool showCeiling = false;      // T Liga e desliga teto

    private static readonly Color WallColor = new Color(0.90f, 0.88f, 0.84f);
    private static readonly Color CeilingColor = new Color(0.96f, 0.96f, 0.94f);

    private GenerationResult last;

    private HashSet<WallKey> openings = new HashSet<WallKey>();

    private GameObject ceiling;

    private bool rebuild;

    void Start()
    {
        if (!fixedSeed)
            seed = Random.Range(HousePlanner.MinSeed, HousePlanner.MaxSeed + 1);

        Build();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            rebuild = true;
    }

    void Update()
    {
        if (rebuild)
        {
            rebuild = false;
            Build();
        }

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

        openings = plan.OpenWalls();

        // comodo pode ter mais de uma parte (formato em L)
        foreach (Room room in plan.Rooms)
            foreach (Area part in room.Parts)
                CreateFloor(part, room.Type);

        BuildWalls(plan);
        BuildCeiling(plan);
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
            BuildLine(true, x, height, y => map.At(x - 1, y) != map.At(x, y));

        for (int y = 0; y <= height; y++)
            BuildLine(false, y, width, x => map.At(x, y - 1) != map.At(x, y));
    }

    void BuildLine(bool vertical, int line, int length, System.Func<int, bool> hasWall)
    {
        int start = 0;
        int state = 0;

        for (int i = 0; i <= length; i++)
        {
            int next = i < length ? WallState(vertical, line, i, hasWall) : 0;

            if (next == state)
                continue;

            if (state != 0)
                CreateWall(vertical, line, start, i - start, state == 2);

            start = i;
            state = next;
        }
    }

    int WallState(bool vertical, int line, int i, System.Func<int, bool> hasWall)
    {
        if (!hasWall(i))
            return 0;

        WallKey key = vertical ? new WallKey(true, line, i) : new WallKey(false, i, line);

        return openings.Contains(key) ? 2 : 1;
    }

    // cria um pedaco de parede. recebe tudo em celulas e converte pra metros aqui dentro, num lugar so
    void CreateWall(bool vertical, int line, int start, int length, bool open)
    {
        float along = (start + length / 2f) * unitSize;
        float across = line * unitSize;

        float centerX = vertical ? across : along;
        float centerZ = vertical ? along : across;

        float size = length * unitSize + wallThickness;
        float sizeX = vertical ? wallThickness : size;
        float sizeZ = vertical ? size : wallThickness;

        if (open)
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

            lines.Add("casa: " + plan.Bounds.Width + "x" + plan.Bounds.Height + " celulas ("
                      + (plan.Bounds.Width * unitSize).ToString("F1") + " x "
                      + (plan.Bounds.Height * unitSize).ToString("F1") + " m)");
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