using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemStoneInteractable : Interactable
{
    public override void OnInteract()
    {
        GameManager.instance.StartCountdown();
        Destroy(this.gameObject);
    }
}
