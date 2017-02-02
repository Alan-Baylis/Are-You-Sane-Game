using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectorLowestWeight
{
    // : IParentBehaviourNodeToWeight
    private string name;
    private int childIndex;
    private int lowestIndex;
    private float childWeight;
    private float lowestWeight;

    private List<IBehaviourWeightNode> children = new List<IBehaviourWeightNode>();

    public SelectorLowestWeight(string name)
    {
        this.name = name;
    }

    public BehaviourTreeStatus Tick(TimeData time)
    {
        lowestIndex = 0;
        lowestWeight = children[lowestIndex].GetWeight();
        for (childIndex = 1; childIndex < children.Count; childIndex++)
        {
            childWeight = children[childIndex].GetWeight();
            if (childWeight < lowestWeight)
            {
                lowestWeight = childWeight;
                lowestIndex = childIndex;
            }
        }

        return children[lowestIndex].Tick(time);
    }

    public void AddChild(IBehaviourWeightNode child)
    {
        children.Add(child);
    }
}
