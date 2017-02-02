using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum EndCondition
{
    Death = 0,
    Insane = 1,
    Escape = 2
}


public class UIGameOver : MonoBehaviour
{
    private const string PLAY_AGAIN = "Play Again?";
    private const float TIMER_DELAY = 2.0f;
    private float m_ColorTimer = 4.0f;

    private Color32 m_ColorRedFlash = new Color32(255, 0, 0, 100);
    private Color32 m_ColorRedText = new Color32(180, 15, 15, 255);
    private Color32 m_ColorTransparent = new Color32(0, 0, 0, 0);
    private Color32 m_ColorStore;

    public Image backPanel;
    public Text diedText;
    public GameObject menuOptions;

    private bool m_Fade1 = false;
    private bool m_Fade2 = false;
    private EndCondition m_EndStore;

    // Use this for initialization
    void Start()
    {
        menuOptions.SetActive(false);
    }

    public void Toggle(bool active)
    {
        this.gameObject.SetActive(active);
    }

    // Update is called once per frame
    void Update()
    {
        if (m_Fade1)
        {
            if (m_EndStore == EndCondition.Death)
                backPanel.color = Color32.Lerp(backPanel.color, Color.black, Time.time);

            if (m_ColorTimer > 2)
            {
                m_ColorTimer -= Time.deltaTime;
                diedText.color = Color32.Lerp(diedText.color, m_ColorStore, 2 * Time.deltaTime);
            }
            else if (m_ColorTimer > 0)
            {
                m_ColorTimer -= Time.deltaTime;
                diedText.color = Color32.Lerp(diedText.color, m_ColorTransparent, 2 * Time.deltaTime);
            }
            else
            {
                m_ColorTimer = TIMER_DELAY;
                m_Fade1 = false;
                m_Fade2 = true;
                diedText.text = PLAY_AGAIN;
            }
        }
        else if (m_Fade2)
        {
            if (m_ColorTimer > 0)
            {
                m_ColorTimer -= Time.deltaTime;
                diedText.color = Color32.Lerp(diedText.color, Color.white, 2 * Time.deltaTime);
            }
            else
            {
                m_ColorTimer = TIMER_DELAY;
                menuOptions.SetActive(true);
                m_Fade2 = false;
            }
        }
    }

    private string ObtainConditionText(EndCondition condition)
    {
        switch(condition)
        {
            case EndCondition.Death:
                return "You Died";

            case EndCondition.Escape:
                return "You Escaped!";

            case EndCondition.Insane:                
                return "You Have Gone Insane";

            default:
                return string.Empty;
        }
    }

    public void ShowDeathUI(EndCondition condition)
    {
        if (!m_Fade1)
        {
            m_EndStore = condition;
            m_ColorStore = (condition == EndCondition.Escape) ? (Color32)Color.white : m_ColorRedText;
            diedText.text = ObtainConditionText(condition);

            Debug.Log("Dead");
            if (condition == EndCondition.Death) backPanel.color = m_ColorRedFlash;
            m_Fade1 = true;
        }
    }
}
