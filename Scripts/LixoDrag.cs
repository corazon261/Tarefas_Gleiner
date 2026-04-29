using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Permite que um item de lixo seja arrastado pela tela.
/// Guarda a sua posição original para voltar caso seja largado no local errado.
/// </summary>
[RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
public class LixoDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identidade")]
    [Tooltip("ID que deve bater com o ID da Lixeira (SlotLixo)")]
    public int idLixo;

    private Vector2 posicaoOriginal;
    private Transform paiOriginal; 
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    
    private bool foiInicializado = false;

    private void Awake()
    {
        VerificarInicializacao();
    }

    /// <summary>Garante que as referências são capturadas corretamente no início.</summary>
    private void VerificarInicializacao()
    {
        if (foiInicializado) return;

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (rectTransform != null) posicaoOriginal = rectTransform.anchoredPosition;
        paiOriginal = transform.parent;

        foiInicializado = true;
    }

    /// <summary>Devolve o lixo ao seu ponto de origem no cenário.</summary>
    public void ResetarPosicao()
    {
        VerificarInicializacao();

        if (paiOriginal != null)
        {
            transform.SetParent(paiOriginal);
            
            if (rectTransform != null) rectTransform.anchoredPosition = posicaoOriginal;
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
            
            transform.localScale = Vector3.one;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        VerificarInicializacao(); 
        
        // Fica "transparente" aos cliques para conseguirmos detetar a lixeira por trás
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        
        transform.SetParent(transform.root); 
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform != null) rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        // Se largou fora de uma lixeira válida, volta à base
        if (transform.parent == transform.root) ResetarPosicao();
    }
}