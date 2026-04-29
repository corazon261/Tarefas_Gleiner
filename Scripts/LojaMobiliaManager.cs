using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LojaMobiliaManager : MonoBehaviour
{
    public static LojaMobiliaManager instance;

    [Header("Painel de Detalhes")]
    public Image imagemPreview; 
    public TextMeshProUGUI textoNomePreview;
    
    public TextMeshProUGUI textoAviso; 
    
    [HideInInspector] public CardMobilia cardSelecionadoAtual; 

    private CardMobilia[] todosOsCards;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        todosOsCards = FindObjectsByType<CardMobilia>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        AtualizarTodosOsCards();
        if (textoAviso != null) textoAviso.gameObject.SetActive(false);

    }

    public void MostrarDetalhes(CardMobilia cardClicado)
    {
        // Salva este card como o "foco" atual da loja
        cardSelecionadoAtual = cardClicado; 

        if (imagemPreview != null)
        {
            imagemPreview.sprite = cardClicado.spriteDoMovel;
            imagemPreview.gameObject.SetActive(true); 
        }
        
        if (textoNomePreview != null)
        {
            textoNomePreview.text = cardClicado.nomeMobilia;
        }
    }

    public void AtualizarTodosOsCards()
    {
        if (todosOsCards == null) return;

        foreach (CardMobilia card in todosOsCards)
        {
            card.AtualizarEstado();
        }
    }

    public void MostrarAviso(string mensagem)
    {
        StartCoroutine(RotinaMostrarAviso(mensagem));
    }

    private System.Collections.IEnumerator RotinaMostrarAviso(string msg)
    {
        //textoAviso.text = msg;
        //textoAviso.gameObject.SetActive(true); // Liga o texto
        
        yield return new WaitForSeconds(2f); // Espera 2 segundos
        
        //textoAviso.gameObject.SetActive(false); // Desliga o texto
    }
}