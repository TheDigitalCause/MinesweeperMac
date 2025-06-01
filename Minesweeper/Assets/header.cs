using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class header : MonoBehaviour
{
    public bool onHeader;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            onHeader = true;
        }
        else
        {
            onHeader = false;
        }
    }
    
}
