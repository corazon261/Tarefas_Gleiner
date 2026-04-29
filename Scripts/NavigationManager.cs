using System.Collections.Generic; 
using UnityEngine;

/// <summary>
/// Gere a transição entre todos os painéis e ecrãs (HUDs) do jogo.
/// Garante que apenas um painel principal está ativo de cada vez.
/// </summary>
public class NavigationManager : MonoBehaviour
{
    public static NavigationManager instance;

    [Header("--- GERENCIADORES DE COOLDOWN ---")]
    public DailyCooldown cooldownAgua;    
    public DailyCooldown cooldownPlanta;  
    public DailyCooldown cooldownMineracao;
    public GameObject cooldownManagerUI;  

    [Header("--- GRUPO DO MAPA ---")]
    public GameObject grupoMapa; 
    public GameObject bttConfig; 

    [Header("--- PAINÉIS PRINCIPAIS (HUDS) ---")]
    public GameObject painelCasa;
    public GameObject painelMineracao;
    public GameObject painelLojaDeRoupa;
    public GameObject painelPantano;
    public GameObject painelBiblioteca;
    public GameObject painelBanco;
    public GameObject painelConfiguracao;

    [Header("--- SUB-ECRÃS (Casa) ---")]
    public GameObject casaMobilia;
    public GameObject casaHud;

    [Header("--- SUB-ECRÃS (Mineração) ---")]
    public GameObject minigameMin;

    [Header("--- MINIGAMES ---")]
    public GameObject minigameAgua;
    public GameObject minigameReplantio;


    public GameObject botaoPausaGlobal;

    private List<GameObject> todosOsPaineis;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void Start()
    {
        todosOsPaineis = new List<GameObject>
        {
            grupoMapa, painelCasa, painelMineracao, painelLojaDeRoupa,
            painelPantano, painelBiblioteca, painelBanco, painelConfiguracao,
            casaMobilia 
        };
    }

    /// <summary>Desativa todos os painéis e liga apenas o alvo pretendido.</summary>
    private void IrParaPainel(GameObject painelAlvo)
    {
        FecharTudo();
        if (painelAlvo != null) painelAlvo.SetActive(true);
    }

    public void AbrirCasa() => IrParaPainel(painelCasa);
    public void AbrirMineracao() => IrParaPainel(painelMineracao);
    public void AbrirLoja() => IrParaPainel(painelLojaDeRoupa);
    public void AbrirBiblioteca() => IrParaPainel(painelBiblioteca);
    public void AbrirBanco() => IrParaPainel(painelBanco);
    
    public void AbrirPantano()
    {
        IrParaPainel(painelPantano);
        if (minigameAgua) minigameAgua.SetActive(false);
        if (minigameReplantio) minigameReplantio.SetActive(false);
        if (cooldownManagerUI) cooldownManagerUI.SetActive(true);
    }

    public void AbrirConfig()
    {
        IrParaPainel(painelConfiguracao);
        if (bttConfig) bttConfig.SetActive(false); 
    }

    public void VoltarParaMapa()
    {
        IrParaPainel(grupoMapa);
        if (bttConfig) bttConfig.SetActive(true); 
    }

    public void IrMobilia()
    {
        if (casaHud) casaHud.SetActive(false);
        if (casaMobilia) casaMobilia.SetActive(true);
    }

    public void VoltarCasaHud()
    {
        if (casaMobilia) casaMobilia.SetActive(false);
        if (casaHud) casaHud.SetActive(true);
    }

    // ==========================================
    // --- LÓGICA DE TRANSIÇÃO DOS MINIGAMES ---
    // ==========================================

    public void IrMinigameMin()
    {
        if (cooldownMineracao != null && cooldownMineracao.PodeJogar())
        {
            if (minigameMin) minigameMin.SetActive(true);
            if (cooldownManagerUI) cooldownManagerUI.SetActive(false); 
            if (botaoPausaGlobal) botaoPausaGlobal.SetActive(true);
        }
    }

    public void WinMinigameMin()
    {
        if (minigameMin) minigameMin.SetActive(false);
        if (painelMineracao) painelMineracao.SetActive(true);
        if (botaoPausaGlobal) botaoPausaGlobal.SetActive(false); 
    }

    public void IrMinigameAguaPantano()
    {
        if (cooldownAgua != null && cooldownAgua.PodeJogar())
        {        
            if (minigameAgua) minigameAgua.SetActive(true);
            if (cooldownManagerUI) cooldownManagerUI.SetActive(false);
            if (botaoPausaGlobal) botaoPausaGlobal.SetActive(true); // <--- LIGA O BOTÃO
        }
    }

    public void IrMinigamePlantio()
    {
        if (cooldownPlanta != null && cooldownPlanta.PodeJogar())
        {
            if (minigameReplantio) minigameReplantio.SetActive(true);
            if (cooldownManagerUI) cooldownManagerUI.SetActive(false);
            if (botaoPausaGlobal) botaoPausaGlobal.SetActive(true); // <--- LIGA O BOTÃO
        }
    }

    public void WinMinigameAgua()
    {
        if (minigameAgua) minigameAgua.SetActive(false);
        RestaurarPantano();
        if (botaoPausaGlobal) botaoPausaGlobal.SetActive(false);
    }

    public void WinMinigameReplantio()
    {
        if (minigameReplantio) minigameReplantio.SetActive(false);
        RestaurarPantano();
        if (botaoPausaGlobal) botaoPausaGlobal.SetActive(false);
    }

    private void RestaurarPantano()
    {
        if (painelPantano) painelPantano.SetActive(true);
        if (cooldownManagerUI) cooldownManagerUI.SetActive(true);
    }

    private void FecharTudo()
    {
        foreach (GameObject painel in todosOsPaineis)
        {
            if (painel != null) painel.SetActive(false);
        }
    }

    public void AbortarMinigameAtual()
    {
        // O gerente olha para ver qual minijogo está ativo no momento
        if (minigameMin != null && minigameMin.activeSelf)
        {
            if (MGMineracao.instance != null) MGMineracao.instance.AbortarMinigame();
        }
        else if (minigameAgua != null && minigameAgua.activeSelf)
        {
            if (MGAgua.instance != null) MGAgua.instance.AbortarMinigame();
        }
        else if (minigameReplantio != null && minigameReplantio.activeSelf)
        {
            if (MGReplantio.instance != null) MGReplantio.instance.AbortarMinigame();
        }
    }
}