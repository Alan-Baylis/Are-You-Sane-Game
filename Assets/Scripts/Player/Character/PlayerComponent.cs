using UnityEngine;
using System.Collections;

interface PlayerConfig
{
    void Configure(PlayerObject player);
}

public abstract class PlayerComponent : MonoBehaviour, PlayerConfig
{
    public bool IsActivated { get { return m_Activated; } }    
    protected PlayerObject m_Player;
    protected bool m_Activated;
    public virtual void Start() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public void Configure(PlayerObject player)
    {
        m_Player = player;
        m_Activated = true;
    }
}
