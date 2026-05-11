using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Define um item do minigame de mineração (Ouro ou Pedra) com 3 tamanhos possíveis.
/// Tamanho: 0 = Pequeno | 1 = Médio | 2 = Grande
/// </summary>
public class ItemMineracao : MonoBehaviour
{
    public enum TipoItem { Ouro, Pedra }
    public enum Tamanho { Pequeno, Medio, Grande }

    [Header("Visual")]
    public Sprite[] spritesOuro;   // 0=pequeno, 1=médio, 2=grande
    public Sprite[] spritesPedra;  // 0=pequeno, 1=médio, 2=grande

    [Header("Status (lido pelo Gancho e MGMineracao)")]
    public TipoItem meuTipo;
    public Tamanho meuTamanho;
    public int valorDesteItem;      // ouro que dá ao coletar (0 para pedra)
    public float pesoDesteItem;     // quanto reduz a velocidade de subida
    public bool foiPego = false;

    // -------------------------------------------------------
    // Velocidades de subida base (sem upgrades de Força)
    // O gancho vazio sobe a velocidadeSubirMax definida no Gancho.
    // Pedra pequena  → mais lenta que o gancho vazio
    // Pedra média    → mais lenta ainda
    // Pedra grande   → a mais lenta de todas
    // O upgrade de Força vai reduzindo os pesos progressivamente.
    // -------------------------------------------------------
    private static readonly float[] PesoPedra = { 220f, 340f, 500f };   // P / M / G
    private static readonly float[] PesoOuro  = { 140f, 240f, 380f };   // P / M / G

    private static readonly Vector2[] EscalaTamanho =
    {
        new Vector2(0.6f, 0.6f),  // Pequeno
        new Vector2(1.0f, 1.0f),  // Médio
        new Vector2(1.5f, 1.5f)   // Grande
    };

    private Image minhaImagem;

    private void Awake()
    {
        minhaImagem = GetComponent<Image>();
    }

    /// <summary>
    /// Configura o item com tipo e tamanho já definidos pelo spawner.
    /// </summary>
    public void Inicializar(TipoItem tipo, Tamanho tamanho)
    {
        foiPego   = false;
        meuTipo   = tipo;
        meuTamanho = tamanho;

        int idx = (int)tamanho;

        // Escala visual
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null) rt.localScale = EscalaTamanho[idx];

        // Sprite
        if (minhaImagem == null) minhaImagem = GetComponent<Image>();
        if (meuTipo == TipoItem.Ouro)
        {
            if (spritesOuro != null && spritesOuro.Length > idx && spritesOuro[idx] != null)
                minhaImagem.sprite = spritesOuro[idx];

            // Valor base + upgrade Valor
            int valorBase = idx + 1;          // Pequeno=1, Médio=2, Grande=3
            int nivelValor = PlayerPrefs.GetInt("UpMin_Valor", 0);
            valorDesteItem = valorBase + nivelValor;  // cada nível +1 em todos

            pesoDesteItem = PesoOuro[idx];
        }
        else
        {
            if (spritesPedra != null && spritesPedra.Length > idx && spritesPedra[idx] != null)
                minhaImagem.sprite = spritesPedra[idx];

            valorDesteItem = 0;               // pedra não dá ouro
            pesoDesteItem  = PesoPedra[idx];
        }

        // Aplica upgrade de Força (reduz peso)
        AplicarUpgradeForça();
    }

    /// <summary>
    /// Recalcula o peso com base no nível de Força comprado na loja.
    /// Nível 0 → pesos originais
    /// Nível 1 → pedra pequena fica com peso igual ao gancho vazio (0)
    /// Nível 2 → média fica como pequena, grande fica como média
    /// Nível 3 → média fica como gancho vazio, grande fica como pequena
    /// </summary>
    private void AplicarUpgradeForça()
    {
        int nivelForca = PlayerPrefs.GetInt("UpMin_Forca", 0);
        if (nivelForca == 0) return;

        if (meuTipo == TipoItem.Pedra)
        {
            switch (nivelForca)
            {
                case 1:
                    if (meuTamanho == Tamanho.Pequeno) pesoDesteItem = 0f;
                    break;
                case 2:
                    if (meuTamanho == Tamanho.Pequeno) pesoDesteItem = 0f;
                    if (meuTamanho == Tamanho.Medio)   pesoDesteItem = PesoPedra[0];
                    if (meuTamanho == Tamanho.Grande)   pesoDesteItem = PesoPedra[1];
                    break;
                case 3:
                    if (meuTamanho == Tamanho.Pequeno) pesoDesteItem = 0f;
                    if (meuTamanho == Tamanho.Medio)   pesoDesteItem = 0f;
                    if (meuTamanho == Tamanho.Grande)   pesoDesteItem = PesoPedra[0];
                    break;
            }
        }
        else // Ouro também fica mais fácil
        {
            switch (nivelForca)
            {
                case 1:
                    if (meuTamanho == Tamanho.Pequeno) pesoDesteItem = 0f;
                    break;
                case 2:
                    if (meuTamanho == Tamanho.Pequeno) pesoDesteItem = 0f;
                    if (meuTamanho == Tamanho.Medio)   pesoDesteItem = PesoOuro[0];
                    if (meuTamanho == Tamanho.Grande)   pesoDesteItem = PesoOuro[1];
                    break;
                case 3:
                    if (meuTamanho == Tamanho.Pequeno) pesoDesteItem = 0f;
                    if (meuTamanho == Tamanho.Medio)   pesoDesteItem = 0f;
                    if (meuTamanho == Tamanho.Grande)   pesoDesteItem = PesoOuro[0];
                    break;
            }
        }
    }
}