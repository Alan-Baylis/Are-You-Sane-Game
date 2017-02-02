using UnityEngine;
using System.Collections.Generic;

public class RandomSelectorNode : SelectorNode
{

    private List<IBehaviourTreeNode> randomTemp = new List<IBehaviourTreeNode>();

	public RandomSelectorNode(string name) : base(name)
    {
        this.name = name;
    }

    public override BehaviourTreeStatus Tick(TimeData time)
    {
        randomTemp.Clear();
        randomTemp.AddRange(children);
        foreach (var child in children)
        {
            int randomIndex = Random.Range(0, randomTemp.Count);
            var childStatus = randomTemp[randomIndex].Tick(time);
            randomTemp.Remove(randomTemp[randomIndex]);
            if (childStatus != BehaviourTreeStatus.Failure)
            {
                return childStatus;
            }
        }

        return BehaviourTreeStatus.Failure;
    }

}
