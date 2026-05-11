using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Gere o sistema de investimentos e saques do jogador, calculando rendimentos baseados em tempo real (ciclos).
/// </summary>
public class BancoManager : MonoBehaviour
{
    public static BancoManager instance;

    [Header("1. UI Principal do Banco")]
    public TextMeshProUGUI textoInvestido;
    public TextMeshProUGUI textoRendimentoPendente;
    public TextMeshProUGUI textoTaxaAtual;

    [Header("2. Janela de Investimento")]
    public GameObject painelInvestimento;
    public Slider sliderValor;
    public TMP_InputField inputValor; 
    
    [Header("3. Janela de Saque")]
    public GameObject painelSaque;
    public Slider sliderSaque;
    public TMP_InputField inputSaque;

    [Header("4. Limites e Balanceamento")]
    public int limiteBanco = 10000;
    [Tooltip("Tempo necessário para o dinheiro render. (1440 = 24 horas, 1 = 1 minuto)")]
    public float minutosParaRendimento = 1440f; 
    public float taxaInicial = 0.05f; 
    public float quedaDaTaxaPorCiclo = 0.01f; 
    public float limiteTaxaMinima = 0.01f; 

    private int dinheiroInvestido;
    private int rendimentoPendente;
    private float taxaAtual;
    private DateTime ultimaVezRendeu;

    private void Awake() 
    { 
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this; 
    }

    private void OnEnable()
    {
        CarregarBancoECalcularRendimentos();
        if (painelInvestimento) painelInvestimento.SetActive(false);
        if (painelSaque) painelSaque.SetActive(false);
    }

    /// <summary>Lê os dados salvos e calcula quantos ciclos de rendimento se passaram enquanto o jogo estava fechado.</summary>
    private void CarregarBancoECalcularRendimentos()
    {
        dinheiroInvestido = PlayerPrefs.GetInt("BancoInvestido", 0);
        rendimentoPendente = PlayerPrefs.GetInt("BancoRendimento", 0);
        taxaAtual = PlayerPrefs.GetFloat("BancoTaxa", taxaInicial); 
        
        string dataSalva = PlayerPrefs.GetString("BancoUltimoRendimento", "");
        bool gerouRendimentoAgora = false;

        if (string.IsNullOrEmpty(dataSalva) || !DateTime.TryParse(dataSalva, out ultimaVezRendeu))
        {
            ultimaVezRendeu = DateTime.Now;
            SalvarBanco();
        }
        else
        {
            TimeSpan tempoPassado = DateTime.Now - ultimaVezRendeu;
            int ciclosPassados = (int)(tempoPassado.TotalMinutes / minutosParaRendimento);

            if (ciclosPassados > 0 && dinheiroInvestido > 0)
            {
                int rendimentoNovo = 0;

                for (int i = 0; i < ciclosPassados; i++)
                {
                    rendimentoNovo += Mathf.RoundToInt(dinheiroInvestido * taxaAtual);
                    taxaAtual -= quedaDaTaxaPorCiclo;
                    if (taxaAtual < limiteTaxaMinima) taxaAtual = limiteTaxaMinima; 
                }

                rendimentoPendente += rendimentoNovo;
                ultimaVezRendeu = ultimaVezRendeu.AddMinutes(ciclosPassados * minutosParaRendimento);
                SalvarBanco();

                gerouRendimentoAgora = true;
                MostrarAvisoGlobal($"O banco rendeu +{rendimentoNovo} moedas enquanto esteve fora!");
            }
        }
        
        // --- O "BOM DIA" DO BANCO ---
        if (!gerouRendimentoAgora)
        {
            if (rendimentoPendente > 0)
                MostrarAvisoGlobal($"Bem-vindo! Você tem {rendimentoPendente} moedas pendentes para sacar.");
            else if (dinheiroInvestido > 0)
                MostrarAvisoGlobal("O seu dinheiro está rendendo. Volte mais tarde para os lucros!");
            else
                MostrarAvisoGlobal("Bem-vindo ao Banco Central! Invista para começar a render.");
        }

        AtualizarUIPrincipal();
    }

    private void AtualizarUIPrincipal()
    {
        if (textoInvestido) textoInvestido.text = $"{dinheiroInvestido}";
        if (textoRendimentoPendente) textoRendimentoPendente.text = $"Rendimentos: {rendimentoPendente}";
        if (textoTaxaAtual) textoTaxaAtual.text = $"Taxa Atual: {(taxaAtual * 100):F0}% ao dia";
    }

    // ==========================================
    // --- LÓGICA DA JANELA DE INVESTIR ---
    // ==========================================

    public void AbrirJanelaInvestir()
    {
        if (EconomyManager.Instance == null || EconomyManager.Instance.currentMoney <= 0) 
        {
            MostrarAvisoGlobal("Sua carteira está vazia!");
            return;
        }

        painelInvestimento.SetActive(true);
        sliderValor.minValue = 1;
        sliderValor.maxValue = EconomyManager.Instance.currentMoney;
        sliderValor.value = 1;
        
        AtualizarInputPeloSlider();
    }

    public void FecharJanelaInvestir() { painelInvestimento.SetActive(false); }

    public void AtualizarInputPeloSlider()
    {
        if (inputValor && inputValor.text != sliderValor.value.ToString("0")) 
            inputValor.text = sliderValor.value.ToString("0");
    }

    public void AtualizarSliderPeloInput()
    {
        if (int.TryParse(inputValor.text, out int valorDigitado))
        {
            valorDigitado = Mathf.Clamp(valorDigitado, 1, (int)sliderValor.maxValue);
            if (sliderValor.value != valorDigitado) sliderValor.value = valorDigitado;
            inputValor.text = valorDigitado.ToString();
        }
    }

    /// <summary>Confirma o investimento aplicando travas matemáticas de segurança.</summary>
    public void ConfirmarInvestimento()
    {
        int valorAInvestir = 0;
        if (!int.TryParse(inputValor.text, out valorAInvestir)) valorAInvestir = (int)sliderValor.value;

        if (EconomyManager.Instance == null || valorAInvestir <= 0) return;

        // REGRA 1: TENTOU INVESTIR MAIS DO QUE TEM
        if (valorAInvestir > EconomyManager.Instance.currentMoney)
        {
            MostrarAvisoGlobal("Você não tem o suficiente para investir tanto!");
            FecharJanelaInvestir();
            return;
        }

        // REGRA 2: LIMITE MÁXIMO DO BANCO (10.000)
        int totalNoBanco = dinheiroInvestido + rendimentoPendente;
        if (totalNoBanco + valorAInvestir > limiteBanco)
        {
            valorAInvestir = limiteBanco - totalNoBanco;
            if (valorAInvestir <= 0)
            {
                MostrarAvisoGlobal("O banco atingiu o limite máximo de 10.000 ouros!");
                FecharJanelaInvestir();
                return;
            }
        }

        if (EconomyManager.Instance.SpendMoney(valorAInvestir))
        {
            dinheiroInvestido += valorAInvestir;
            if (dinheiroInvestido == valorAInvestir) ultimaVezRendeu = DateTime.Now;

            SalvarBanco();
            AtualizarUIPrincipal();
            FecharJanelaInvestir();
            MostrarAvisoGlobal($"Você investiu {valorAInvestir} moedas!");
        }
    }

    // ==========================================
    // --- LÓGICA DA JANELA DE SAQUE ---
    // ==========================================

    public void AbrirJanelaSaque()
    {
        int totalNoBanco = dinheiroInvestido + rendimentoPendente;
        if (totalNoBanco <= 0) 
        {
            MostrarAvisoGlobal("Você não tem dinheiro no banco para sacar!");
            return; 
        }

        painelSaque.SetActive(true);
        sliderSaque.minValue = 1;
        sliderSaque.maxValue = totalNoBanco;
        sliderSaque.value = totalNoBanco; 
        
        AtualizarInputSaquePeloSlider();
    }

    public void FecharJanelaSaque() { painelSaque.SetActive(false); }

    public void AtualizarInputSaquePeloSlider()
    {
        if (inputSaque && inputSaque.text != sliderSaque.value.ToString("0")) 
            inputSaque.text = sliderSaque.value.ToString("0");
    }

    public void AtualizarSliderSaquePeloInput()
    {
        if (int.TryParse(inputSaque.text, out int valorDigitado))
        {
            valorDigitado = Mathf.Clamp(valorDigitado, 1, (int)sliderSaque.maxValue);
            if (sliderSaque.value != valorDigitado) sliderSaque.value = valorDigitado;
            inputSaque.text = valorDigitado.ToString();
        }
    }

    /// <summary>Processa o saque priorizando a retirada dos rendimentos antes do valor investido.</summary>
    public void ConfirmarSaque()
    {
        int valorASacar = (int)sliderSaque.value;
        int totalNoBanco = dinheiroInvestido + rendimentoPendente;

        if (totalNoBanco <= 0 || valorASacar <= 0) return;
        if (valorASacar > totalNoBanco) valorASacar = totalNoBanco;

        int valorOriginalDisplay = valorASacar;

        if (valorASacar <= rendimentoPendente)
        {
            rendimentoPendente -= valorASacar;
        }
        else
        {
            valorASacar -= rendimentoPendente; 
            rendimentoPendente = 0;
            dinheiroInvestido -= valorASacar; 
        }

        if (dinheiroInvestido < 0) dinheiroInvestido = 0;
        if (rendimentoPendente < 0) rendimentoPendente = 0;

        if (EconomyManager.Instance != null) EconomyManager.Instance.AddMoney(valorOriginalDisplay);

        SalvarBanco();
        AtualizarUIPrincipal();
        FecharJanelaSaque();
        MostrarAvisoGlobal($"Você sacou {valorOriginalDisplay} moedas!");
    }

    // ==========================================
    // --- INTEGRAÇÃO E SAVES ---
    // ==========================================

    public void AumentarTaxaMinigame(float aumentoPercentual)
    {
        taxaAtual += aumentoPercentual; 
        SalvarBanco();
        AtualizarUIPrincipal();
    }

    private void SalvarBanco()
    {
        PlayerPrefs.SetInt("BancoInvestido", dinheiroInvestido);
        PlayerPrefs.SetInt("BancoRendimento", rendimentoPendente);
        PlayerPrefs.SetFloat("BancoTaxa", taxaAtual);
        PlayerPrefs.SetString("BancoUltimoRendimento", ultimaVezRendeu.ToString());
        PlayerPrefs.Save();
    }

    // Função unificada que chama apenas o MessageManager global
    private void MostrarAvisoGlobal(string mensagem)
    {
        if (MessageManager.Instance != null)
        {
            MessageManager.Instance.MostrarMensagem(mensagem);
        }
    }
}