using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Card de upgrade na Loja de Mineração.
/// 
/// TIPOS E REGRAS (conforme design doc):
/// ─────────────────────────────────────────────────────────────
/// Tempo     → +2s por compra | máx 3 compras | 5/10/15 ouros
/// Distância → 3 níveis de alcance | máx 3 compras | 5/10/15 ouros
/// Força     → 3 níveis de peso   | máx 3 compras | 5/10/15 ouros
/// Ouro      → % de ouro no spawn | máx 3 compras | 10/30/60 ouros
/// Valor     → +1$ por tamanho    | máx 3 compras | 10/30/60 ouros
/// ─────────────────────────────────────────────────────────────
/// Após atingir o nível máximo o botão fica bloqueado.
/// </summary>
public class SlotUpgrade : MonoBehaviour
{
    // -------------------------------------------------------
    // ENUMERAÇÕES
    // -------------------------------------------------------
    public enum TipoUpgrade { Tempo, Distancia, Forca, Ouro, Valor }

    // -------------------------------------------------------
    // INSPECTOR
    // -------------------------------------------------------
    [Header("Configuração do Card")]
    public TipoUpgrade meuTipo;
    public string nomeUpgrade;
    [TextArea] public string descricao;

    [Header("UI do Card")]
    public TextMeshProUGUI textoNivel;
    public TextMeshProUGUI textoPreco;
    public Button botaoComprar;
    public Image imagemCard;          // opcional

    // -------------------------------------------------------
    // DADOS INTERNOS (calculados em runtime)
    // -------------------------------------------------------
    [HideInInspector] public int nivelAtual;
    [HideInInspector] public int custoAtual;

    private const int NivelMaximo = 3;

    // Tabelas de custo por tipo
    private static readonly int[] CustoTipoA = { 5, 10, 15 };   // Tempo / Distância / Força
    private static readonly int[] CustoTipoB = { 10, 30, 60 };  // Ouro / Valor

    // Chave PlayerPrefs de cada tipo
    private string ChaveSave => "UpMin_" + meuTipo.ToString();

    // -------------------------------------------------------
    // UNITY
    // -------------------------------------------------------
    private void OnEnable() => AtualizarInformacoes();

    // -------------------------------------------------------
    // ATUALIZAR VISUAL
    // -------------------------------------------------------
    public void AtualizarInformacoes()
    {
        nivelAtual = PlayerPrefs.GetInt(ChaveSave, 0);
        bool noMax = nivelAtual >= NivelMaximo;

        // Custo da PRÓXIMA compra
        int[] tabelaCusto = UsaTabelaA() ? CustoTipoA : CustoTipoB;
        custoAtual = noMax ? 0 : tabelaCusto[nivelAtual];

        // Texto nível
        if (textoNivel)
            textoNivel.text = noMax ? "MAX" : $"Nv {nivelAtual}";

        // Texto preço
        if (textoPreco)
            textoPreco.text = noMax ? "—" : $"{custoAtual}";

        // Botão comprar
        if (botaoComprar)
            botaoComprar.interactable = !noMax;
    }

    private bool UsaTabelaA()
    {
        return meuTipo == TipoUpgrade.Tempo
            || meuTipo == TipoUpgrade.Distancia
            || meuTipo == TipoUpgrade.Forca;
    }

    // -------------------------------------------------------
    // AÇÕES DE UI
    // -------------------------------------------------------
    /// <summary>Exibe os detalhes deste slot no painel da loja. Vincule ao corpo do card.</summary>
    public void ClicouParaVerDetalhes()
    {
        if (LojaMineracaoManager.instance != null)
            LojaMineracaoManager.instance.MostrarDetalhes(this);
    }

    /// <summary>Executa a compra. Vincule ao botão Comprar do card.</summary>
    public void ClicouNoComprar()
    {
        nivelAtual = PlayerPrefs.GetInt(ChaveSave, 0);

        if (nivelAtual >= NivelMaximo)
        {
            Aviso("Este upgrade já está no nível máximo!");
            return;
        }

        if (EconomyManager.Instance == null)
        {
            Debug.LogWarning("[SlotUpgrade] EconomyManager não encontrado.");
            return;
        }

        int[] tabela = UsaTabelaA() ? CustoTipoA : CustoTipoB;
        int custo    = tabela[nivelAtual];

        if (EconomyManager.Instance.SpendMoney(custo))
        {
            nivelAtual++;
            PlayerPrefs.SetInt(ChaveSave, nivelAtual);
            PlayerPrefs.Save();

            AtualizarInformacoes();

            // Atualiza painel de detalhes
            if (LojaMineracaoManager.instance != null)
                LojaMineracaoManager.instance.MostrarDetalhes(this);

            Aviso($"{nomeUpgrade} atualizado para Nv {nivelAtual}!");
        }
        else
        {
            Aviso("Você não tem ouros suficientes!");
        }
    }

    // -------------------------------------------------------
    // HELPER
    // -------------------------------------------------------
    private void Aviso(string msg)
    {
        if (MessageManager.Instance != null)
            MessageManager.Instance.MostrarMensagem(msg);
    }
}