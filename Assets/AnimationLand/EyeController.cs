using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeController : MonoBehaviour
{
    public Renderer rend;

    public animatedExpressions[] faceAnims;

    public float scrollSpeed = 1f;
    public float changeInterval = 0.25F;

    List<string> animationList;

    [Dropdown("animationList")] 
    public string animToPlay;

    public int currentAnim;
    
    float timer = 0.0f;
    float offset;
    int animNum;

    // Start is called before the first frame update
    void Start()
    {
        rend = GetComponent<Renderer>();
        offset = 0;
        animNum = 0;
        currentAnim = 0;

        animationList = new List<string>();
        for(int i = 0; i < faceAnims.Length; i++)
        {
            animationList.Add(faceAnims[i].animName);
        }
        animToPlay = animationList[0];
    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < faceAnims.Length; i++)
        {
            if(faceAnims[i].animName.Equals(animToPlay))
            {
                currentAnim = i;
                break;
            }
            else
            {
                currentAnim = 0;
            }
        }
        timer += Time.deltaTime;
        if(timer >= scrollSpeed)
        {
            if(animNum < faceAnims[currentAnim].animOrder.GetLength(0)-1)
            {
                animNum +=1;
            }
            else 
            {
                animNum = 0;
            }
            timer = timer - scrollSpeed;
            offset = faceAnims[currentAnim].animOrder[animNum];
        }
        
        rend.material.mainTexture = faceAnims[currentAnim].texture;
        rend.material.SetTexture ("_EmissionMap", faceAnims[currentAnim].texture);
        rend.material.SetTextureOffset("_MainTex", new Vector2(offset,0));
        rend.material.SetTextureOffset("_MainTex", new Vector2(offset,0));
        rend.material.SetTextureOffset("_EmissionMap", new Vector2(offset,0));
    }
}
