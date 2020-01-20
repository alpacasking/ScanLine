using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetCamera : MonoBehaviour
{
    public Camera MyCamera;
    public DepthTextureMode Mode;
    // Start is called before the first frame update
    void Start()
    {
        MyCamera.depthTextureMode |= Mode;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
