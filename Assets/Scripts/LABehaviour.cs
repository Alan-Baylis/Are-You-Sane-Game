using UnityEngine;
using System.Collections;

public class LABehaviour : MonoBehaviour
{
    private IBehaviourTreeNode m_tree;

    // Use this for initialization
    void Start ()
    {
        BehaviourTreeBuilder builder = new BehaviourTreeBuilder();
        this.m_tree = builder
            .Sequence("my-sequence")
            .Do("action1", t =>
            {
                return BehaviourTreeStatus.Success;
            })
            .Do("action2", t =>
            {
                return BehaviourTreeStatus.Success;
            })
            .End().Build();
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
