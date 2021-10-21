using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReticuleCenter : MonoBehaviour
{
    [SerializeField] bool left = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Canvas canvas = FindObjectOfType<Canvas>();

        float h = canvas.GetComponent<RectTransform>().rect.height;
        float w = canvas.GetComponent<RectTransform>().rect.width;

        RectTransform rt = GetComponent<RectTransform>();
        if (left)
        {
            rt.anchoredPosition = new Vector3((-w / 4), rt.anchoredPosition.y, 0.0f);
        }
        else
        {
            rt.anchoredPosition = new Vector3((w / 4), rt.anchoredPosition.y, 0.0f);
        }
        
    }
}
