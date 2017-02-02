using UnityEngine;
using System.Collections;

public abstract class LAComponent : MonoBehaviour, LAConfig
{
    protected GameObject player;
    protected LAObject Annie;

    protected bool ActiveComponent { get { return Annie != null; } }

    public void Configure(GameObject player, LAObject annie)
    {
        this.player = player;
        this.Annie = annie;
    }

    

    //public virtual void SwitchState() { }

	// Use this for initialization
	public virtual void Start () { }

    // Update is called once per frame
    public virtual void Update () { }
}
