using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gere o minigame de replantio de mudas.
/// Valida quando todos os buracos foram preenchidos com plantas.
/// </summary>
public class MGReplantio : MonoBehaviour
{
    public static MGReplantio instance;

    [Header("--- CONEXÕES ---")]
    public DailyCooldown meuCooldownEspecifico;

    private List<BuracoSlot> todosOsBuracos = new List<BuracoSlot>();

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        
        todosOsBuracos.AddRange(GetComponentsInChildren<BuracoSlot>(true));
    }

    private void OnEnable() 
    { 
        ResetarTudo(); 
    }

    /// <summary>Esvazia todos os buracos de terra para uma nova partida.</summary>
    public void ResetarTudo()
    {
        foreach (var buraco in todosOsBuracos) 
        {
            if (buraco != null) buraco.ResetarBuraco();
        }
    }

    /// <summary>Chamado por cada buraco quando recebe uma muda. Checa se o jogo acabou.</summary>
    public void VerificarProgresso()
    {
        int totalPlantado = 0;
        foreach (var buraco in todosOsBuracos) 
        {
            if (buraco.estaOcupado) totalPlantado++;
        }

        if (totalPlantado >= todosOsBuracos.Count)
        {
            StartCoroutine(RotinaFinalizarJogo());
        }
    }

    private IEnumerator RotinaFinalizarJogo()
    {
        // Aguarda meio segundo de forma segura para o jogador ver a última planta a nascer
        yield return new WaitForSeconds(0.5f);

        float taxaSalva = PlayerPrefs.GetFloat("BancoTaxa", 0.05f); 
        taxaSalva += 0.01f; 
        PlayerPrefs.SetFloat("BancoTaxa", taxaSalva);
        PlayerPrefs.Save();

        if (meuCooldownEspecifico != null) meuCooldownEspecifico.GastarTentativa();

        if (NavigationManager.instance) NavigationManager.instance.WinMinigameReplantio();
        if(MessageManager.Instance != null) MessageManager.Instance.MostrarMensagem("Pântano ajudado! Taxa do banco subiu em 1%!");
    }


    public void AbortarMinigame()
    {
        ResetarTudo();
        if (NavigationManager.instance) NavigationManager.instance.WinMinigameReplantio();
    }
}