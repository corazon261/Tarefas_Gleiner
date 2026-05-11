using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla o ecrã de conquistas e a barra de progresso baseada no total de moedas ganhas na vida.
/// </summary>
public class BibliotecaManager : MonoBehaviour
{
    [Header("Barra de Progresso Visual")]
    public Image barraFill; 
    public TextMeshProUGUI textoProgresso; 

    [Header("Os Livros (Da Esquerda para a Direita)")]
    public Button[] botoesLivros;
    public RectTransform[] marcadoresUI;

    [Header("Metas (A 1ª DEVE ser 0 para o Livro 1)")]
    public float[] metasMoedas; 

    private int historicoMoedas = 0;

    private void OnEnable()
    {
        if (EconomyManager.Instance != null)
        {
            historicoMoedas = EconomyManager.Instance.totalMoedasGanhas;
        }

        AtualizarBarraProgresso();
        DesbloquearLivros();
        PosicionarMarcadoresAutomaticamente();
    }

    /// <summary>Alinha as imagens dos marcadores na barra de acordo com a percentagem da meta.</summary>
    private void PosicionarMarcadoresAutomaticamente()
    {
        if (metasMoedas == null || metasMoedas.Length == 0 || marcadoresUI == null) return;

        float metaFinal = metasMoedas[metasMoedas.Length - 1];
        if (metaFinal <= 0) return; // Proteção contra divisão por zero

        for (int i = 0; i < marcadoresUI.Length; i++)
        {
            if (marcadoresUI[i] != null && i < metasMoedas.Length)
            {
                float percentagem = metasMoedas[i] / metaFinal;

                marcadoresUI[i].anchorMin = new Vector2(percentagem, marcadoresUI[i].anchorMin.y);
                marcadoresUI[i].anchorMax = new Vector2(percentagem, marcadoresUI[i].anchorMax.y);
                marcadoresUI[i].pivot = new Vector2(0.5f, marcadoresUI[i].pivot.y);
                marcadoresUI[i].anchoredPosition = new Vector2(0, marcadoresUI[i].anchoredPosition.y);
            }
        }
    }

    private void AtualizarBarraProgresso()
    {
        if (barraFill == null || metasMoedas == null || metasMoedas.Length == 0) return;

        float metaFinal = metasMoedas[metasMoedas.Length - 1];
        if (metaFinal <= 0) return; 

        float historicoLimitado = Mathf.Clamp(historicoMoedas, 0, metaFinal);
        barraFill.fillAmount = historicoLimitado / metaFinal;

        if (textoProgresso != null)
        {
            textoProgresso.text = $"{historicoLimitado} / {metaFinal}";
        }
    }

    private void DesbloquearLivros()
    {
        for (int i = 0; i < botoesLivros.Length; i++)
        {
            if (botoesLivros[i] == null) continue;

            if (i < metasMoedas.Length && historicoMoedas >= metasMoedas[i])
                botoesLivros[i].interactable = true; 
            else
                botoesLivros[i].interactable = false; 
        }
    }
}