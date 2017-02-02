using UnityEngine;
using System.Collections;

public class PlayerObject : MonoBehaviour
{
    private const int COMPONENT_COUNT = 5;
    private PlayerComponent[] m_components = new PlayerComponent[COMPONENT_COUNT];

    private PlayerController    m_movement;
    private PlayerEventsUI      m_UI;
    private PlayerFlashLight    m_flashlight;
    private PlayerHeuristics    m_heuristics;
    private PlayerInteractions  m_interactions;

    private bool m_active = false;
    public bool IsActivated { get { return m_active; } }

    public PlayerController Controller      { get { return m_movement; } }
    public PlayerEventsUI UI                { get { return m_UI; } }
    public PlayerFlashLight Flashlight      { get { return m_flashlight; } }
    public PlayerHeuristics Heuristics      { get { return m_heuristics; } }
    public PlayerInteractions Interactions  { get { return m_interactions; } }

	// Use this for initialization
	void Start ()
    {
        m_movement      = GetComponentInChildren<PlayerController>();
        m_UI            = GetComponentInChildren<PlayerEventsUI>();
        m_flashlight    = GetComponentInChildren<PlayerFlashLight>();
        m_heuristics    = GetComponentInChildren<PlayerHeuristics>();
        m_interactions  = GetComponentInChildren<PlayerInteractions>();
        Setup();
	}

    // Update is called once per frame
    void Update() { }

    private void Setup()
    {
        m_components[0] = m_movement;
        m_components[1] = m_UI;
        m_components[2] = m_flashlight;
        m_components[3] = m_heuristics;
        m_components[4] = m_interactions;
        foreach (PlayerComponent component in m_components)
        {
            Debug.Log(component);
            component.Configure(this);
        }

        m_active = true;
    }

    public void PlayerEnd(EndCondition condition) // Other Death things here such as disabling audio and gameobjects/repositioning maybe?
    {
        m_active = false;
        m_UI.ShowGameOverUI(condition);
        m_movement.DisableMouseLock();
    }
}
