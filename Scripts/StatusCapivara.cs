using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Gere a fome da capivara, a sua atualização passiva com o tempo e o feedback visual (Animações e UI).
/// </summary>
public class StatusCapivara : MonoBehaviour
{
    public static StatusCapivara instance;

    [Header("Configurações de Fome")]
    public int fomeMaxima = 5;
    [Tooltip("Minutos reais necessários para a capivara perder 1 ponto de fome.")]
    public float minutosParaPerder1Ponto = 30f; 

    [Header("UI e Feedback")]
    public TextMeshProUGUI textoFome; 
    public Image[] iconesFome; 
    public GameObject balaoFome; 

    [Header("Animações (Final)")]
    public Animator animatorCapivara; 

    [Header("Fallback Teste (Sem Animação)")]
    public Image imagemCapivaraNoCenario; 
    public Sprite spriteAlegreEstatico;   
    public Sprite spriteTristeEstatico;   

    private int fomeAtual;
    private DateTime ultimaAtualizacao;

    private void Awake() 
    { 
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this; 
    }

    private void Start()
    {
        // Substituímos os Invokes inseguros por Corrotinas robustas
        StartCoroutine(RotinaDecaimentoDeFome());
        StartCoroutine(RotinaGerenciarBalao());
    }

    private void OnEnable()
    {
        CarregarStatus();
    }

    /// <summary>Lê o save do jogador e calcula quanta fome foi perdida enquanto o jogo esteve fechado.</summary>
    private void CarregarStatus()
    {
        fomeAtual = PlayerPrefs.GetInt("NivelDeFome", fomeMaxima);
        string tempoSalvo = PlayerPrefs.GetString("UltimaVezComida", "");

        if (!string.IsNullOrEmpty(tempoSalvo) && DateTime.TryParse(tempoSalvo, out ultimaAtualizacao))
        {
            TimeSpan tempoPassado = DateTime.Now - ultimaAtualizacao;
            int pontosPerdidos = (int)(tempoPassado.TotalMinutes / minutosParaPerder1Ponto);
            
            if (pontosPerdidos > 0)
            {
                fomeAtual -= pontosPerdidos;
                if (fomeAtual < 0) fomeAtual = 0;
                
                ultimaAtualizacao = ultimaAtualizacao.AddMinutes(pontosPerdidos * minutosParaPerder1Ponto);
                SalvarStatus(); 
            }
        }
        else
        {
            ultimaAtualizacao = DateTime.Now;
            SalvarStatus(); 
        }

        AtualizarVisual();
    }

    /// <summary>Corrotina que verifica a cada 60 segundos se está na hora de descontar 1 ponto de fome.</summary>
    private IEnumerator RotinaDecaimentoDeFome()
    {
        WaitForSeconds espera = new WaitForSeconds(60f); // Otimização de memória (Cache)

        while (true)
        {
            yield return espera;
            
            TimeSpan tempoPassado = DateTime.Now - ultimaAtualizacao;
            if (tempoPassado.TotalMinutes >= minutosParaPerder1Ponto && fomeAtual > 0)
            {
                fomeAtual--;
                ultimaAtualizacao = DateTime.Now; 
                SalvarStatus();
                AtualizarVisual();
            }
        }
    }

    /// <summary>Alimenta a capivara, respeitando o limite máximo do estômago.</summary>
    public void Comer(int quantidade)
    {
        fomeAtual += quantidade;
        if (fomeAtual > fomeMaxima) fomeAtual = fomeMaxima;
        
        ultimaAtualizacao = DateTime.Now;
        SalvarStatus();
        AtualizarVisual();
    }

    private void SalvarStatus()
    {
        PlayerPrefs.SetInt("NivelDeFome", fomeAtual);
        PlayerPrefs.SetString("UltimaVezComida", ultimaAtualizacao.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>Atualiza os textos, ícones e a expressão facial da capivara.</summary>
    private void AtualizarVisual()
    {
        if (textoFome) textoFome.text = $"Fome: {fomeAtual}/{fomeMaxima}";

        bool deveFicarTriste = (fomeAtual <= 2);

        // Lógica Híbrida Blindada: Usa o Animator se existir, senão cai para o Sprite
        if (animatorCapivara != null && animatorCapivara.runtimeAnimatorController != null)
        {
            animatorCapivara.SetBool("estaTriste", deveFicarTriste);
        }
        else if (imagemCapivaraNoCenario != null && spriteAlegreEstatico != null && spriteTristeEstatico != null)
        {
            imagemCapivaraNoCenario.sprite = deveFicarTriste ? spriteTristeEstatico : spriteAlegreEstatico;
        }

        // Atualiza a barra de ícones de forma dinâmica
        for (int i = 0; i < iconesFome.Length; i++)
        {
            if (iconesFome[i] != null) iconesFome[i].enabled = (i < fomeAtual);
        }
    }

    /// <summary>Controla o aparecimento do balão de aviso quando a fome está crítica.</summary>
    private IEnumerator RotinaGerenciarBalao()
    {
        WaitForSeconds esperaCiclo = new WaitForSeconds(30f);
        WaitForSeconds esperaBalao = new WaitForSeconds(5f);

        while (true)
        {
            yield return esperaCiclo;

            if (balaoFome != null && fomeAtual <= 1)
            {
                balaoFome.SetActive(true);
                yield return esperaBalao;
                balaoFome.SetActive(false);
            }
        }
    }
}