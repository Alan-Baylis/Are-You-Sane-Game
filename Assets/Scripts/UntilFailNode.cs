using UnityEngine;
using System.Collections;
using System;

public class UntilFailNode : RepeaterNode
{

    public UntilFailNode(string name, Func<int, bool> repeater) : base (name, repeater)
    {
        this.repeatCount = 0;
        this.name = name;
        this.repeater = repeater;
    }

    public override BehaviourTreeStatus Tick(TimeData time)
    {
        while (repeater.Invoke(repeatCount))
        {
            var childStatus = child.Tick(time);
            if (childStatus != BehaviourTreeStatus.Success && childStatus != BehaviourTreeStatus.Failure)
            {
                return childStatus;
            }

            if (childStatus == BehaviourTreeStatus.Failure)
                break;

            repeatCount++;
        }

        repeatCount = 0;
        return BehaviourTreeStatus.Success;
    }

}
