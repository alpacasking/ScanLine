using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class UpdateDistanceLine : MonoBehaviour
{
    private PostProcessVolume m_Volume;
    private ScanLine m_scanLine;
    public Transform TargetObject;
    private 
    // Start is called before the first frame update
    void Start()
    {
        m_scanLine = ScriptableObject.CreateInstance<ScanLine>();
        m_scanLine.enabled.Override(true);
        

        m_Volume = PostProcessManager.instance.QuickVolume(gameObject.layer, 100f, m_scanLine);
    }

    // Update is called once per frame
    void Update()
    {
        m_scanLine.Target.Override(TargetObject.position);
        m_scanLine.DistanceThresold.Override(Time.time*10%20);
    }

    void OnDestroy()
    {
        RuntimeUtilities.DestroyVolume(m_Volume, true, true);
    }
}

