using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente que define se o objeto pescado é valioso (Ouro) ou inútil (Pedra).
/// </summary>
public class ItemMineracao : MonoBehaviour
{
    public enum TipoItem { Ouro, PedraRuim }
    
    [Header("Configurações Visuais")]
    public TipoItem meuTipo;
    public Sprite[] spritesOuro;
    public Sprite[] spritesPedra;

    [Header("Status (Atribuído Automaticamente)")]
    public int valorDesteItem;
    public float pesoDesteItem; 
    public bool foiPego = false;

    private Image minhaImagem;

    private void Awake()
    {
        minhaImagem = GetComponent<Image>();
    }

    /// <summary>Gera o tamanho, o sprite e a riqueza do item baseado na sorte do jogador.</summary>
    public void GerarAleatorio()
    {
        foiPego = false;
        if (minhaImagem == null) return;

        int nivelTaxa = PlayerPrefs.GetInt("UpMin_TaxaOuro", 0);
        int nivelValor = PlayerPrefs.GetInt("UpMin_ValorOuro", 0);

        float eficienciaTaxa = MGMineracao.instance != null ? MGMineracao.instance.ObterEficiencia(nivelTaxa) : 0.45f;
        float eficienciaValor = MGMineracao.instance != null ? MGMineracao.instance.ObterEficiencia(nivelValor) : 0.45f;

        meuTipo = Random.value <= eficienciaTaxa ? TipoItem.Ouro : TipoItem.PedraRuim;

        float escala = Random.Range(0.5f, 1.5f);
        transform.localScale = new Vector3(escala, escala, 1);

        if (meuTipo == TipoItem.Ouro)
        {
            if (spritesOuro.Length > 0) minhaImagem.sprite = spritesOuro[Random.Range(0, spritesOuro.Length)];
            
            int valorMaximoPossivel = Random.Range(80, 150); 
            valorDesteItem = Mathf.RoundToInt(valorMaximoPossivel * escala * eficienciaValor);
            pesoDesteItem = 80f * escala; 
        }
        else
        {
            if (spritesPedra.Length > 0) minhaImagem.sprite = spritesPedra[Random.Range(0, spritesPedra.Length)];
            
            valorDesteItem = Mathf.RoundToInt(Random.Range(1, 5) * escala);
            pesoDesteItem = 180f * escala; 
        }
    }
}