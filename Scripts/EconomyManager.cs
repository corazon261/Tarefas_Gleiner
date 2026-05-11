using UnityEngine;
using System;

/// <summary>
/// Gerencia o dinheiro do jogador e o histórico total de ganhos.
/// Utiliza o padrão Singleton seguro para ser acessado globalmente de qualquer cena.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance;

    [Header("Status da Economia")]
    public int currentMoney;
    public int totalMoedasGanhas;

    [Header("Configurações")]
    [Tooltip("Valor máximo que o jogador pode carregar na carteira.")]
    public int limiteCarteira = 10000;

    /// <summary>Evento disparado sempre que o saldo é alterado (útil para atualizar a UI).</summary>
    public event Action<int> OnMoneyChanged;

    private void Awake()
    {
        // Padrão Singleton Seguro: destrói a cópia se já existir um gerente na cena
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject); 
        
        CarregarEconomia();
    }

    private void Start()
    {
        // Dispara o evento inicial para a UI mostrar o valor correto ao abrir o jogo
        OnMoneyChanged?.Invoke(currentMoney);
    }

    /// <summary>Carrega o dinheiro salvo no dispositivo do jogador.</summary>
    private void CarregarEconomia()
    {
        currentMoney = PlayerPrefs.GetInt("PlayerMoney", 0);
        totalMoedasGanhas = PlayerPrefs.GetInt("Moedas_Historico", 0);
        Debug.Log($"🟢 [ECONOMIA] Banco iniciado com {currentMoney} moedas.");
    }

    /// <summary>
    /// Adiciona moedas à carteira do jogador, respeitando o limite máximo da carteira.
    /// </summary>
    /// <param name="amount">Quantidade de moedas a adicionar.</param>
    public void AddMoney(int amount)
    {
        if (amount <= 0) return; // Trava de segurança contra valores negativos

        currentMoney += amount;
        if (currentMoney > limiteCarteira) currentMoney = limiteCarteira;

        totalMoedasGanhas += amount; // Atualiza o histórico para a Biblioteca

        SalvarEconomia();
        
        Debug.Log($"💰 [ECONOMIA] Ganhou {amount}. Saldo: {currentMoney}");
        OnMoneyChanged?.Invoke(currentMoney);
    }

    /// <summary>
    /// Tenta gastar moedas. Retorna falso se não houver saldo suficiente.
    /// </summary>
    /// <param name="amount">Valor a ser cobrado.</param>
    /// <returns>True se a compra foi aprovada, False se o saldo for insuficiente.</returns>
    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return false;

        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            SalvarEconomia();

            Debug.Log($"✅ [ECONOMIA] Compra de {amount} aprovada! Sobrou: {currentMoney}");
            OnMoneyChanged?.Invoke(currentMoney);
            return true;
        }

        Debug.LogWarning($"❌ [ECONOMIA] Saldo insuficiente! Possui {currentMoney}, custa {amount}");
        return false;
    }

    /// <summary>Salva os dados de economia no disco rígido.</summary>
    private void SalvarEconomia()
    {
        PlayerPrefs.SetInt("PlayerMoney", currentMoney);
        PlayerPrefs.SetInt("Moedas_Historico", totalMoedasGanhas);
        PlayerPrefs.Save();
    }
}