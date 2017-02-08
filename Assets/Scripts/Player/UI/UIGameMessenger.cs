using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum GameMesseage
{    
    DoorPrompt = 0,
    DoorClose = 1,
    DoorLocked = 2
}


public class UIGameMessenger : MonoBehaviour
{
    private const string DOOR_CLOSE = "Press E to Close";
    private const string DOOR_PROMPT = "Press E to Open";
    private const string DOOR_LOCKED = "Door is Locked";
    private Color32 m_noAplhaWhite = new Color32(255, 255, 255, 0);
    private Text m_interactionMessage;
    private Text m_scriptMessage; // These will need to be public for multiple

    private string getMessage(GameMesseage message)
    {
        switch(message)
        {
            case GameMesseage.DoorLocked:
                return DOOR_LOCKED;

            case GameMesseage.DoorPrompt:
                return DOOR_PROMPT;

            case GameMesseage.DoorClose:
                return DOOR_CLOSE;

            default:
                return string.Empty;
        }
    }

    public void ShowInteractionMessage(GameMesseage message, float duration)
    {
        ShowInteractionMessage(message);
        m_interactionMessage.CrossFadeColor(m_noAplhaWhite, duration, true, true);
    }

    public void ShowInteractionMessage(GameMesseage message)
    {
        m_interactionMessage.CrossFadeColor(Color.white, 0f, true, true);
        m_interactionMessage.text = getMessage(message);
    }

    public void ClearInteractionMessage()
    {
        m_interactionMessage.text = string.Empty;
    }

	// Use this for initialization
	void Start ()
    {
        m_interactionMessage = GetComponent<Text>();

        if (m_interactionMessage != null)
        {
            ClearInteractionMessage();
        }
        else
        {
            Debug.LogError("No Message Text Has Been Assigned to the UIGame->UIGameMessenger");
        }
	}
	
	// Update is called once per frame
	void Update ()
    {

	}
}
