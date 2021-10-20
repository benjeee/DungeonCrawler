using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] public float countdown_time = 30.0f;

    public float countdown_time_remaining = 0.0f;

    public static GameManager instance;

    public enum GameState { SEARCHING, FOUND, SUCCESS, FAILURE }

    public GameState gameState = GameState.SEARCHING;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than 1 GameManager instantiated!");
        }
        else
        {
            instance = this;
        }
    }

    public void StartCountdown()
    {
        Debug.Log("STARTING COUNTDOWN");
        gameState = GameState.FOUND;
        countdown_time_remaining = countdown_time;
    }

    public void CompleteGame()
    {
        Debug.Log("SUCCESS");
        gameState = GameState.SUCCESS;
    }

    void Update()
    {
        if (gameState == GameState.FOUND)
        {
            countdown_time_remaining -= Time.deltaTime;
            if (countdown_time_remaining < 0.0f)
            {
                gameState = GameState.FAILURE;
                Debug.Log("FAILURE!!!!");
            }
        }
    }
}
