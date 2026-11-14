using System.Collections.Generic;
using UnityEngine;
using PCG.Generation;

// camada da unity: pede uma planta ao gerador e constroi a casa na cena.
// toda a logica de geracao fica na pasta Generation, que e c# puro.
// teclas: R gera outra casa, P salva um print.
public class HouseBuilder : MonoBehaviour
{
    public int seed = 42;
    public HouseSettings settings = new HouseSettings();

    [Header("Escala e medidas, em metros")]
    public float unitSize = 0.5f;        // quantos metros vale uma celula
    public float wallHeight = 2.7f;
    public float wallThickness = 0.1f;

    private GenerationResult last;       // a ultima geracao, mostrada no painel

    void Start()
    {
        Build();
    }

    void Update()
    {
        // R gera uma casa nova com semente sorteada
        if (Input.GetKeyDown(KeyCode.R))
        {
            seed = Random.Range(0, 999999);
            Build();
        }

        // P salva um print
        if (Input.GetKeyDown(KeyCode.P))
            StartCoroutine(TakeScreenshot());
    }

    void Build()
    {
        // apaga a casa anterior
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

        if (!plan.Entrance.HasValue)
            Debug.LogWarning("sem entrada: nem sala nem corredor tocam a fachada");

        // um comodo em L tem mais de uma parte, e cada parte ganha sua laje
        foreach (Room room in plan.Rooms)
            foreach (Area part in room.Parts)
                CreateFloor(part, room.Type);

        BuildWalls(plan);
    }

    // laje fina cobrindo uma parte de um comodo, com a cor do tipo dele
    void CreateFloor(Area area, RoomType type)
    {
        // a Area guarda o canto e a unity posiciona pelo centro,
        // por isso soma metade da largura e metade da altura
        float centerX = (area.X + area.Width / 2f) * unitSize;
        float centerZ = (area.Y + area.Height / 2f) * unitSize;

        CreateBox(type.ToString(),
                  new Vector3(centerX, 0f, centerZ),
                  new Vector3(area.Width * unitSize, 0.1f, area.Height * unitSize),
                  ColorFor(type));
    }

    // levanta parede em toda linha da grade que separa dois comodos diferentes.
    // fora da casa o mapa devolve Outside, que nao e igual a nenhum comodo,
    // entao a fachada sai da mesma regra das paredes internas.
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

    // um pedaco de parede de 1 celula de comprimento. se ali for porta, fica o vao.
    void CreateWall(FloorPlan plan, WallKey key)
    {
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

        // fundo escuro, pra dar leitura em cima de qualquer cor
        GUI.Box(new Rect(8, 8, 320, lines.Count * 19 + 10), "");

        for (int i = 0; i < lines.Count; i++)
            GUI.Label(new Rect(14, 12 + i * 19, 400, 20), lines[i], style);
    }

    // espera o quadro terminar de desenhar antes de capturar,
    // senao o painel pode ficar de fora do print
    System.Collections.IEnumerator TakeScreenshot()
    {
        yield return new WaitForEndOfFrame();

        string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string file = $"print_v{HousePlanner.Version}_seed{last.Seed}_{stamp}.png";

        ScreenCapture.CaptureScreenshot(file);
        Debug.Log("print salvo: " + file);
    }
}
