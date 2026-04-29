using UnityEngine;

public class Reset : MonoBehaviour
{
    [ContextMenu("RESETAR TODOS OS DADOS (DEBUG)")]
    public void ResetarDados()
    {
        // 1. Apaga tudo do computador/celular
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        // 2. Reseta o dinheiro para 1000
        if (EconomyManager.Instance != null)
        {
            int dinheiroAtual = EconomyManager.Instance.currentMoney;
            EconomyManager.Instance.SpendMoney(dinheiroAtual); 
            EconomyManager.Instance.AddMoney(0); 
        }

        // 3. Atualiza os botões da Loja de Mineração
        SlotUpgrade[] slots = FindObjectsByType<SlotUpgrade>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in slots) 
        {
            if (s != null) s.AtualizarInformacoes();
        }

        // 4. Atualiza os botões da Loja da Casa (Mobílias)
        CardMobilia[] mobilias = FindObjectsByType<CardMobilia>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var m in mobilias)
        {
            if (m != null) m.AtualizarEstado();
        }

        Debug.Log("⚠️ SAVE ZERADO! Você resetou a economia, os upgrades da mineração, as mobílias da casa e os limites diários.");
    }
}