﻿using System;

public class ActionNode : IBehaviourTreeNode
{

    /// <summary>
    /// The name of the node.
    /// </summary>
    protected string name;

    /// <summary>
    /// Function to invoke for the action.
    /// </summary>
    protected Func<TimeData, BehaviourTreeStatus> fn;

    public ActionNode(string name, Func<TimeData, BehaviourTreeStatus> fn)
    {
        this.name = name;
        this.fn = fn;
    }

    public BehaviourTreeStatus Tick(TimeData time)
    {
        return fn(time);
    }

}
