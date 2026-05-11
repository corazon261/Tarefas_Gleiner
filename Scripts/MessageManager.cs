using UnityEngine;
using TMPro;
using System.Collections;

public class MessageManager : MonoBehaviour
{
    public static MessageManager Instance;

    public GameObject painelMensagem;
    public TextMeshProUGUI textoMensagem;
    
    private Coroutine rotinaAtual; // Guarda apenas a rotina da mensagem atual

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if(painelMensagem) painelMensagem.SetActive(false);
    }

    public void MostrarMensagem(string msg)
    {
        if (painelMensagem == null) return;

        // Se já tiver uma mensagem na tela, apaga ela e começa a nova na hora
        if (rotinaAtual != null) StopCoroutine(rotinaAtual);
        
        rotinaAtual = StartCoroutine(RotinaMensagem(msg));
    }

    private IEnumerator RotinaMensagem(string msg)
    {
        textoMensagem.text = msg;
        painelMensagem.SetActive(true);
        
        // Usa o tempo real do computador, à prova de travamentos do Unity
        yield return new WaitForSecondsRealtime(3f);
        
        painelMensagem.SetActive(false);
        rotinaAtual = null;
    }
}