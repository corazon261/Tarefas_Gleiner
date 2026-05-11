using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Controla o estado de iluminação da casa baseado num temporizador real.
/// Escurece os móveis e muda o cenário quando o tempo expira.
/// </summary>
public class SistemaDeLuz : MonoBehaviour
{
    public static SistemaDeLuz instance;

    [Header("1. O Botão de Luz")]
    public Image imagemBotaoLuz; 
    public TextMeshProUGUI textoBotaoLuz; 
    public int custoDaLuz = 50;
    public float minutosPorPagamento = 30f; 
    
    public Color corBotaoVerde = new Color(0.3f, 0.8f, 0.3f);   
    public Color corBotaoVermelho = new Color(0.8f, 0.3f, 0.3f); 

    [Header("2. Sprites do Fundo (2 Estados)")]
    public Image imagemFundoCasa; 
    public Sprite fundoComLuz;    
    public Sprite fundoSemLuz;    

    [Header("3. Os Móveis")]
    public Image[] spritesMoveis; 
    public Color corEscuraParaMoveis = new Color(0.4f, 0.4f, 0.4f, 1f); 

    private DateTime dataVencimento;
    private bool luzEstavaLigada = true; 

    private void Awake() 
    { 
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this; 
    }

    private void Start()
    {
        CarregarTempo();
        luzEstavaLigada = (dataVencimento > DateTime.Now);
        AplicarVisuais(luzEstavaLigada);
    }

    private void Update()
    {
        ChecarTempo();
    }

    /// <summary>Lê o registo do temporizador. Se for a primeira vez, oferece tempo de cortesia.</summary>
    private void CarregarTempo()
    {
        string tempoSalvo = PlayerPrefs.GetString("VencimentoLuz", "");
        
        if (string.IsNullOrEmpty(tempoSalvo) || !DateTime.TryParse(tempoSalvo, out dataVencimento))
        {
            // Inicia com 5 minutos de luz grátis para testes
            dataVencimento = DateTime.Now.AddMinutes(5f); 
            SalvarTempo();
        }
    }

    private void SalvarTempo()
    {
        PlayerPrefs.SetString("VencimentoLuz", dataVencimento.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>Compara o tempo atual com o vencimento e dita o estado visual.</summary>
    private void ChecarTempo()
    {
        TimeSpan tempoRestante = dataVencimento - DateTime.Now;
        bool temLuzAgora = tempoRestante.TotalSeconds > 0;

        if (temLuzAgora)
        {
            if (imagemBotaoLuz) imagemBotaoLuz.color = corBotaoVerde;
            if (textoBotaoLuz) textoBotaoLuz.text = $"{tempoRestante.Hours:D2}:{tempoRestante.Minutes:D2}:{tempoRestante.Seconds:D2}";
        }
        else
        {
            if (imagemBotaoLuz) imagemBotaoLuz.color = corBotaoVermelho;
            if (textoBotaoLuz) textoBotaoLuz.text = "Sem Luz!";
        }

        // Apenas aplica o processamento visual pesado se o estado mudar de facto
        if (temLuzAgora != luzEstavaLigada)
        {
            AplicarVisuais(temLuzAgora);
            luzEstavaLigada = temLuzAgora;
        }
    }

    /// <summary>Chamado pelo botão da UI para renovar o tempo da luz gastando moedas.</summary>
    public void ClicouPagarLuz()
    {
        // Trava 1: Já está paga
        if (dataVencimento > DateTime.Now)
        {
            if (MessageManager.Instance != null) MessageManager.Instance.MostrarMensagem("A conta de energia já está paga!");
            return;
        }

        if (EconomyManager.Instance != null)
        {
            // O SpendMoney devolve VERDADEIRO se tem dinheiro, e FALSO se está pobre
            if (EconomyManager.Instance.SpendMoney(custoDaLuz))
            {
                if (dataVencimento < DateTime.Now)
                    dataVencimento = DateTime.Now.AddMinutes(minutosPorPagamento);
                else
                    dataVencimento = dataVencimento.AddMinutes(minutosPorPagamento);

                SalvarTempo();
                AplicarVisuais(true);
                
                if (MessageManager.Instance != null) MessageManager.Instance.MostrarMensagem("Luz ligada com sucesso!");
            }
            else
            {
                // AQUI ESTÁ O TRATAMENTO DE ERRO!
                if (MessageManager.Instance != null) MessageManager.Instance.MostrarMensagem("Moedas insuficientes para pagar a luz!");
            }
        }
    }

    /// <summary>Altera o fundo e a cor dos móveis mantendo o canal Alpha intacto.</summary>
    private void AplicarVisuais(bool temLuz)
    {
        if (imagemFundoCasa != null)
        {
            imagemFundoCasa.sprite = temLuz ? fundoComLuz : fundoSemLuz;
        }

        Color corMovel = temLuz ? Color.white : corEscuraParaMoveis;

        foreach (Image movel in spritesMoveis)
        {
            if (movel != null)
            {
                Color corFinal = corMovel;
                corFinal.a = movel.color.a; // Preserva a transparência original do móvel
                movel.color = corFinal;
            }
        }
    }
}