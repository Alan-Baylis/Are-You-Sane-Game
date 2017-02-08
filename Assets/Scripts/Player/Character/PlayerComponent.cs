using UnityEngine;
using System.Collections;

interface PlayerConfig
{
    void Configure(PlayerObject player);
}

public abstract class PlayerComponent : MonoBehaviour, PlayerConfig
{
    protected PlayerObject m_player;
    protected bool m_activated;
    public bool IsActivated { get { return m_activated; } }    
    public virtual void Start() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public void Configure(PlayerObject player)
    {
        m_player = player;
        m_activated = true;
    }
}
