using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Lixeira que recebe os itens arrastados.
/// Valida se o ID do lixo largado corresponde ao ID que ela aceita.
/// </summary>
public class SlotLixo : MonoBehaviour, IDropHandler
{
    [Header("Configuração")]
    [Tooltip("ID que a lixeira aceita. Tem que ser igual ao ID do script LixoDrag.")]
    public int idEsperado; 

    public void OnDrop(PointerEventData eventData)
    {
        GameObject objetoArrastado = eventData.pointerDrag;
        
        if (objetoArrastado != null)
        {
            LixoDrag scriptLixo = objetoArrastado.GetComponent<LixoDrag>();

            // Se for lixo e os IDs baterem, é sucesso!
            if (scriptLixo != null && scriptLixo.idLixo == idEsperado)
            {
                scriptLixo.transform.SetParent(transform);
                scriptLixo.transform.position = transform.position;
                
                if (MGAgua.instance != null) MGAgua.instance.RegistrarAcerto();
            }
        }
    }
}