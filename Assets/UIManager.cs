using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [SerializeField] Text countdownText;
    [SerializeField] Text successFailText;

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

    // Update is called once per frame
    void Update()
    {
        GameManager gmInstance = GameManager.instance;
        if (gmInstance.gameState == GameManager.GameState.FOUND)
        {
            countdownText.text = gmInstance.countdown_time_remaining.ToString();
        }
        else
        {
            countdownText.text = "";
            if (gmInstance.gameState == GameManager.GameState.SUCCESS)
            {
                successFailText.text = "SUCCESS";
            }
            else if (gmInstance.gameState == GameManager.GameState.FAILURE)
            {
                successFailText.text = "FAILURE";
            }
            else
            {
                successFailText.text = "";
            }
        }
    }
}
