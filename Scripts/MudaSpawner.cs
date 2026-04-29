using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Fica estático como um botão, mas ao ser arrastado gera um "clone" (fantasma) de si mesmo 
/// para o jogador levar até ao buraco de terra.
/// </summary>
public class MudaSpawner : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Configuração")]
    [Tooltip("Deixe vazio para ele clonar a si próprio.")]
    public GameObject prefabFantasma; 

    private GameObject cloneAtual;
    private RectTransform rectTransformClone;
    private CanvasGroup canvasGroupClone;

    private void Start()
    {
        if (prefabFantasma == null) prefabFantasma = gameObject;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        cloneAtual = Instantiate(prefabFantasma, transform.root);
        
        // Remove o script do clone para evitar bugs de clonagem infinita
        Destroy(cloneAtual.GetComponent<MudaSpawner>());

        rectTransformClone = cloneAtual.GetComponent<RectTransform>();
        canvasGroupClone = cloneAtual.GetComponent<CanvasGroup>();
        
        if (canvasGroupClone == null) canvasGroupClone = cloneAtual.AddComponent<CanvasGroup>();
        
        canvasGroupClone.blocksRaycasts = false; 
        rectTransformClone.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        rectTransformClone.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (cloneAtual != null) rectTransformClone.position = eventData.position;
    }

    private void OnDisable() { LimparClone(); }
    public void OnEndDrag(PointerEventData eventData) { LimparClone(); }

    private void LimparClone()
    {
        if (cloneAtual != null)
        {
            Destroy(cloneAtual);
            cloneAtual = null; 
        }
    }
}