using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectorHighestWeight : ParentBehaviour<IBehaviourWeightNode>
{
    private string name;
    private int childIndex;
    private int highestIndex;
    private float childWeight;
    private float highestWeight;

    private List<IBehaviourWeightNode> children = new List<IBehaviourWeightNode>();

    public SelectorHighestWeight(string name)
    {
        this.name = name;
    }

    public override BehaviourTreeStatus Tick(TimeData time)
    {
        highestIndex = 0;
        highestWeight = children[highestIndex].GetWeight();
        for (childIndex = 1; childIndex < children.Count; childIndex++)
        {
            childWeight = children[childIndex].GetWeight();
            if (childWeight > highestWeight)
            {
                highestWeight = childWeight;
                highestIndex = childIndex;
            }
        }

        return children[highestIndex].Tick(time);
    }

    public override void AddChild<U>(U child)
    {
        children.Add(child);
    }
}
