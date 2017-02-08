using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// This sequence will run a regular sequence return ONLY on the nodes which meet the weight condition. Moreover, only the node which meet the weight condition will be put into a regular sequence returning on the first
/// one that fails or success on when they all return a non-fail status.
/// </summary>
public class SequenceConditionNode : ParentBehaviour<IBehaviourWeightNode>
{

    protected string m_name;

    protected Func<float, bool> m_childrenFunction;
    protected List<IBehaviourWeightNode> m_children = new List<IBehaviourWeightNode>();

    public SequenceConditionNode(string name, Func<float, bool> fn)
    {
        m_childrenFunction = fn;
        m_name = name;
    }

    public override BehaviourTreeStatus Tick(TimeData time)
    {
        foreach (var child in m_children)
        {
            if (m_childrenFunction.Invoke(child.GetWeight()))
            {
                BehaviourTreeStatus status = child.Tick(time);
                if (status != BehaviourTreeStatus.Success)
                {
                    return status;
                }
            }
        }

        return BehaviourTreeStatus.Success;
    }

    public override void AddChild<U>(U child)
    {
        throw new NotImplementedException();
    }
}
