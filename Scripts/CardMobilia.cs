using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardMobilia : MonoBehaviour
{
    public enum CategoriaMobilia { Sofa, Quadro, GuardaRoupa, Geladeira }

    [Header("1. DADOS DO MÓVEL")]
    public string idUnico; 
    public CategoriaMobilia categoria;
    public string nomeMobilia; 
    public int preco = 100;
    public Sprite spriteDoMovel; 

    [Header("2. LIGAÇÕES DO PREFAB")]
    public Image imagemDoCard;       
    public TextMeshProUGUI textoBotao; 
    public TextMeshProUGUI textoPreco; 

    [Header("3. LIGAÇÃO COM A CASA")]
    public Image localNaCasa; 

    [HideInInspector] public bool comprado;
    [HideInInspector] public bool equipado;

    private void Start()
    {
        if (imagemDoCard && spriteDoMovel) imagemDoCard.sprite = spriteDoMovel;
        AtualizarEstado();
    }

    private void OnEnable() => AtualizarEstado();

    public void AtualizarEstado()
    {
        comprado = (preco <= 0) || (PlayerPrefs.GetInt($"MobComp_{idUnico}", 0) == 1);
        string equipadoAtual = PlayerPrefs.GetString($"MobEqp_{categoria}", "");
        equipado = (equipadoAtual == idUnico);

        if (equipado)
        {
            if (textoBotao) textoBotao.text = "Equipado";
            if (textoPreco) textoPreco.text = "-";
        }
        else if (comprado)
        {
            if (textoBotao) textoBotao.text = "Equipar";
            if (textoPreco) textoPreco.text = "-";
        }
        else
        {
            if (textoBotao) textoBotao.text = "Comprar";
            if (textoPreco) textoPreco.text = preco.ToString();
        }
    }

    /// <summary>
    /// Apenas exibe o móvel no painel de detalhes.
    /// Vincule esta função ao clique no CORPO do Card.
    /// </summary>
    public void SelecionarCard()
    {
        if (LojaMobiliaManager.instance != null)
        {
            LojaMobiliaManager.instance.MostrarDetalhes(this);
        }
    }

    /// <summary>
    /// Compra ou Equipa o móvel imediatamente.
    /// Vincule esta função ao clique no botão AÇÃO (Comprar/Equipar).
    /// </summary>
    public void ExecutarAcao()
    {
        // Força a atualização do painel de preview
        SelecionarCard();

        if (equipado) return; 

        if (!comprado)
        {
            if (EconomyManager.Instance != null && EconomyManager.Instance.SpendMoney(preco))
            {
                comprado = true;
                PlayerPrefs.SetInt($"MobComp_{idUnico}", 1);
                PlayerPrefs.Save();
                EquiparEsteMovel(); 
                if (MessageManager.Instance != null) MessageManager.Instance.MostrarMensagem($"Comprou {nomeMobilia}!");
            }
            else
            {
                if (MessageManager.Instance != null) MessageManager.Instance.MostrarMensagem("Sem dinheiro!");
            }
        }
        else
        {
            EquiparEsteMovel();
            if (MessageManager.Instance != null) MessageManager.Instance.MostrarMensagem($"{nomeMobilia} equipado!");
        }
    }

    private void EquiparEsteMovel()
    {
        PlayerPrefs.SetString($"MobEqp_{categoria}", idUnico);
        PlayerPrefs.Save();
        
        PersistenceManager pm = Object.FindFirstObjectByType<PersistenceManager>();
        if (pm != null) pm.CarregarTudoNaCasa();

        if (LojaMobiliaManager.instance != null) LojaMobiliaManager.instance.AtualizarTodosOsCards();
    }
}