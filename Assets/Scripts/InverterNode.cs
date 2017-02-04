﻿using System;


/// <summary>
/// Decorator node that inverts the success/failure of its child.
/// </summary>
public class InverterNode : ParentBehaviour<IBehaviourTreeNode>
{

    /// <summary>
    /// Name of the node.
    /// </summary>
    private string name;

    /// <summary>
    /// The child to be inverted.
    /// </summary>
    private IBehaviourTreeNode childNode;

    public InverterNode(string name)
    {
        this.name = name;
    }

    public override BehaviourTreeStatus Tick(TimeData time)
    {
        if (childNode == null)
        {
            throw new ApplicationException("InverterNode must have a child node!");
        }

        var result = childNode.Tick(time);
        if (result == BehaviourTreeStatus.Failure)
        {
            return BehaviourTreeStatus.Success;
        }
        else if (result == BehaviourTreeStatus.Success)
        {
            return BehaviourTreeStatus.Failure;
        }
        else
        {
            return result;
        }
    }

    /// <summary>
    /// Add a child to the parent node.
    /// </summary>
    public override void AddChild<T>(T child)
    {
        if (this.childNode != null)
        {
            throw new ApplicationException("Can't add more than a single child to InverterNode!");
        }

        this.childNode = child;
    }


}
