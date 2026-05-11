using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class MGMineracao : MonoBehaviour
{
    public static MGMineracao instance;

    [Header("Conexões Obrigatórias")]
    public DailyCooldown meuCooldownEspecifico;
    public RectTransform areaDeSpawn;
    public GameObject prefabItem;
    public GanchoMineracao gancho;

    [Header("UI")]
    public TextMeshProUGUI textoTempo;
    public TextMeshProUGUI textoOuroAtual;

    [Header("Configurações Base")]
    public int quantidadeItensBase = 15;
    public float tempoBase = 15f;

    [Header("Configuração da Força (Loja)")]
    public float pesoItemPequeno = 100f;

    [HideInInspector] public List<ItemMineracao> itensNaTela = new List<ItemMineracao>();

    private float tempoRestante;
    private int ouroGanhoNestaPartida;
    private bool jogoRolando = false;

    private static readonly float[] ProporçaoOuro = { 0.20f, 0.40f, 0.60f, 0.80f };

    [Header("Alcance Base e Máximo (Unidades)")]
    public float alcanceBase = 600f;
    public float alcanceNivel1 = 800f;
    public float alcanceNivel2 = 1000f;
    [Tooltip("Nível 3: Valor gigante. Só vai parar quando bater na borda da tela!")]
    public float alcanceNivel3 = 5000f;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void OnEnable() => IniciarMinigame();

    // Quando o objeto é desativado, ele limpa a tela automaticamente.
    private void OnDisable()
    {
        jogoRolando = false;
        LimparTela();
    }

    // Função centralizada de faxina (Limpa itens e reseta o gancho)
    private void LimparTela()
    {
        foreach (var item in itensNaTela)
        {
            if (item != null) Destroy(item.gameObject);
        }
        itensNaTela.Clear();

        if (gancho != null) gancho.ResetarGancho();
    }

    public void IniciarMinigame()
    {
        LimparTela();

        ouroGanhoNestaPartida = 0;

        int nivelTempo = PlayerPrefs.GetInt("UpMin_Tempo", 0);
        tempoRestante = tempoBase + (nivelTempo * 2f);

        jogoRolando = true;

        if (meuCooldownEspecifico != null) meuCooldownEspecifico.GastarTentativa();

        AtualizarTextos();
        SpawnarItens();
    }

    private void Update()
    {
        if (!jogoRolando) return;

        tempoRestante -= Time.deltaTime;
        AtualizarTextos();

        if (tempoRestante <= 0f)
        {
            tempoRestante = 0f;
            jogoRolando = false;
            Invoke(nameof(FinalizarMinigame), 1f);
        }
    }

    private void SpawnarItens()
    {
        if (areaDeSpawn == null || prefabItem == null) return;

        int nivelOuro = Mathf.Clamp(PlayerPrefs.GetInt("UpMin_Ouro", 0), 0, 3);
        float propOuro = ProporçaoOuro[nivelOuro];

        int total = quantidadeItensBase + nivelOuro * 2;

        Rect area = areaDeSpawn.rect;
        float minX = area.xMin + 60f;
        float maxX = area.xMax - 60f;
        float minY = area.yMin + 60f;
        float maxY = area.yMax - 60f;

        List<Vector2> posOcupadas = new List<Vector2>();

        for (int i = 0; i < total; i++)
        {
            bool isOuro = (Random.value < propOuro);
            ItemMineracao.TipoItem tipo = isOuro ? ItemMineracao.TipoItem.Ouro : ItemMineracao.TipoItem.Pedra;
            int idxTamanho = Random.Range(0, 3);
            ItemMineracao.Tamanho tamanho = (ItemMineracao.Tamanho)idxTamanho;

            Vector2 pos = EncontrarPosicaoLivre(minX, maxX, minY, maxY, posOcupadas);
            posOcupadas.Add(pos);

            GameObject obj = Instantiate(prefabItem, areaDeSpawn);
            obj.transform.localPosition = new Vector3(pos.x, pos.y, 0f);

            ItemMineracao script = obj.GetComponent<ItemMineracao>();
            if (script != null)
            {
                script.Inicializar(tipo, tamanho);
                itensNaTela.Add(script);
            }
        }
    }

    private Vector2 EncontrarPosicaoLivre(float minX, float maxX, float minY, float maxY, List<Vector2> ocupadas)
    {
        float raioMinimo = 80f;
        int tentativas = 60;

        for (int t = 0; t < tentativas; t++)
        {
            Vector2 candidato = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            bool livre = true;
            foreach (Vector2 p in ocupadas)
            {
                if (Vector2.Distance(candidato, p) < raioMinimo)
                {
                    livre = false;
                    break;
                }
            }
            if (livre) return candidato;
        }
        return new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
    }

    public void AdicionarPontos(int valorBase)
    {
        if (!jogoRolando || valorBase <= 0) return;

        int nivelValor = Mathf.Clamp(PlayerPrefs.GetInt("UpMin_Valor", 0), 0, 3);
        int valorAjustado = valorBase + nivelValor;

        ouroGanhoNestaPartida += valorAjustado;
        AtualizarTextos();
    }

    public float ObterPesoAjustado(float pesoOriginal)
    {
        int nivelForca = Mathf.Clamp(PlayerPrefs.GetInt("UpMin_Forca", 0), 0, 3);
        float pesoAjustado = pesoOriginal - (pesoItemPequeno * nivelForca);
        return Mathf.Max(0f, pesoAjustado);
    }

    public float ObterAlcanceMaximo()
    {
        int nivelDist = Mathf.Clamp(PlayerPrefs.GetInt("UpMin_Distancia", 0), 0, 3);
        switch (nivelDist)
        {
            case 1: return alcanceNivel1;
            case 2: return alcanceNivel2;
            case 3: return alcanceNivel3;
            default: return alcanceBase;
        }
    }

    private void FinalizarMinigame()
    {
        if (ouroGanhoNestaPartida > 0 && EconomyManager.Instance != null)
            EconomyManager.Instance.AddMoney(ouroGanhoNestaPartida);

        if (MessageManager.Instance != null)
            MessageManager.Instance.MostrarMensagem($"Mineração encerrada! Você ganhou {ouroGanhoNestaPartida} ouros.");

        if (NavigationManager.instance != null)
            NavigationManager.instance.WinMinigameMin();
    }

    public void AbortarMinigame()
    {
        jogoRolando = false;
        LimparTela();

        if (meuCooldownEspecifico != null) meuCooldownEspecifico.DevolverTentativa();
        if (NavigationManager.instance != null) NavigationManager.instance.WinMinigameMin();
    }

    private void AtualizarTextos()
    {
        if (textoTempo) textoTempo.text = $"{Mathf.CeilToInt(tempoRestante)}s";
        if (textoOuroAtual) textoOuroAtual.text = $"Ouro: {ouroGanhoNestaPartida}";
    }
}