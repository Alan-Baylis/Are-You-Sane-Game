using UnityEngine;
using System.Collections;

public class LightBehaviour : MonoBehaviour
{    
    private const string EMPTY_LIGHT = "LightEmpty";
    private const string BLINK_LIGHT = "LightBlinking";

    private Animator m_Animation;
    private Light m_Light;
    private Material m_Material;
    private Color m_InitialColor;
    private string m_ActiveAnimationString;

    private void Start()
    {
        m_Light = GetComponent<Light>();
        m_Animation = GetComponent<Animator>();
        m_Material = MyHelper.FindComponentInChildWithTag<MeshRenderer>(this.gameObject, GameTag.Material).material;
        m_InitialColor = m_Material.color;
    }

    private void PlayAnimation(string state)
    {
        m_Animation.Play(state, -1, 0f);
    }

    public void Blink()
    {
        PlayAnimation(BLINK_LIGHT);
    }

    public void LightOff()
    {
        PlayAnimation(EMPTY_LIGHT);
        m_Light.intensity = 0f;
        m_Material.color = Color.black;
    }

    public void LightOn()
    {
        PlayAnimation(EMPTY_LIGHT);
        m_Light.intensity = 1.2f;
        m_Material.color = m_InitialColor;
    }




}
