using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;
    
    [Tooltip("Arraste o seu novo Painel de Pause para aqui")]
    public GameObject painelPause;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Chamado pelo botão "Pausa" dentro do minijogo.</summary>
    public void PausarJogo()
    {
        if (painelPause) painelPause.SetActive(true);
        Time.timeScale = 0f; // A Mágica: Congela todo o tempo do jogo!
    }

    /// <summary>Chamado pelo botão "Continuar" do painel de pause.</summary>
    public void ContinuarJogo()
    {
        if (painelPause) painelPause.SetActive(false);
        Time.timeScale = 1f; // Descongela o tempo e o jogo volta a correr
    }

    /// <summary>Chamado pelo botão "Abandonar" do painel de pause.</summary>
    public void AbandonarMinijogo()
    {
        ContinuarJogo(); // Descongela o tempo primeiro para não quebrar os menus
        
        // Pede ao NavigationManager para cancelar o minijogo que estiver aberto
        if (NavigationManager.instance != null)
        {
            NavigationManager.instance.AbortarMinigameAtual();
        }
    }
}