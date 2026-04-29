using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Controla a interface da loja de upgrades da mineração, exibindo títulos e descrições dos buffs.
/// </summary>
public class LojaMineracaoManager : MonoBehaviour
{
    public static LojaMineracaoManager instance;

    [Header("Painel Branco (Visualização)")]
    public GameObject painelDetalhes; 
    public TextMeshProUGUI textoNomeDetalhe;
    public TextMeshProUGUI textoDescricaoDetalhe;
    
    [Header("Feedback Visual")]
    public TextMeshProUGUI textoAviso; 

    private Coroutine avisoCorrotinaAtual;

    private void Awake() 
    { 
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this; 
    }

    private void OnEnable()
    {
        FecharDetalhes(); 
        if (textoAviso != null) textoAviso.gameObject.SetActive(false);
    }

    /// <summary>Exibe as estatísticas do upgrade selecionado no painel lateral.</summary>
    public void MostrarDetalhes(SlotUpgrade slot)
    {
        if (painelDetalhes) painelDetalhes.SetActive(true);
        if (textoNomeDetalhe) textoNomeDetalhe.text = slot.nomeUpgrade;
        if (textoDescricaoDetalhe) textoDescricaoDetalhe.text = slot.descricao;
    }

    /// <summary>Oculta o painel de estatísticas.</summary>
    public void FecharDetalhes()
    {
        if (painelDetalhes) painelDetalhes.SetActive(false); 
    }

    /// <summary>Exibe um aviso rápido ao comprar um upgrade ou ao faltar dinheiro.</summary>
    public void MostrarAviso(string mensagem)
    {
        if (textoAviso == null) return;
        if (avisoCorrotinaAtual != null) StopCoroutine(avisoCorrotinaAtual);
        avisoCorrotinaAtual = StartCoroutine(RotinaMostrarAviso(mensagem));
    }

    private IEnumerator RotinaMostrarAviso(string msg)
    {
        textoAviso.text = msg;
        textoAviso.gameObject.SetActive(true); 
        yield return new WaitForSeconds(2f); 
        textoAviso.gameObject.SetActive(false); 
    }
}