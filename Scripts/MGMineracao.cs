using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Gerenciador central do minigame de mineração.
/// Cronômetro, spawn de itens, pontuação e integração com upgrades da loja.
/// </summary>
public class MGMineracao : MonoBehaviour
{
    public static MGMineracao instance;

    // -------------------------------------------------------
    // REFERÊNCIAS
    // -------------------------------------------------------
    [Header("Conexões Obrigatórias")]
    public DailyCooldown meuCooldownEspecifico;
    public RectTransform areaDeSpawn;
    public GameObject prefabItem;           // Um único prefab com o componente ItemMineracao
    public GanchoMineracao gancho;

    [Header("UI")]
    public TextMeshProUGUI textoTempo;
    public TextMeshProUGUI textoOuroAtual;

    // -------------------------------------------------------
    // CONFIGURAÇÕES BASE
    // -------------------------------------------------------
    [Header("Configurações Base")]
    public int quantidadeItensBase = 15;
    [Tooltip("Tempo base do minigame em segundos (antes de upgrades).")]
    public float tempoBase = 15f;

    [Header("Configuração da Força (Loja)")]
    [Tooltip("Insira o peso de uma pedra PEQUENA. O upgrade de Força desconta esse valor a cada nível comprado.")]
    public float pesoItemPequeno = 100f; // Ajuste para bater com o peso da sua pedra P

    // -------------------------------------------------------
    // ESTADO INTERNO
    // -------------------------------------------------------
    [HideInInspector] public List<ItemMineracao> itensNaTela = new List<ItemMineracao>();

    private float tempoRestante;
    private int ouroGanhoNestaPartida;
    private bool jogoRolando = false;

    // -------------------------------------------------------
    // PROPORÇÃO OURO (controlada pelo upgrade Ouro)
    // Nível 0 → 20% | 1 → 40% | 2 → 60% | 3 → 80%
    // -------------------------------------------------------
    private static readonly float[] ProporçaoOuro = { 0.20f, 0.40f, 0.60f, 0.80f };

    // -------------------------------------------------------
    // ALCANCE MÁXIMO (controlado pelo upgrade Distância)
    // O valor real é lido pelo Gancho via ObterAlcanceMaximo()
    // -------------------------------------------------------
    [Header("Alcance Base e Máximo")]
    public float alcanceBase = 700f;
    public float alcanceNivel1 = 900f;
    public float alcanceNivel2 = 1100f;
    [Tooltip("Nível 3: vai até o limite inferior da área de spawn.")]
    public float alcanceNivel3 = 1400f;

    // -------------------------------------------------------
    // UNITY
    // -------------------------------------------------------
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void OnEnable()
    {
        IniciarMinigame();
    }

    private void OnDisable()
    {
        jogoRolando = false;
    }

    // -------------------------------------------------------
    // INÍCIO PÚBLICO (vincule ao botão que inicia o minigame)
    // -------------------------------------------------------
    public void IniciarMinigame()
    {
        foreach (var item in itensNaTela)
            if (item != null) Destroy(item.gameObject);
        itensNaTela.Clear();

        ouroGanhoNestaPartida = 0;

        // UPGRADE 1: TEMPO (+2s por nível)[cite: 5]
        int nivelTempo = PlayerPrefs.GetInt("UpMin_Tempo", 0);
        tempoRestante = tempoBase + (nivelTempo * 2f);

        jogoRolando = true;

        if (meuCooldownEspecifico != null) meuCooldownEspecifico.GastarTentativa();
        if (gancho != null) gancho.ResetarGancho();

        AtualizarTextos();
        SpawnarItens();
    }

    // -------------------------------------------------------
    // UPDATE
    // -------------------------------------------------------
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

    // -------------------------------------------------------
    // SPAWN DE ITENS
    // -------------------------------------------------------
    private void SpawnarItens()
    {
        if (areaDeSpawn == null || prefabItem == null) return;

        // UPGRADE 4: OURO (Aumenta a % de spawnar ouro)[cite: 5]
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

    // -------------------------------------------------------
    // PONTUAÇÃO & UPGRADES FINAIS
    // -------------------------------------------------------

    /// <summary>Chamado pelo GanchoMineracao ao entregar um item coletado.</summary>
    public void AdicionarPontos(int valorBase)
    {
        if (!jogoRolando || valorBase <= 0) return;

        // UPGRADE 5: VALOR (+1$ em todos os tamanhos a cada nível)[cite: 5]
        int nivelValor = Mathf.Clamp(PlayerPrefs.GetInt("UpMin_Valor", 0), 0, 3);
        int valorAjustado = valorBase + nivelValor;

        ouroGanhoNestaPartida += valorAjustado;
        AtualizarTextos();
    }

    // UPGRADE 3: FORÇA (Reduz o peso do item na hora de subir)[cite: 5]
    public float ObterPesoAjustado(float pesoOriginal)
    {
        int nivelForca = Mathf.Clamp(PlayerPrefs.GetInt("UpMin_Forca", 0), 0, 3);

        // Cada nível "perdoa" o equivalente ao peso de um item pequeno, zerando a lentidão[cite: 5].
        float pesoAjustado = pesoOriginal - (pesoItemPequeno * nivelForca);

        // Garante que não fique negativo (no mínimo a velocidade normal do gancho vazio)[cite: 5].
        return Mathf.Max(0f, pesoAjustado);
    }

    // UPGRADE 2: DISTÂNCIA (Vai mais fundo)[cite: 5]
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

    // -------------------------------------------------------
    // FINALIZAR E UI
    // -------------------------------------------------------
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
        foreach (var item in itensNaTela) if (item != null) Destroy(item.gameObject);
        itensNaTela.Clear();

        if (meuCooldownEspecifico != null) meuCooldownEspecifico.DevolverTentativa();
        if (NavigationManager.instance != null) NavigationManager.instance.WinMinigameMin();
    }

    private void AtualizarTextos()
    {
        if (textoTempo) textoTempo.text = $"{Mathf.CeilToInt(tempoRestante)}s";
        if (textoOuroAtual) textoOuroAtual.text = $"Ouro: {ouroGanhoNestaPartida}";
    }
}