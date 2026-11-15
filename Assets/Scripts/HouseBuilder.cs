using System.Collections.Generic;
using UnityEngine;
using PCG.Generation;

// le a planta gerada e constroi a casa na cena
public class HouseBuilder : MonoBehaviour
{
    public int seed = 42;
    public HouseSettings settings = new HouseSettings();

    [Header("Escala e medidas, em metros")]
    public float unitSize = 0.5f;        // quantos metros vale uma celula
    public float wallHeight = 2.7f;
    public float wallThickness = 0.1f;

    // dados da ultima geracao, mostrados na tela e no console
    private GenerationResult last;

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

    void Build()
    {
        // apaga a casa anterior pra montar uma nova qnd aperta R
        foreach (Transform child in transform)
            Destroy(child.gameObject);

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

        // porta da rua
        if (!plan.Entrance.HasValue)
            Debug.LogWarning("sem entrada: nem sala nem corredor tocam a fachada");

        // comodo pode ter mais de uma parte (formato em L)
        foreach (Room room in plan.Rooms)
            foreach (Area part in room.Parts)
                CreateFloor(part, room.Type);

        BuildWalls(plan);
    }

    // cria uma laje fina no lugar de uma parte do comodo. medidas em celulas.
    void CreateFloor(Area area, RoomType type)
    {
        // a Area guarda o canto, a unity posiciona pelo centro
        float centerX = (area.X + area.Width / 2f) * unitSize;
        float centerZ = (area.Y + area.Height / 2f) * unitSize;

        CreateBox(type.ToString(),
                  new Vector3(centerX, 0f, centerZ),
                  new Vector3(area.Width * unitSize, 0.1f, area.Height * unitSize),
                  ColorFor(type));
    }

    // varre a grade e poe parede onde duas celulas vizinhas
    // pertencem a comodos diferentes, ou onde faz borda com a rua.
    // todas as medidas aqui estao em celulas.
    void BuildWalls(FloorPlan plan)
    {
        CellMap map = new CellMap(plan);
        int width = plan.Bounds.Width;
        int height = plan.Bounds.Height;

        // paredes verticais: ficam entre a celula (x-1, y) e a celula (x, y)
        for (int x = 0; x <= width; x++)
            for (int y = 0; y < height; y++)
                if (map.At(x - 1, y) != map.At(x, y))
                    CreateWall(plan, new WallKey(true, x, y));

        // paredes horizontais: ficam entre a celula (x, y-1) e a celula (x, y)
        for (int x = 0; x < width; x++)
            for (int y = 0; y <= height; y++)
                if (map.At(x, y - 1) != map.At(x, y))
                    CreateWall(plan, new WallKey(false, x, y));
    }

    // cria um pedaco de parede. recebe tudo em celulas e converte
    // pra metros aqui dentro, num lugar so.
    void CreateWall(FloorPlan plan, WallKey key)
    {
        // essa celula de parede virou porta?
        if (plan.Doors.Contains(key))
            return;

        // a chave marca o inicio do pedaco. o centro fica meia celula
        // adiante, no sentido em que a parede corre.
        float centerX = (key.Vertical ? key.X : key.X + 0.5f) * unitSize;
        float centerZ = (key.Vertical ? key.Y + 0.5f : key.Y) * unitSize;

        // 1 celula de comprimento num eixo, espessura fixa em metros no outro
        float sizeX = key.Vertical ? wallThickness : unitSize;
        float sizeZ = key.Vertical ? unitSize : wallThickness;

        CreateBox("Wall",
                  new Vector3(centerX, wallHeight / 2f, centerZ),
                  new Vector3(sizeX, wallHeight, sizeZ),
                  new Color(0.90f, 0.88f, 0.84f));
    }

    // todo objeto da casa e um cubo esticado, filho deste objeto.
    // ser filho e o que permite apagar a casa inteira de uma vez no Build.
    void CreateBox(string name, Vector3 center, Vector3 size, Color color)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.parent = transform;
        box.transform.position = center;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().material.color = color;
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

    // painel de dados no canto superior esquerdo
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
        lines.Add("v" + HousePlanner.Version);

        GUIStyle style = new GUIStyle();
        style.fontSize = 15;
        style.normal.textColor = Color.white;

        // fundo escuro, pra ler em cima de qualquer cor
        GUI.Box(new Rect(8, 8, 320, lines.Count * 19 + 10), "");

        for (int i = 0; i < lines.Count; i++)
            GUI.Label(new Rect(14, 12 + i * 19, 400, 20), lines[i], style);
    }

    // espera o frame terminar de desenhar antes de capturar,
    // senao o overlay pode nao aparecer no print
    System.Collections.IEnumerator TakeScreenshot()
    {
        yield return new WaitForEndOfFrame();

        string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string file = $"print_v{HousePlanner.Version}_seed{last.Seed}_{stamp}.png";

        ScreenCapture.CaptureScreenshot(file);
        Debug.Log("print salvo: " + file);
    }
}
