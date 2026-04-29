using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.Rendering.HableCurve;

/// <summary>
/// Controla o balanço, descida, subida e colisão do gancho no minigame de mineração.
/// </summary>
public class GanchoMineracao : MonoBehaviour
{
    [Header("Garra")]
    public Image imagemGarra;
    public Sprite spriteAberto;
    public Sprite spriteFechado;
    private float posicaoInicialGarraY;

    [Header("Corda")]
    public RectTransform cordaContainer;
    public GameObject prefabSegmentoCorda;
    public float tamanhoSegmento = 20f;
    private float acumuladorCorda = 0f;


    private List<GameObject> segmentos = new List<GameObject>();

    [Header("Referências")]
    public RectTransform meuRect; 
    public Transform pontaDoGancho; 
    
    [Header("Configurações Iniciais Base")]
    public float velocidadeGiro = 2.5f;
    public float anguloMaximo = 65f;

    [Header("Alcance REAL do Gancho")]
    public float alcanceMaximo = 1000f;
    public float velocidadeDescerMax = 600f; 
    public float velocidadeSubirMax = 500f;  
    
    private enum Estado { Girando, Descendo, Subindo }
    private Estado estadoAtual = Estado.Girando;
    private float alturaOriginal;
    private ItemMineracao itemFisgado;

    private float velDescerCalculada;
    private float velSubirCalculada;
    private float tempoGiro = 0f;



    private void Start() 
    { 
        alturaOriginal = meuRect.sizeDelta.y;

        RectTransform rt = pontaDoGancho.GetComponent<RectTransform>();
        posicaoInicialGarraY = rt.anchoredPosition.y;
    }

    private void CalcularEficienciaAtual()
    {
        if (MGMineracao.instance == null) return;

        float efVelocidade = MGMineracao.instance.ObterEficiencia(MGMineracao.instance.nivelVelocidade);
        velDescerCalculada = velocidadeDescerMax * efVelocidade;
        velSubirCalculada = velocidadeSubirMax * efVelocidade;
    }

    private void Update()
    {
        switch (estadoAtual)
        {
            case Estado.Girando:
                tempoGiro += Time.deltaTime;
                float angulo = Mathf.Sin(tempoGiro * velocidadeGiro) * anguloMaximo;
                meuRect.localRotation = Quaternion.Euler(0, 0, angulo);

                bool disparar = false;

                bool clicou = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
                bool tocou = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

                if (Time.timeScale > 0 && (clicou || tocou))
                {
                    if (!EstaSobreUmBotao())
                        disparar = true;
                }

                if (disparar)
                {
                    CalcularEficienciaAtual();
                    estadoAtual = Estado.Descendo;
                }
                break;

            case Estado.Descendo:

                acumuladorCorda += velDescerCalculada * Time.deltaTime;

                while (acumuladorCorda >= tamanhoSegmento && (segmentos.Count + 1) * tamanhoSegmento <= alcanceMaximo)
                {
                    AdicionarSegmento();
                    acumuladorCorda -= tamanhoSegmento;
                }

                AtualizarPosicaoGarra();

                ChecarColisaoComOuro();

                float comprimentoAtual = Mathf.Abs(posicaoInicialGarraY) + (segmentos.Count * tamanhoSegmento);

                if (comprimentoAtual >= alcanceMaximo)
                {
                    estadoAtual = Estado.Subindo;
                }

                break;

            case Estado.Subindo:

                float velocidadeAtual = velSubirCalculada;

                if (itemFisgado != null)
                {
                    velocidadeAtual -= itemFisgado.pesoDesteItem;
                    velocidadeAtual = Mathf.Max(velocidadeAtual, 40f);
                }

                acumuladorCorda += velocidadeAtual * Time.deltaTime;

                while (acumuladorCorda >= tamanhoSegmento && segmentos.Count > 0)
                {
                    RemoverSegmento();
                    acumuladorCorda -= tamanhoSegmento;
                }

                AtualizarPosicaoGarra();

                if (segmentos.Count == 0)
                {
                    if (itemFisgado != null)
                    {
                        if (MGMineracao.instance != null)
                            MGMineracao.instance.AdicionarPontos(itemFisgado.valorDesteItem);

                        Destroy(itemFisgado.gameObject);
                        itemFisgado = null;
                    }

                    if (imagemGarra != null && spriteAberto != null)
                        imagemGarra.sprite = spriteAberto;

                    estadoAtual = Estado.Girando;
                }

                break;
        }
    }

    /// <summary>Raio-X para ignorar fundos de Canvas e detetar apenas botões reais.</summary>
    private bool EstaSobreUmBotao()
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            eventData.position = Touchscreen.current.primaryTouch.position.ReadValue();
        else if (Mouse.current != null)
            eventData.position = Mouse.current.position.ReadValue();

        List<RaycastResult> resultados = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, resultados);

        foreach (var hit in resultados)
        {
            if (hit.gameObject.GetComponentInParent<Button>() != null) return true; 
        }
        return false;
    }

    private void ChecarColisaoComOuro()
    {
        if (MGMineracao.instance == null) return;
        foreach (ItemMineracao ouro in MGMineracao.instance.ourosNaTela)
        {
            if (ouro == null || ouro.foiPego) continue;
            if (Vector2.Distance(pontaDoGancho.position, ouro.transform.position) < 60f)
            {
                ouro.foiPego = true;
                itemFisgado = ouro;

                ouro.transform.SetParent(pontaDoGancho);
                ouro.transform.localPosition = new Vector3(0f, -80f, 0f);

                if (imagemGarra != null && spriteFechado != null)
                    imagemGarra.sprite = spriteFechado;

                estadoAtual = Estado.Subindo; 
                break; 
            }
        }
    }

    public void ResetarGancho()
    {
        estadoAtual = Estado.Girando; 
        meuRect.localRotation = Quaternion.Euler(0, 0, 0); 
        if (alturaOriginal > 0) meuRect.sizeDelta = new Vector2(meuRect.sizeDelta.x, alturaOriginal);
        if (itemFisgado != null)
        {
            Destroy(itemFisgado.gameObject);
            itemFisgado = null;
        }
    }
    void AdicionarSegmento()
    {
        GameObject seg = Instantiate(prefabSegmentoCorda, cordaContainer);
        seg.transform.localScale = Vector3.one;

        float y = posicaoInicialGarraY - (segmentos.Count * tamanhoSegmento);
        seg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, y);

        segmentos.Add(seg);
    }

    void RemoverSegmento()
    {
        if (segmentos.Count == 0) return;

        GameObject ultimo = segmentos[segmentos.Count - 1];
        segmentos.RemoveAt(segmentos.Count - 1);
        Destroy(ultimo);
    }

    void AtualizarPosicaoGarra()
    {
        if (pontaDoGancho == null) return;

        RectTransform rt = pontaDoGancho.GetComponent<RectTransform>();

        float y = posicaoInicialGarraY - (segmentos.Count * tamanhoSegmento);
        rt.anchoredPosition = new Vector2(0, y);
    }


}