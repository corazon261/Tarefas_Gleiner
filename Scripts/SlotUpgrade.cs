using UnityEngine;
using TMPro;

public class SlotUpgrade : MonoBehaviour
{
    // Apenas os 5 slots oficiais (ValorOuro removido, BuffOuro adicionado)
    public enum TipoUpgrade { Velocidade, TaxaOuro, BuffTempo, BuffOuro }
    
    [Header("Configurações")]
    public TipoUpgrade meuTipo;
    public string nomeUpgrade;
    [TextArea] public string descricao;
    
    public int custoBase = 50;
    public float multiplicadorCusto = 1.5f; 
    [HideInInspector] public int nivelMaximo = 3; 

    [Header("UI do Botão")]
    public TextMeshProUGUI textoNivel; 
    public TextMeshProUGUI textoPreco;

    [HideInInspector] public int nivelAtual;
    [HideInInspector] public int custoAtual;

    private bool eUmBuff;
    private string chaveDoBuff; // Ex: "BuffTempo_ProximaPartida" ou "BuffOuro_ProximaPartida"

    void Awake()
    {
        eUmBuff = (meuTipo == TipoUpgrade.BuffTempo || meuTipo == TipoUpgrade.BuffOuro);
        chaveDoBuff = meuTipo.ToString() + "_ProximaPartida";

        // Trava apenas os upgrades permanentes no Nv 3
        if (!eUmBuff)
        {
            nivelMaximo = 3;
        }
    }

    void OnEnable() { AtualizarInformacoes(); }

    public void AtualizarInformacoes()
    {
        nivelAtual = PlayerPrefs.GetInt("UpMin_" + meuTipo.ToString(), 0);

        if (eUmBuff)
        {
            custoAtual = custoBase;
            if (textoNivel) textoNivel.text = "Buff";
        }
        else
        {
            custoAtual = Mathf.RoundToInt(custoBase * Mathf.Pow(multiplicadorCusto, nivelAtual));
            if (textoNivel) textoNivel.text = "Nv: " + nivelAtual;
        }

        if (textoPreco)
        {
            // Se for buff e já comprou para a próxima rodada, mostra "ATIVO"
            if (eUmBuff && PlayerPrefs.GetInt(chaveDoBuff, 0) == 1)
            {
                textoPreco.text = "ATIVO";
            }
            // Se for normal e bateu no nível máximo, mostra "MAX"
            else if (!eUmBuff && nivelAtual >= nivelMaximo)
            {
                textoPreco.text = "MAX";
            }
            else
            {
                textoPreco.text = custoAtual.ToString(); 
            }
        }
    }

    public void ClicouParaVerDetalhes()
    {
        if (LojaMineracaoManager.instance != null) LojaMineracaoManager.instance.MostrarDetalhes(this);
    }

    public void ClicouNoComprar()
    {
        // Proteção: Se o Banco Central não existir, não faz nada
        if (EconomyManager.Instance == null) 
        {
            Debug.LogWarning("Banco Central não encontrado!");
            return;
        }

        bool travadoNoMaximo = (!eUmBuff && nivelAtual >= nivelMaximo);
        bool buffJaComprado = (eUmBuff && PlayerPrefs.GetInt(chaveDoBuff, 0) == 1);

        if (buffJaComprado)
        {
            Debug.Log("Você já comprou este buff! Jogue uma partida para poder comprar novamente.");
            MessageManager.Instance.MostrarMensagem("Buff já comprado! Jogue uma partida para comprar novamente.");
            return;
        }

        if (!travadoNoMaximo)
        {
            bool conseguiuComprar = EconomyManager.Instance.SpendMoney(custoAtual);

            if (conseguiuComprar)
            {
                AplicarEfeitoDoUpgrade();
                
                PlayerPrefs.Save();
                AtualizarInformacoes();

                if (LojaMineracaoManager.instance != null) LojaMineracaoManager.instance.MostrarDetalhes(this); 
                
                if (MessageManager.Instance != null) MessageManager.Instance.MostrarMensagem($"Comprou {nomeUpgrade}!");
            }
            else
            {
                // TRATAMENTO DE ERRO PARA O JOGADOR
                if (MessageManager.Instance != null) MessageManager.Instance.MostrarMensagem("Você não tem moedas suficientes para este upgrade!");
            }
        }
        else
        {
            if (MessageManager.Instance != null) MessageManager.Instance.MostrarMensagem("Este upgrade já está no nível máximo!");
        }
    }

    private void AplicarEfeitoDoUpgrade()
    {
        if (eUmBuff)
        {
            // Deixa salvo que a próxima partida tem esse buff ativo
            PlayerPrefs.SetInt(chaveDoBuff, 1);
            Debug.Log($"✅ {nomeUpgrade} Ativo para a próxima rodada!");
        }
        else
        {
            nivelAtual++;
            PlayerPrefs.SetInt("UpMin_" + meuTipo.ToString(), nivelAtual);
        }
    }
}