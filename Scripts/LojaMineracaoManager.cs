using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Gerencia a interface da Loja de Mineração.
/// Controla o painel de detalhes do upgrade selecionado e o feedback visual de compra.
/// 
/// SETUP NO INSPECTOR:
///   - Arraste o painel de detalhes (painelDetalhes)
///   - Arraste os textos de nome e descrição dentro desse painel
///   - Arraste o texto de aviso (aparece e some após 2s)
///   - Cada SlotUpgrade chama MostrarDetalhes(this) ao ser clicado
/// </summary>
public class LojaMineracaoManager : MonoBehaviour
{
    public static LojaMineracaoManager instance;

    // -------------------------------------------------------
    // INSPECTOR
    // -------------------------------------------------------
    [Header("Painel de Detalhes do Upgrade")]
    public GameObject painelDetalhes;
    public TextMeshProUGUI textoNomeDetalhe;
    public TextMeshProUGUI textoDescricaoDetalhe;
    public TextMeshProUGUI textoNivelDetalhe;     // ex: "Nível 2 / 3"
    public TextMeshProUGUI textoProximoCusto;      // ex: "Próximo: 15 ouros"

    [Header("Feedback Visual")]
    public TextMeshProUGUI textoAviso;

    [Header("Moedas do Jogador (opcional — referência ao CurrencyUI)")]
    public TextMeshProUGUI textoMoedasNaLoja;     // exibe saldo atual dentro da loja

    // -------------------------------------------------------
    // PRIVADOS
    // -------------------------------------------------------
    private Coroutine avisoCoroutine;
    private SlotUpgrade slotAtual;

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
        FecharDetalhes();

        if (textoAviso != null) textoAviso.gameObject.SetActive(false);

        // Inscreve no evento de moedas para atualizar o saldo em tempo real
        if (EconomyManager.Instance != null && textoMoedasNaLoja != null)
        {
            EconomyManager.Instance.OnMoneyChanged += AtualizarMoedasNaLoja;
            AtualizarMoedasNaLoja(EconomyManager.Instance.currentMoney);
        }

        // Atualiza todos os cards visíveis
        AtualizarTodosOsSlots();
    }

    private void OnDisable()
    {
        if (EconomyManager.Instance != null && textoMoedasNaLoja != null)
            EconomyManager.Instance.OnMoneyChanged -= AtualizarMoedasNaLoja;
    }

    // -------------------------------------------------------
    // PAINEL DE DETALHES
    // -------------------------------------------------------
    /// <summary>
    /// Abre o painel de detalhes mostrando as informações do upgrade clicado.
    /// Chamado por SlotUpgrade.ClicouParaVerDetalhes().
    /// </summary>
    public void MostrarDetalhes(SlotUpgrade slot)
    {
        if (slot == null) return;
        slotAtual = slot;

        if (painelDetalhes != null) painelDetalhes.SetActive(true);

        if (textoNomeDetalhe != null)
            textoNomeDetalhe.text = slot.nomeUpgrade;

        if (textoDescricaoDetalhe != null)
            textoDescricaoDetalhe.text = slot.descricao;

        if (textoNivelDetalhe != null)
        {
            bool noMax = slot.nivelAtual >= 3;
            textoNivelDetalhe.text = noMax
                ? "Nível MÁXIMO"
                : $"Nível {slot.nivelAtual} / 3";
        }

        if (textoProximoCusto != null)
        {
            bool noMax = slot.nivelAtual >= 3;
            textoProximoCusto.text = noMax
                ? "Upgrade máximo comprado"
                : $"Próxima compra: {slot.custoAtual} ouros";
        }
    }

    /// <summary>Fecha o painel de detalhes.</summary>
    public void FecharDetalhes()
    {
        if (painelDetalhes != null) painelDetalhes.SetActive(false);
        slotAtual = null;
    }

    // -------------------------------------------------------
    // BOTÃO COMPRAR DO PAINEL (opcional)
    // -------------------------------------------------------
    /// <summary>
    /// Permite ter um botão "Comprar" centralizado no painel de detalhes
    /// em vez de um botão por card. Vincule ao onClick no Inspector.
    /// </summary>
    public void ClicouComprarNoPainel()
    {
        if (slotAtual != null)
            slotAtual.ClicouNoComprar();
    }

    // -------------------------------------------------------
    // FEEDBACK
    // -------------------------------------------------------
    /// <summary>Exibe um aviso rápido na tela (substitui o anterior se houver).</summary>
    public void MostrarAviso(string mensagem)
    {
        if (textoAviso == null) return;
        if (avisoCoroutine != null) StopCoroutine(avisoCoroutine);
        avisoCoroutine = StartCoroutine(RotinaAviso(mensagem));
    }

    private IEnumerator RotinaAviso(string msg)
    {
        textoAviso.text = msg;
        textoAviso.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        textoAviso.gameObject.SetActive(false);
        avisoCoroutine = null;
    }

    // -------------------------------------------------------
    // HELPERS
    // -------------------------------------------------------
    private void AtualizarMoedasNaLoja(int valor)
    {
        if (textoMoedasNaLoja != null)
            textoMoedasNaLoja.text = $"{valor}";
    }

    /// <summary>Força todos os SlotUpgrade da cena a atualizar seus visuais.</summary>
    public void AtualizarTodosOsSlots()
    {
        SlotUpgrade[] slots = GetComponentsInChildren<SlotUpgrade>(true);
        foreach (var s in slots)
            if (s != null) s.AtualizarInformacoes();
    }
}