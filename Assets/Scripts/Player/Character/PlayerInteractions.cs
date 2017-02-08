using UnityEngine;
using System.Collections;

public class PlayerInteractions : PlayerComponent
{
    private bool m_largeDoorRange = false;

    public void EnterLargeDoorRange()
    {
        m_largeDoorRange = true;
    }

    public void LeaveLargeDoorRange()
    {
        m_largeDoorRange = false;
    }

	// Use this for initialization
	public override void Start ()
    {
	
	}

    // Update is called once per frame
    public override void Update ()
    {
	
	}

    
    
}
