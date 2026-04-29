using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gere o minigame de limpar o pântano.
/// Valida quando todos os lixos foram colocados nas lixeiras corretas.
/// </summary>
public class MGAgua : MonoBehaviour
{
    public static MGAgua instance;
    
    [Header("--- CONEXÕES ---")]
    public DailyCooldown meuCooldownEspecifico; 

    [HideInInspector] public int totalAcertos = 0;
    private List<LixoDrag> todosOsLixos = new List<LixoDrag>();

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        
        // Busca todos os lixos que estão dentro deste minigame
        todosOsLixos.AddRange(GetComponentsInChildren<LixoDrag>(true));
    }

    private void OnEnable() 
    { 
        ResetarTudo(); 
    }

    /// <summary>Devolve todos os lixos para as suas posições originais e zera a pontuação.</summary>
    public void ResetarTudo()
    {
        totalAcertos = 0;
        foreach (LixoDrag lixo in todosOsLixos) 
        {
            if (lixo) lixo.ResetarPosicao();
        }
    }

    /// <summary>Chamado pelas lixeiras quando o jogador acerta o lixo correto.</summary>
    public void RegistrarAcerto()
    {
        totalAcertos++;
        if (totalAcertos >= todosOsLixos.Count)
        {
            FinalizarJogo();
        }
    }

    private void FinalizarJogo()
    {

        // Integração com o Banco Central (+1% de taxa de rendimento)
        float taxaSalva = PlayerPrefs.GetFloat("BancoTaxa", 0.05f); 
        taxaSalva += 0.01f; 
        PlayerPrefs.SetFloat("BancoTaxa", taxaSalva); 
        PlayerPrefs.Save();
        
        if (meuCooldownEspecifico != null) meuCooldownEspecifico.GastarTentativa();

        if (NavigationManager.instance) NavigationManager.instance.WinMinigameAgua();

        if(MessageManager.Instance != null) MessageManager.Instance.MostrarMensagem("Pântano ajudado! Taxa do banco subiu em 1%!");
    }

    public void AbortarMinigame()
    {
        ResetarTudo();
        // Não aumenta a taxa do banco e volta para o pântano
        if (NavigationManager.instance) NavigationManager.instance.WinMinigameAgua();
    }
}