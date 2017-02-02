using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIInsanity : MonoBehaviour
{
    public Image m_Background;
    public Image m_FocalRim;

    private Color32 m_BackgroundColour;
    private Color32 m_FocalRimColour;

    private float m_BackgroundAlphaC;
    private float m_FocalRimAlphaC;

    private static float m_BackgroundAlphaIncrement;
    private static float m_FocalRimAlphaIncrement;

    public static void SetPerSecondIncrements(float bAmount, float fAmount)
    {
        m_BackgroundAlphaIncrement  = bAmount;
        m_FocalRimAlphaIncrement    = fAmount;

        Debug.Log("Increase Back:" + m_BackgroundAlphaIncrement);
        Debug.Log("Increase Focal:" + m_FocalRimAlphaIncrement);
    }

    public void IncreaseAlpha()
    {
        m_BackgroundAlphaC += m_BackgroundAlphaIncrement;
        m_FocalRimAlphaC += m_FocalRimAlphaIncrement;

        if (m_BackgroundAlphaC > 1)
        {
            m_BackgroundAlphaC = Mathf.Abs(1 - m_BackgroundAlphaC);
            m_BackgroundColour.a++;
            m_Background.color = m_BackgroundColour;
        }

        if (m_FocalRimAlphaC > 1)
        {
            m_FocalRimAlphaC = Mathf.Abs(1 - m_FocalRimAlphaC);
            m_FocalRimColour.a++;
            m_FocalRim.color = m_FocalRimColour;
        }
    }

	// Use this for initialization
	void Start ()
    {
        m_BackgroundAlphaC  = 0.0f;
        m_FocalRimAlphaC    = 0.0f;
        m_BackgroundColour  = m_Background.color;
        m_FocalRimColour    = m_FocalRim.color;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
