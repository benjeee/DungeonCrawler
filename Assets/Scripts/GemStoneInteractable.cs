using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemStoneInteractable : Interactable
{
    public override void OnInteract()
    {
        Debug.Log("INTERACT GEM STONE");
        GameManager.instance.StartCountdown();
        Destroy(this.gameObject);
    }
}
