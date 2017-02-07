using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameTimer : MonoBehaviour
{
    [SerializeField]
    private UIInsanity m_InsanityControl;

    [SerializeField]
    private bool m_IgnoreTimerTesting = false;


    private PlayerObject m_player;
    private Text m_textUI;
    private int m_timerInc;
    private float m_timer;
    private float m_gameMinutes;
    private float m_gameHour;
    private float m_timerMax = 2f;

    // Use this for initialization
    void Start ()
    {
        m_player = GameObject.FindGameObjectWithTag(GameTag.Player).GetComponent<PlayerObject>();        
        m_textUI = GetComponent<Text>();
        ResetTimer();

        if (m_IgnoreTimerTesting)
            m_timerMax = 80000f;
        
    }

    public void ResetTimer()
    {
        m_gameHour      = 10f;
        m_gameMinutes   = 0f;
        m_timer         = 0f;
        m_textUI.text   = "10:00 pm";
    }

    // Update is called once per frame
    void Update ()
    {
        if (m_player == null) return;
        if (!m_player.IsActivated) return;

	    if (m_timer < m_timerMax)
        {
            m_timer += Time.deltaTime;
        }
        else
        {
            // Apply Visual Effects to Camera and UI here (per second for insanity)
            m_InsanityControl.IncreaseAlpha();
            m_player.UI.IncreaseMotionBlur();

            // The vortex limit should have already been set by this point
            if (GameData.Difficulty == GameDifficulty.Psychotic) m_player.UI.IncreaseVortex();

            m_timer = 0f;
            if (m_gameMinutes < 59)
            {
                m_gameMinutes++;
            }
            else
            {
                // Increase Player insanity every hour by amount set in game data due to difficulty
                m_player.Heuristics.IncreaseInsanity();
                m_gameMinutes = 0;
                m_gameHour++;                
            }

            if (m_gameHour > 11)
            {
                m_gameHour = 0;            
            }  

            // Dont concatinate this variable assignment until we are sure of everything working
            if (m_gameMinutes < 10)
            {
                if (m_gameHour < 10)
                {
                    m_textUI.text = "0" + m_gameHour.ToString("f0") + ":0" + m_gameMinutes.ToString("f0");
                }
                else
                {
                    m_textUI.text = m_gameHour.ToString("f0") + ":0" + m_gameMinutes.ToString("f0");
                }
            }
            else
            {
                if (m_gameHour < 10)
                {
                    m_textUI.text = "0" + m_gameHour.ToString("f0") + ":" + m_gameMinutes.ToString("f0");
                }
                else
                {
                    m_textUI.text = m_gameHour.ToString("f0") + ":" + m_gameMinutes.ToString("f0");
                }
                
            }

            // We are certain their is only going to be one night
            if (m_gameHour != 11) 
            {
                m_textUI.text += " am";
            }
            else
            {
                m_textUI.text += " pm";
            }
        }
	}
}
