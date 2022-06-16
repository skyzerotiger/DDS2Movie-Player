using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class D2MTest : MonoBehaviour
{
    D2M d2m;
    public string filename;
    public RawImage rawImage;
    
    // Start is called before the first frame update
    void Start()
    {
        d2m = new D2M();
        d2m.Load(Application.streamingAssetsPath + filename);
        rawImage.texture = d2m.texture;
        rawImage.rectTransform.sizeDelta = new Vector2(d2m.width, d2m.height);
    }

    // Update is called once per frame
    void Update()
    {
        d2m.Update();
    }
}
