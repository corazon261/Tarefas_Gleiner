using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardComida : MonoBehaviour
{
    public enum CategoriaComida { Frutas, Massas, Doces, Bebidas }

    [Header("1. Dados do Alimento")]
    public CategoriaComida categoria; 
    public string idComida; 
    public string nomeComida; 
    public int preco = 10;
    public int energiaQueRecupera = 1;

    [Header("2. UI do Card (Prefab)")]
    public Image imagemNoCard; 
    public Sprite fotoDaComida; 
    public TextMeshProUGUI textoPreco;

    void Start()
    {
        if (imagemNoCard && fotoDaComida) imagemNoCard.sprite = fotoDaComida;
        if (textoPreco) textoPreco.text = preco.ToString();
    }

    /// <summary>
    /// Apenas exibe a comida no painel de detalhes.
    /// Vincule esta função ao clique no CORPO do Card.
    /// </summary>
    public void SelecionarCard()
    {
        if (LojaComidaManager.instance != null)
        {
            LojaComidaManager.instance.MostrarDetalhes(this);
        }
    }

    /// <summary>
    /// Compra e consome a comida imediatamente.
    /// Vincule esta função ao clique no botão COMPRAR.
    /// </summary>
    public void ComprarComida()
    {
        // Garante que o painel atualiza mesmo se clicar direto no comprar
        SelecionarCard();

        int fomeAtual = PlayerPrefs.GetInt("NivelDeFome", 5); 

        if (fomeAtual >= 5)
        {
            if (MessageManager.Instance != null) MessageManager.Instance.MostrarMensagem("A capivara está cheia!");
            return;
        }

        if (EconomyManager.Instance != null && EconomyManager.Instance.SpendMoney(preco))
        {
            if (StatusCapivara.instance != null)
            {
                StatusCapivara.instance.Comer(energiaQueRecupera);
            }
            else
            {
                fomeAtual += energiaQueRecupera;
                if (fomeAtual > 5) fomeAtual = 5; 
                PlayerPrefs.SetInt("NivelDeFome", fomeAtual);
                PlayerPrefs.Save();
            }
            
            if (MessageManager.Instance != null)
            {
                MessageManager.Instance.MostrarMensagem($"Você comprou {nomeComida}!");
            }
        }
        else
        {
            if (MessageManager.Instance != null) MessageManager.Instance.MostrarMensagem("Sem dinheiro suficiente!");
        }
    }
}