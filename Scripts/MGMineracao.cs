using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Gerenciador central do minigame de mineração. 
/// Controla o cronómetro, pontuação, spawn de minérios e aplicação de buffs temporários.
/// </summary>
public class MGMineracao : MonoBehaviour
{
    public static MGMineracao instance;

    [Header("Conexões")]
    public DailyCooldown meuCooldownEspecifico;
    public RectTransform areaDeSpawn;
    public GameObject prefabOuro;

    [Header("Textos UI")]
    public TextMeshProUGUI textoTempo;
    public TextMeshProUGUI textoOuroAtual;

    [Header("Configurações da Partida")]
    public int quantidadeItensNaTela = 15;
    public float tempoDePartida = 60f;

    [Header("Buffs Temporários (1 Partida)")]
    public float tempoExtraDoBuff = 15f; 
    public float multiplicadorDoBuffOuro = 1.5f; 

    [HideInInspector] public int nivelVelocidade = 0;
    [HideInInspector] public int nivelTaxaOuro = 0;

    public List<ItemMineracao> ourosNaTela = new List<ItemMineracao>();
    private int ouroGanhoNestaPartida = 0;
    private float tempoRestante;
    private bool jogoRolando = false;
    private bool buffOuroDestaPartidaAtivo = false;

    private void Awake() 
    { 
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this; 
    }

    private void OnEnable()
    {
        CarregarUpgrades();
        ResetarEIniciarMinigame();
    }

    /// <summary>Lê os níveis de upgrade do jogador para aplicar na partida atual.</summary>
    private void CarregarUpgrades()
    {
        nivelVelocidade = PlayerPrefs.GetInt("UpMin_Velocidade", 0);
        nivelTaxaOuro = PlayerPrefs.GetInt("UpMin_TaxaOuro", 0);
    }

    /// <summary>Converte o nível do upgrade em uma porcentagem matemática (0.45 a 1.0).</summary>
    public float ObterEficiencia(int nivel)
    {
        if (nivel <= 0) return 0.45f;
        if (nivel == 1) return 0.50f;
        if (nivel == 2) return 0.70f;
        return 1.00f; 
    }

    /// <summary>Limpa a tela, gasta a energia e dá início ao cronômetro.</summary>
    public void ResetarEIniciarMinigame()
    {
        foreach (var ouro in ourosNaTela) { if (ouro != null) Destroy(ouro.gameObject); }
        ourosNaTela.Clear();

        ouroGanhoNestaPartida = 0;
        tempoRestante = tempoDePartida;
        
        // 1. Aplica Buff de Tempo
        if (PlayerPrefs.GetInt("BuffTempo_ProximaPartida", 0) == 1)
        {
            tempoRestante += tempoExtraDoBuff;
        }

        // 2. Aplica Buff de Ouro
        buffOuroDestaPartidaAtivo = (PlayerPrefs.GetInt("BuffOuro_ProximaPartida", 0) == 1);
        
        jogoRolando = true;

        if (meuCooldownEspecifico != null) 
            meuCooldownEspecifico.GastarTentativa();

        GanchoMineracao gancho = Object.FindFirstObjectByType<GanchoMineracao>();
        if (gancho != null) gancho.ResetarGancho();

        AtualizarTextos();
        SpawnarItens();
    }

    private void Update()
    {
        if (!jogoRolando) return;

        tempoRestante -= Time.deltaTime;
        AtualizarTextos();

        if (tempoRestante <= 0)
        {
            tempoRestante = 0;
            jogoRolando = false;
            Invoke(nameof(FinalizarJogo), 1.0f);
        }
    }

    /// <summary>Distribui os ouros e pedras aleatoriamente pelo campo sem sobreposição.</summary>
    private void SpawnarItens()
    {
        float limiteMinX = areaDeSpawn.rect.xMin + 50f;
        float limiteMaxX = areaDeSpawn.rect.xMax - 50f;
        float limiteMinY = areaDeSpawn.rect.yMin + 50f;
        float limiteMaxY = areaDeSpawn.rect.yMax - 50f;

        float centroX = (limiteMinX + limiteMaxX) / 2f;
        List<Vector3> posicoesOcupadas = new List<Vector3>();

        int quantidadeExtra = nivelTaxaOuro * 3;
        int totalItens = quantidadeItensNaTela + quantidadeExtra;

        for (int i = 0; i < totalItens; i++)
        {
            Vector3 pos = Vector3.zero;
            bool achou = false;
            int tentativas = 0;

            while (!achou && tentativas < 50)
            {
                if (i == 0) pos = new Vector3(Random.Range(centroX - 250f, centroX - 80f), Random.Range(limiteMaxY - 150f, limiteMaxY), 0);
                else if (i == 1) pos = new Vector3(Random.Range(centroX + 80f, centroX + 250f), Random.Range(limiteMaxY - 150f, limiteMaxY), 0);
                else pos = new Vector3(Random.Range(limiteMinX, limiteMaxX), Random.Range(limiteMinY, limiteMaxY - 200f), 0);

                achou = true;
                foreach (Vector3 pOcupada in posicoesOcupadas)
                {
                    if (Vector3.Distance(pos, pOcupada) < 100f) { achou = false; break; }
                }
                tentativas++;
            }

            if (achou)
            {
                posicoesOcupadas.Add(pos);
                GameObject novoObj = Instantiate(prefabOuro, areaDeSpawn);
                novoObj.transform.localPosition = pos;
                
                ItemMineracao scriptItem = novoObj.GetComponent<ItemMineracao>();
                scriptItem.GerarAleatorio();
                ourosNaTela.Add(scriptItem);
            }
        }
    }

    /// <summary>Calcula e adiciona o valor fisgado à carteira local da partida.</summary>
    public void AdicionarPontos(int valorBaseDoItem)
    {
        if (!jogoRolando) return;

        float valorReduzido = valorBaseDoItem * 0.5f;
        if (buffOuroDestaPartidaAtivo) valorReduzido *= multiplicadorDoBuffOuro;

        ouroGanhoNestaPartida += Mathf.RoundToInt(valorReduzido);
        AtualizarTextos();
    }

    private void AtualizarTextos()
    {
        if (textoOuroAtual) textoOuroAtual.text = $"Ganhos: {ouroGanhoNestaPartida}";
        if (textoTempo) textoTempo.text = $"Tempo: {Mathf.CeilToInt(tempoRestante)}s";
    }

    /// <summary>Encerra a partida, paga o jogador e limpa os buffs usados.</summary>
    private void FinalizarJogo()
    {
        if (ouroGanhoNestaPartida > 0 && EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(ouroGanhoNestaPartida);
        }

        PlayerPrefs.DeleteKey("BuffTempo_ProximaPartida");
        PlayerPrefs.DeleteKey("BuffOuro_ProximaPartida");
        PlayerPrefs.Save();

        if (NavigationManager.instance != null) NavigationManager.instance.WinMinigameMin();
        else gameObject.SetActive(false);

        if (NavigationManager.instance != null && NavigationManager.instance.bttConfig != null) 
            NavigationManager.instance.bttConfig.SetActive(true);
    }

    public void AbortarMinigame()
    {
        jogoRolando = false;
        
        // NOVIDADE: Devolve a tentativa se o jogador sair antes de terminar
        if (meuCooldownEspecifico != null) 
            meuCooldownEspecifico.DevolverTentativa();

        foreach (var ouro in ourosNaTela) { if (ouro != null) Destroy(ouro.gameObject); }
        ourosNaTela.Clear();
        
        if (NavigationManager.instance != null) 
            NavigationManager.instance.WinMinigameMin();
    }
}