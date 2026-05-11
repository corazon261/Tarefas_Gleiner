using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public enum TipoMinigame { Agua, Planta, Mineracao, Outro }

/// <summary>
/// Gerencia o tempo de espera e o limite de tentativas diárias dos minigames.
/// </summary>
public class DailyCooldown : MonoBehaviour
{
    [Header("--- DEBUG ---")]
    [Tooltip("Se ativado, ignora o tempo e permite jogar infinitamente.")]
    public bool modoTeste = false; 

    [Header("Configuração Geral")]
    public TipoMinigame meuTipoMinigame; 
    public int tentativasMaximas = 3; 

    [Header("Tempo de Recarga")]
    public int horas = 24;
    public int minutos = 0;
    public int segundos = 0;

    [Header("Interface")]
    public Button botaoEntrar; 
    public TextMeshProUGUI textoContador; 

    private int tentativasRestantes;
    private DateTime dataProximaRecarga;
    private bool estaBloqueado = false;
    
    // Propriedade que gera a chave de Save dinamicamente evitando erros de digitação
    private string ChaveSave => $"Cooldown_{meuTipoMinigame}";

    private void OnEnable()
    {
        CarregarDados();
        VerificarRecarga();
        AtualizarUI();
    }

    private void Update()
    {
        if (modoTeste)
        {
            AplicarModoTeste();
            return; 
        }

        AtualizarRelogio();
    }

    /// <summary>Verifica se o jogador ainda tem tentativas ou se o tempo já passou.</summary>
    public bool PodeJogar()
    {
        if (modoTeste) return true;
        
        CarregarDados();
        VerificarRecarga();
        return !estaBloqueado && tentativasRestantes > 0;
    }

    /// <summary>Consome uma tentativa do jogador e salva o progresso.</summary>
    public void GastarTentativa()
    {
        if (modoTeste) return;

        CarregarDados();
        if (tentativasRestantes > 0)
        {
            tentativasRestantes--;
            PlayerPrefs.SetInt($"{ChaveSave}_Tentativas", tentativasRestantes);
            
            if (tentativasRestantes <= 0) BloquearJogo();
            
            AtualizarUI();
            PlayerPrefs.Save();
        }
    }

    /// <summary>Inicia a contagem regressiva travando o minigame.</summary>
    private void BloquearJogo()
    {
        estaBloqueado = true;
        dataProximaRecarga = DateTime.Now.AddHours(horas).AddMinutes(minutos).AddSeconds(segundos);
        
        PlayerPrefs.SetString($"{ChaveSave}_DataRecarga", dataProximaRecarga.ToString());
        
        if (botaoEntrar) botaoEntrar.interactable = false;
    }

    /// <summary>Checa se o tempo de bloqueio já expirou e reseta as tentativas.</summary>
    private void VerificarRecarga()
    {
        if (!PlayerPrefs.HasKey($"{ChaveSave}_DataRecarga")) return;

        string dataString = PlayerPrefs.GetString($"{ChaveSave}_DataRecarga");
        if (DateTime.TryParse(dataString, out dataProximaRecarga))
        {
            if (DateTime.Now >= dataProximaRecarga)
            {
                ResetarTentativas();
            }
            else
            {
                estaBloqueado = true;
                if (botaoEntrar) botaoEntrar.interactable = false;
            }
        }
        else
        {
            ResetarTentativas(); // Se a data estiver corrompida, libera por segurança
        }
    }

    /// <summary>Devolve todas as tentativas ao jogador.</summary>
    private void ResetarTentativas()
    {
        tentativasRestantes = tentativasMaximas;
        estaBloqueado = false;
        
        PlayerPrefs.SetInt($"{ChaveSave}_Tentativas", tentativasMaximas);
        PlayerPrefs.DeleteKey($"{ChaveSave}_DataRecarga");
        
        if (botaoEntrar) botaoEntrar.interactable = true;
        AtualizarUI();
    }

    private void CarregarDados()
    {
        tentativasRestantes = PlayerPrefs.GetInt($"{ChaveSave}_Tentativas", tentativasMaximas);
    }

    private void AtualizarUI()
    {
        if (!estaBloqueado && textoContador)
        {
            textoContador.text = $"{meuTipoMinigame}: {tentativasRestantes}/{tentativasMaximas}";
        }
    }

    private void AtualizarRelogio()
    {
        if (!estaBloqueado || textoContador == null) return;

        TimeSpan tempo = dataProximaRecarga - DateTime.Now;
        if (tempo.TotalSeconds > 0) 
        {
            textoContador.text = $"{tempo.Hours:00}:{tempo.Minutes:00}:{tempo.Seconds:00}";
        }
        else 
        {
            ResetarTentativas();
        }
    }

    private void AplicarModoTeste()
    {
        if (textoContador) textoContador.text = "MODO DEV";
        if (botaoEntrar) botaoEntrar.interactable = true; 
    }




    /// <summary>Devolve uma tentativa ao jogador (caso ele aborte o jogo).</summary>
    public void DevolverTentativa()
    {
        if (modoTeste) return;

        CarregarDados();
        if (tentativasRestantes < tentativasMaximas)
        {
            tentativasRestantes++;
            PlayerPrefs.SetInt($"{ChaveSave}_Tentativas", tentativasRestantes);
            
            // Se estava bloqueado, libera o botão novamente
            if (estaBloqueado)
            {
                estaBloqueado = false;
                PlayerPrefs.DeleteKey($"{ChaveSave}_DataRecarga");
                if (botaoEntrar) botaoEntrar.interactable = true;
            }
            
            AtualizarUI();
            PlayerPrefs.Save();
        }
    }
}