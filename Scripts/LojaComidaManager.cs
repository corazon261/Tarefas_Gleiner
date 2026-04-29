using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Gere a UI da loja de comidas, alternando entre categorias e exibindo os detalhes do item selecionado.
/// </summary>
public class LojaComidaManager : MonoBehaviour
{
    public static LojaComidaManager instance;

    [Header("Paineis de Categoria")]
    public GameObject[] paineisDeCards; 

    [Header("Painel de Detalhes (Telão)")]
    public Image imagemPreview; 
    public TextMeshProUGUI textoNomePreview;
    public TextMeshProUGUI textoDetalhesPreview; 
    
    [Header("Feedback Visual")]
    public TextMeshProUGUI textoAvisoNaTela; 
    private Coroutine avisoAtual; 

    [HideInInspector] public CardComida cardSelecionadoAtual; 

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void Start()
    {
        MostrarPainel(0);
        if (textoAvisoNaTela != null) textoAvisoNaTela.gameObject.SetActive(false);
    }

    /// <summary>Alterna as abas visíveis da loja (Frutas, Doces, etc).</summary>
    public void MostrarPainel(int indexDesejado)
    {
        for (int i = 0; i < paineisDeCards.Length; i++)
        {
            if (paineisDeCards[i] != null)
            {
                paineisDeCards[i].SetActive(i == indexDesejado);
            }
        }
    }

    /// <summary>Exibe a foto e os dados da comida no telão principal.</summary>
    public void MostrarDetalhes(CardComida cardClicado)
    {
        cardSelecionadoAtual = cardClicado;

        if (imagemPreview != null && cardClicado.fotoDaComida != null)
        {
            imagemPreview.sprite = cardClicado.fotoDaComida;
            imagemPreview.gameObject.SetActive(true); 
        }
        
        if (textoNomePreview != null) textoNomePreview.text = cardClicado.nomeComida;
        if (textoDetalhesPreview != null) textoDetalhesPreview.text = $"Recupera {cardClicado.energiaQueRecupera} de Fome";
    }

    /// <summary>Exibe um alerta na tela cancelando o anterior para evitar sobreposição visual.</summary>
    public void MostrarAviso(string mensagem)
    {
        if (textoAvisoNaTela == null) return;
        if (avisoAtual != null) StopCoroutine(avisoAtual);
        avisoAtual = StartCoroutine(RotinaMostrarAviso(mensagem));
    }

    private IEnumerator RotinaMostrarAviso(string msg)
    {
        textoAvisoNaTela.text = msg;
        textoAvisoNaTela.gameObject.SetActive(true); 
        yield return new WaitForSeconds(1.5f); 
        textoAvisoNaTela.gameObject.SetActive(false); 
    }
}