using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartZoneInteractable : Interactable
{
    public override void OnInteract()
    {
        if (GameManager.instance.gameState == GameManager.GameState.FOUND)
        {
            GameManager.instance.CompleteGame();
        }
    }
}
