using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GanchoMineracao : MonoBehaviour
{
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
    public float alturaSegmento = 40f;
    public float larguraCorda = 16f;

    [Header("Pêndulo")]
    public float velocidadeGiro = 2.0f;
    public float anguloMaximo = 70f;

    [Header("Velocidades e Colisão")]
    public float velocidadeDescer = 550f;
    public float velocidadeSubir = 500f;
    public float velocidadeSubirMinima = 40f;
    public float raioColisao = 80f;

    private enum Estado { Girando, Descendo, Subindo }
    private Estado estadoAtual = Estado.Girando;

    private float tempoGiro = 0f;
    private float distanciaDescida = 0f;
    private float garraYInicial = 0f;
    private float cordaOrigemY = 0f;

    private ItemMineracao itemFisgado = null;
    private readonly List<RectTransform> segmentos = new List<RectTransform>();

    private void Start()
    {
        if (garraRect != null) garraYInicial = garraRect.anchoredPosition.y;
        if (cordasContainer != null) cordaOrigemY = cordasContainer.anchoredPosition.y;

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

    private void TickGirando()
    {
        tempoGiro += Time.deltaTime;
        float angulo = Mathf.Sin(tempoGiro * velocidadeGiro) * anguloMaximo;
        transform.localRotation = Quaternion.Euler(0f, 0f, angulo);

        bool clicou = false;
        Vector2 posicaoClique = Vector2.zero;

        // API Unificada: Captura Mouse, Touch e Caneta sem conflitos!
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            clicou = true;
            posicaoClique = Pointer.current.position.ReadValue();
        }

        if (Time.timeScale > 0 && clicou)
        {
            // Passamos a posição exata do clique para o nosso "Raio-X" de botões
            if (!EstaSobreUmBotao(posicaoClique))
            {
                EntrarDescendo();
            }
        }

        cordasContainer.gameObject.SetActive(false);
    }
    private bool EstaSobreUmBotao(Vector2 posicaoTela)
    {
        if (EventSystem.current == null) return false;

        PointerEventData ev = new PointerEventData(EventSystem.current);
        ev.position = posicaoTela;

        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ev, hits);

        foreach (var h in hits)
        {
            // Se encontrar algum botão debaixo do dedo, bloqueia o gancho
            if (h.gameObject.GetComponentInParent<Button>() != null) return true;
        }

        return false;
    }

    private void TickDescendo()
    {
        // Pega o limite circular normal do upgrade (Nível 3 é infinito)
        float alcance = MGMineracao.instance != null ? MGMineracao.instance.ObterAlcanceMaximo() : 700f;

        distanciaDescida += velocidadeDescer * Time.deltaTime;
        distanciaDescida = Mathf.Min(distanciaDescida, alcance);

        AplicarPosicaoGarra();
        SincronizarObjetoMinerado();
        AtualizarCorda();
        ChecarColisao();

        // NOVIDADE: Checagem ABSOLUTA do tamanho da tela!
        bool bateuNaTela = BateuNaBordaDaTela();

        // Se chegou no limite da cordinha OU bateu nos cantos do telemóvel, ele volta!
        if (bateuNaTela || distanciaDescida >= alcance)
        {
            estadoAtual = Estado.Subindo;
        }

        cordasContainer.gameObject.SetActive(true);
    }

    /// <summary>
    /// Converte a posição 3D da garra para pixels na tela real do dispositivo
    /// e avisa se ela ultrapassou as bordas (esquerda, direita ou chão).
    /// </summary>
    private bool BateuNaBordaDaTela()
    {
        if (garraRect == null) return false;

        // Calcula a posição global EXATA dos dentes
        Vector3 posDentes = garraRect.TransformPoint(new Vector3(0, -garraRect.rect.height * 0.8f, 0));

        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        Vector2 posTela = RectTransformUtility.WorldToScreenPoint(cam, posDentes);

        // Margem de segurança (5 pixels) para não vazar nada visualmente
        float margem = 5f;

        // Se a posição da garra for menor que a margem (esquerda/chão) 
        // ou maior que a resolução da tela (direita), ela bateu!
        if (posTela.x <= margem || posTela.x >= Screen.width - margem || posTela.y <= margem)
        {
            return true;
        }

        return false;
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

            // 1. APLICAMOS A POSIÇÃO FINAL EXATA ANTES DE FAZER QUALQUER OUTRA COISA
            AplicarPosicaoGarra();
            SincronizarObjetoMinerado();

            // 2. AGORA ESCONDEMOS A CORDA (assim não há o "salto" visual)
            EsconderCorda();

            // 3. ENTREGAMOS O ITEM E VOLTAMOS A GIRAR
            EntregarItem();
            EntrarGirando();
            return;
        }

        AplicarPosicaoGarra();
        SincronizarObjetoMinerado();
        AtualizarCorda();
    }

    private void AplicarPosicaoGarra()
    {
        if (garraRect == null) return;
        garraRect.anchoredPosition = new Vector2(0f, garraYInicial - distanciaDescida);
    }

    private void SincronizarObjetoMinerado()
    {
        if (objetoMineradoRect == null || garraRect == null) return;
        objetoMineradoRect.anchoredPosition = garraRect.anchoredPosition;
    }

    private void AtualizarCorda()
    {
        if (cordasContainer == null || prefabSegmentoCorda == null) return;

        float comprimento = 0f;
        if (garraRect != null)
        {
            // O comprimento deve ser exatamente a distância entre a origem da corda e o Y atual da garra
            comprimento = Mathf.Abs(garraRect.anchoredPosition.y - cordaOrigemY);
        }

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

        while (segmentos.Count < totalNecessario)
            CriarSegmento();

        for (int i = 0; i < segmentos.Count; i++)
        {
            if (segmentos[i] == null) continue;

            if (i >= totalNecessario)
            {
                segmentos[i].gameObject.SetActive(false);
                continue;
            }

            segmentos[i].gameObject.SetActive(true);
            segmentos[i].anchoredPosition = new Vector2(0f, -i * alturaSegmento);

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

    private void ChecarColisao()
    {
        if (garraRect == null || MGMineracao.instance == null) return;

        Vector2 posDentes = garraRect.TransformPoint(new Vector3(0, -garraRect.rect.height * 0.8f, 0));

        foreach (ItemMineracao item in MGMineracao.instance.itensNaTela)
        {
            if (item == null || item.foiPego) continue;

            if (Vector2.Distance(posDentes, (Vector2)item.transform.position) <= raioColisao)
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

        if (objetoMineradoRect != null && garraRect != null)
        {
            // Pega a coordenada global EXATA dos dentes da garra e joga a pedra pra lá
            Vector3 posDentes = garraRect.TransformPoint(new Vector3(0, -garraRect.rect.height * 0.8f, 0));
            item.transform.position = posDentes;

            item.transform.SetParent(objetoMineradoRect, true);
        }

        SincronizarObjetoMinerado();
        AplicarSpriteGarra(spriteFechado);
        estadoAtual = Estado.Subindo;
    }

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