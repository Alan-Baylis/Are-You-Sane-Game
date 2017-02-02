using UnityEngine;
using System.Collections;

public class CellDoorInteractor : MonoBehaviour
{
    public CellDoorScript m_DoorScript;
    private bool m_playerInRange = false;
    private PlayerObject m_player = null;

    void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Player")
        {
            Debug.Log("Player in Door");
            if (m_player == null)
                m_player = other.transform.GetComponent<PlayerObject>();

            if (!m_DoorScript.InTransition)
            {
                m_playerInRange = true;

                if (m_DoorScript.isOpened)
                {
                    m_player.UI.PromptDoorCloseMessage();
                }
                else
                {
                    m_player.UI.PromptDoorMessage();
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform.tag == "Player")
        {
            m_playerInRange = false;
            m_player.UI.ClearInteractionMessage();
        }
    }

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (m_playerInRange)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (m_DoorScript.AttemptToOpen())
                {
                    m_player.UI.ClearInteractionMessage();
                }
                else
                {
                    m_player.UI.LockedDoorMessage();
                }
            }
        }
    }
}
