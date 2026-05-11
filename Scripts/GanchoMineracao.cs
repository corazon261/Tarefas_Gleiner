using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Controla o gancho de mineração (Canvas UI).
///
/// HIERARQUIA ESPERADA:
///   Gancho  ← este script + RectTransform (rotaciona)
///   ├── Cordas          ← pivot (0.5, 1) | anchorMin/Max (0.5, 1) | anchoredPos (0, 0)
///   │                      Posicionado exatamente onde a corda deve COMEÇAR (base do gancho)
///   ├── ObjetoMinerado  ← pivot (0.5, 0.5)
///   └── Garra           ← pivot (0.5, 1) | anchoredPos Y inicial = garraYInicial
///
/// CORDA:
///   O comprimento total da corda é sempre:
///       cordaTotal = |garraRect.anchoredPosition.y - cordaOrigemY|
///   Onde cordaOrigemY é a anchoredPosition.y do cordasContainer (normalmente 0).
///   Assim a corda termina EXATAMENTE no topo da Garra a qualquer momento,
///   independente de estar descendo, subindo com ou sem item.
/// </summary>
public class GanchoMineracao : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Referências da Hierarquia")]
    public RectTransform garraRect;
    public RectTransform cordasContainer;
    public RectTransform objetoMineradoRect;

    [Header("Sprites da Garra")]
    public Image imagemGarra;
    public Sprite spriteAberto;
    public Sprite spriteFechado;

    [Header("Corda")]
    public GameObject prefabSegmentoCorda;
    [Tooltip("Height de cada segmento de corda em pixels.")]
    public float alturaSegmento = 40f;
    [Tooltip("Largura da corda em pixels.")]
    public float larguraCorda = 16f;

    [Header("Pêndulo")]
    public float velocidadeGiro = 2.0f;
    public float anguloMaximo = 70f;

    [Header("Velocidades")]
    public float velocidadeDescer = 550f;
    public float velocidadeSubir = 500f;
    public float velocidadeSubirMinima = 40f;

    [Header("Colisão")]
    public float raioColisao = 55f;

    // ─────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────

    private enum Estado { Girando, Descendo, Subindo }
    private Estado estadoAtual = Estado.Girando;


    private float tempoGiro = 0f;
    private float distanciaDescida = 0f;
    private float garraYInicial = 0f;
    private float cordaOrigemY = 0f;   // anchoredPosition.y do cordasContainer

    private ItemMineracao itemFisgado = null;
    private readonly List<RectTransform> segmentos = new List<RectTransform>();

    // ─────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────

    private void Start()
    {
        if (garraRect != null)
            garraYInicial = garraRect.anchoredPosition.y;

        if (cordasContainer != null)
            cordaOrigemY = cordasContainer.anchoredPosition.y;

        EsconderCorda();
        AplicarSpriteGarra(spriteAberto);
    }

    private void Update()
    {
        switch (estadoAtual)
        {
            case Estado.Girando: TickGirando(); break;
            case Estado.Descendo: TickDescendo(); break;
            case Estado.Subindo: TickSubindo(); break;
        }
    }

    // ─────────────────────────────────────────────
    // ESTADOS
    // ─────────────────────────────────────────────

    private void TickGirando()
    {
        tempoGiro += Time.deltaTime;
        float angulo = Mathf.Sin(tempoGiro * velocidadeGiro) * anguloMaximo;
        transform.localRotation = Quaternion.Euler(0f, 0f, angulo);

        bool clicou = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                   || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);

        // 1. TRAVA DO PAUSE: Adicionado o Time.timeScale > 0 para evitar o clique fantasma
        if (Time.timeScale > 0 && clicou && !EstaSobreUmBotao())
            EntrarDescendo();

        cordasContainer.gameObject.SetActive(false);
    }

    private void TickDescendo()
    {
        float alcance = ObterAlcance();

        distanciaDescida += velocidadeDescer * Time.deltaTime;
        distanciaDescida = Mathf.Min(distanciaDescida, alcance);

        AplicarPosicaoGarra();
        SincronizarObjetoMinerado();
        AtualizarCorda();
        ChecarColisao();

        if (distanciaDescida >= alcance)
            estadoAtual = Estado.Subindo;

        cordasContainer.gameObject.SetActive(true);
    }

    private void TickSubindo()
    {
        float vel = velocidadeSubir;
        if (itemFisgado != null)
        {
           
            float pesoAjustado = itemFisgado.pesoDesteItem;
            if (MGMineracao.instance != null)
                pesoAjustado = MGMineracao.instance.ObterPesoAjustado(pesoAjustado);

            vel = Mathf.Max(vel - pesoAjustado, velocidadeSubirMinima);
        }

        distanciaDescida -= vel * Time.deltaTime;

        if (distanciaDescida <= 0f)
        {
            distanciaDescida = 0f;
            AplicarPosicaoGarra();
            SincronizarObjetoMinerado();

            EsconderCorda();

            EntregarItem();
            EntrarGirando();
            return;
        }

        AplicarPosicaoGarra();
        SincronizarObjetoMinerado();
        AtualizarCorda();
    }

    // ─────────────────────────────────────────────
    // GARRA
    // ─────────────────────────────────────────────

    private void AplicarPosicaoGarra()
    {
        if (garraRect == null) return;
        garraRect.anchoredPosition = new Vector2(0f, garraYInicial - distanciaDescida);
    }

    // ─────────────────────────────────────────────
    // ITEM COLETADO
    // ─────────────────────────────────────────────

    private void SincronizarObjetoMinerado()
    {
        if (objetoMineradoRect == null || garraRect == null) return;
        objetoMineradoRect.anchoredPosition = garraRect.anchoredPosition;
    }

    // ─────────────────────────────────────────────
    // CORDA — comprimento baseado na posição REAL da garra
    // ─────────────────────────────────────────────

    /// <summary>
    /// Calcula o comprimento da corda como a distância entre
    /// o topo do cordasContainer e o topo da Garra (anchoredPosition.y).
    /// Assim a ponta inferior da corda sempre cola no topo da Garra.
    /// </summary>
    private void AtualizarCorda()
    {
        if (cordasContainer == null || prefabSegmentoCorda == null) return;

        // Comprimento real da corda = distância entre origem da corda e topo da garra
        // garraRect.anchoredPosition.y é negativo quando desceu → valor absoluto = comprimento
        float comprimento = 0f;
        if (garraRect != null)
            comprimento = Mathf.Abs(garraRect.anchoredPosition.y - cordaOrigemY);

        if (comprimento <= 0.5f)
        {
            EsconderCorda();
            return;
        }

        if (!cordasContainer.gameObject.activeSelf)
            cordasContainer.gameObject.SetActive(true);

        int segInteiros = Mathf.FloorToInt(comprimento / alturaSegmento);
        float resto = comprimento - segInteiros * alturaSegmento;
        bool temResto = resto > 0.5f;
        int totalNecessario = segInteiros + (temResto ? 1 : 0);

        // Cria segmentos se precisar
        while (segmentos.Count < totalNecessario)
            CriarSegmento();

        // Posiciona e dimensiona
        for (int i = 0; i < segmentos.Count; i++)
        {
            if (segmentos[i] == null) continue;

            if (i >= totalNecessario)
            {
                segmentos[i].gameObject.SetActive(false);
                continue;
            }

            segmentos[i].gameObject.SetActive(true);
            // Pivot (0.5, 1): Y=0 é o topo do container, cresce para baixo
            segmentos[i].anchoredPosition = new Vector2(0f, -i * alturaSegmento);

            // Último segmento tem a altura do "resto" fracionado
            float altura = (i == segInteiros && temResto) ? resto : alturaSegmento;
            segmentos[i].sizeDelta = new Vector2(larguraCorda, altura);
        }
    }

    private void CriarSegmento()
    {
        GameObject go = Instantiate(prefabSegmentoCorda, cordasContainer);
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();

        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(larguraCorda, alturaSegmento);

        segmentos.Add(rt);
    }

    private void EsconderCorda()
    {
        foreach (var s in segmentos)
            if (s != null) s.gameObject.SetActive(false);

        if (cordasContainer != null)
            cordasContainer.gameObject.SetActive(false);
    }

    private void LimparSegmentos()
    {
        foreach (var s in segmentos)
            if (s != null) Destroy(s.gameObject);
        segmentos.Clear();
    }

    // ─────────────────────────────────────────────
    // COLISÃO
    // ─────────────────────────────────────────────

    private void ChecarColisao()
    {
        if (garraRect == null || MGMineracao.instance == null) return;

        Vector2 posGarra = (Vector2)garraRect.position;

        foreach (ItemMineracao item in MGMineracao.instance.itensNaTela)
        {
            if (item == null || item.foiPego) continue;

            if (Vector2.Distance(posGarra, (Vector2)item.transform.position) <= raioColisao)
            {
                FisgarItem(item);
                break;
            }
        }
    }

    private void FisgarItem(ItemMineracao item)
    {
        item.foiPego = true;
        itemFisgado = item;

        if (objetoMineradoRect != null)
        {
            item.transform.SetParent(objetoMineradoRect, false);
            RectTransform rt = item.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }
        }

        SincronizarObjetoMinerado();
        AplicarSpriteGarra(spriteFechado);
        estadoAtual = Estado.Subindo;
    }

    // ─────────────────────────────────────────────
    // ENTREGA DO ITEM
    // ─────────────────────────────────────────────

    private void EntregarItem()
    {
        if (itemFisgado != null)
        {
            if (MGMineracao.instance != null)
                MGMineracao.instance.AdicionarPontos(itemFisgado.valorDesteItem);

            Destroy(itemFisgado.gameObject);
            itemFisgado = null;
        }

        AplicarSpriteGarra(spriteAberto);
    }

    // ─────────────────────────────────────────────
    // TRANSIÇÕES
    // ─────────────────────────────────────────────

    private void EntrarDescendo()
    {
        distanciaDescida = 0f;
        estadoAtual = Estado.Descendo;
    }

    private void EntrarGirando()
    {
        float a = transform.localEulerAngles.z;
        if (a > 180f) a -= 360f;
        tempoGiro = Mathf.Asin(Mathf.Clamp(a / anguloMaximo, -1f, 1f)) / velocidadeGiro;
        estadoAtual = Estado.Girando;
    }

    // ─────────────────────────────────────────────
    // RESET PÚBLICO
    // ─────────────────────────────────────────────

    public void ResetarGancho()
    {
        estadoAtual = Estado.Girando;
        distanciaDescida = 0f;
        tempoGiro = 0f;
        itemFisgado = null;

        transform.localRotation = Quaternion.identity;

        if (garraRect != null)
            garraRect.anchoredPosition = new Vector2(0f, garraYInicial);

        if (objetoMineradoRect != null)
            objetoMineradoRect.anchoredPosition = new Vector2(0f, garraYInicial);

        AplicarSpriteGarra(spriteAberto);
        EsconderCorda();
        LimparSegmentos();
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    private float ObterAlcance() =>
        MGMineracao.instance != null ? MGMineracao.instance.ObterAlcanceMaximo() : 700f;

    private void AplicarSpriteGarra(Sprite sprite)
    {
        if (imagemGarra != null && sprite != null)
            imagemGarra.sprite = sprite;
    }

    private bool EstaSobreUmBotao()
    {
        if (EventSystem.current == null) return false;

        PointerEventData ev = new PointerEventData(EventSystem.current);

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            ev.position = Touchscreen.current.primaryTouch.position.ReadValue();
        else if (Mouse.current != null)
            ev.position = Mouse.current.position.ReadValue();

        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ev, hits);

        foreach (var h in hits)
            if (h.gameObject.GetComponentInParent<Button>() != null) return true;

        return false;
    }
}