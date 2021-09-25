using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_ScalabaleViewport : MonoBehaviour
{
    RectTransform rTransform;

    // Start is called before the first frame update
    void Start()
    {
        rTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        rTransform.sizeDelta = new Vector2(rTransform.sizeDelta.x, rTransform.position.y - 50);
    }
}
