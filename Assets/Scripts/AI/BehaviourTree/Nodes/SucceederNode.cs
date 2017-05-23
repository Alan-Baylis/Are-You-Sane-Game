using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SucceederNode : ParentBehaviour<IBehaviourTreeNode>
{
    protected string m_Name;
    protected List<IBehaviourTreeNode> m_Children = new List<IBehaviourTreeNode>();

    public SucceederNode(string name)
    {
        this.m_Name = name;
    }

    public override BehaviourTreeStatus Tick(TimeData time)
    {
        foreach (var child in m_Children)
            child.Tick(time);

        return BehaviourTreeStatus.Success;
    }

    public override void AddChild<U>(U child)
    {
        m_Children.Add(child);
    }
}
