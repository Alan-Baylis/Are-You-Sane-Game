using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SelectorConditionNode : ParentBehaviour<IBehaviourWeightNode>
{
    private string m_name;
    private int m_childIndex;
    private float m_childWeight;

    private List<IBehaviourWeightNode> m_children = new List<IBehaviourWeightNode>();
    protected Func<float, bool> m_childrenFunction;

    public SelectorConditionNode(string name, Func<float, bool> fn)
    {
        m_childrenFunction = fn;
        m_name = name;
    }

    public override BehaviourTreeStatus Tick(TimeData time)
    {
        for (m_childIndex = 0; m_childIndex < m_children.Count; m_childIndex++)
        {
            m_childWeight = m_children[m_childIndex].GetWeight();
            if (m_childrenFunction.Invoke(m_childWeight))
            {
                return m_children[m_childIndex].Tick(time);
            }
        }

        return BehaviourTreeStatus.Failure;
    }

    public override void AddChild<U>(U child)
    {
        m_children.Add(child);
    }
}
