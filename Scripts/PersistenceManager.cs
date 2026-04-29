using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Procura no 'Save' as mobílias que o jogador tem equipadas e aplica as imagens corretas na casa.
/// </summary>
public class PersistenceManager : MonoBehaviour
{
    [System.Serializable]
    public class SlotMobilia
    {
        public string nomeDaCategoria; 
        public CardMobilia.CategoriaMobilia categoria;
        public Image naCasa; 
    }

    [Header("Configurações dos Móveis na Casa")]
    public SlotMobilia[] slots;

    private void Start()
    {
        CarregarTudoNaCasa();
    }

    /// <summary>Lê as preferências guardadas e procura o respetivo Cartão de Mobília para carregar o seu Sprite.</summary>
    public void CarregarTudoNaCasa()
    {
        CardMobilia[] todosOsCards = Object.FindObjectsByType<CardMobilia>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var slot in slots)
        {
            string idSalvo = PlayerPrefs.GetString($"MobEqp_{slot.categoria}", "");

            if (!string.IsNullOrEmpty(idSalvo))
            {
                foreach (var card in todosOsCards)
                {
                    if (card.categoria == slot.categoria && card.idUnico == idSalvo)
                    {
                        if (slot.naCasa != null && card.spriteDoMovel != null)
                        {
                            slot.naCasa.sprite = card.spriteDoMovel;
                            
                            Color c = slot.naCasa.color;
                            c.a = 1f;
                            slot.naCasa.color = c;
                        }
                        break; 
                    }
                }
            }
        }
    }
}