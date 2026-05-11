using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; 

/// <summary>
/// Buraco de terra que recebe a muda de planta transportada pelo jogador.
/// </summary>
public class BuracoSlot : MonoBehaviour, IDropHandler
{
    [Header("Visual")]
    [Tooltip("O GameObject/Imagem da planta que nascerá aqui.")]
    public GameObject plantaVisual; 

    [HideInInspector] public bool estaOcupado = false;

    /// <summary>Apaga a planta para iniciar uma nova rodada.</summary>
    public void ResetarBuraco()
    {
        estaOcupado = false;
        if (plantaVisual != null) plantaVisual.SetActive(false); 
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (estaOcupado) return;

        GameObject origem = eventData.pointerDrag; 
        
        // Verifica se o objeto largado veio de um Spawner de Mudas válido
        if (origem != null && origem.GetComponent<MudaSpawner>() != null)
        {
            estaOcupado = true;

            if (plantaVisual != null) 
            {
                plantaVisual.SetActive(true);

                Image imgOrigem = origem.GetComponent<Image>();
                Image imgDestino = plantaVisual.GetComponent<Image>();

                // Copia a cor exata da semente arrastada
                if (imgOrigem != null && imgDestino != null) imgDestino.color = imgOrigem.color;
            }

            if (MGReplantio.instance != null) MGReplantio.instance.VerificarProgresso();
        }
    }
}