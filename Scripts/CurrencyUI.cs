using UnityEngine;
using TMPro; 

/// <summary>
/// Controla a exibição visual do dinheiro do jogador na interface.
/// Escuta os eventos do EconomyManager de forma passiva e eficiente.
/// </summary>
public class CurrencyUI : MonoBehaviour
{
    [Header("Componentes Visuais")]
    [Tooltip("Texto da interface que mostrará o número de moedas.")]
    public TextMeshProUGUI moneyText;

    private void Start()
    {
        // Inscreve-se no evento do banco para ser avisado automaticamente de mudanças
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnMoneyChanged += UpdateText;
            UpdateText(EconomyManager.Instance.currentMoney); 
        }
    }

    private void OnDestroy()
    {
        // Desinscreve-se do evento para evitar Memory Leaks se a tela for destruída
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnMoneyChanged -= UpdateText;
        }
    }

    /// <summary>Atualiza o texto na tela com a nova quantia recebida pelo evento.</summary>
    private void UpdateText(int newAmount)
    {
        if (moneyText != null)
        {
            moneyText.text = newAmount.ToString();
        }
    }
}