using UnityEngine;
using System;
using System.Collections.Generic;

public class RepeaterNode : ParentBehaviour<IBehaviourTreeNode>
{
    /// <summary>
    /// The name of the node.
    /// </summary>
    protected string name;

    /// <summary>
    /// The Single child node.
    /// </summary>
    protected IBehaviourTreeNode child; //todo: optimization, bake this to an array.

    protected Func<int, bool> repeater; // The lambda expression for the repeater

    protected int repeatCount; // Counter of the repeater

    public RepeaterNode(string name, Func<int, bool> repeater)
    {
        this.repeatCount = 0;
        this.name = name;
        this.repeater = repeater;
    }

    public override BehaviourTreeStatus Tick(TimeData time)
    {
        if(repeater.Invoke(repeatCount))
        {
            var childStatus = child.Tick(time);
            if (childStatus != BehaviourTreeStatus.Success && childStatus != BehaviourTreeStatus.Failure)
            {
                return childStatus;
            }

            repeatCount++;
        }

        repeatCount = 0;
        return BehaviourTreeStatus.Success;
    }

    /// <summary>
    /// Add a child node to the selector.
    /// </summary>
    public override void AddChild<T>(T child)
    {
        this.child = child;
    }

}
