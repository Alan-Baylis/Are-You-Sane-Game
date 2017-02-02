﻿using System.Collections.Generic;

/// <summary>
/// Selects the first node that succeeds. Tries successive nodes until it finds one that doesn't fail.
/// </summary>
public class SelectorNode : IParentBehaviourTreeNode
{

    /// <summary>
    /// The name of the node.
    /// </summary>
    protected string name;

    /// <summary>
    /// List of child nodes.
    /// </summary>
    protected List<IBehaviourTreeNode> children = new List<IBehaviourTreeNode>(); //todo: optimization, bake this to an array.

    public SelectorNode(string name)
    {
        this.name = name;
    }

    public virtual BehaviourTreeStatus Tick(TimeData time)
    {
        foreach (var child in children)
        {
            var childStatus = child.Tick(time);
            if (childStatus != BehaviourTreeStatus.Failure)
            {
                return childStatus;
            }
        }

        return BehaviourTreeStatus.Failure;
    }

    /// <summary>
    /// Add a child node to the selector.
    /// </summary>
    public void AddChild(IBehaviourTreeNode child)
    {
        children.Add(child);
    }
}
