using UnityEngine;
using System.Collections.Generic;

public class RandomSequenceNode : SequenceNode
{
    private List<IBehaviourTreeNode> randomTemp = new List<IBehaviourTreeNode>();

	public RandomSequenceNode(string name) : base(name)
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

            if (childStatus != BehaviourTreeStatus.Success) // Success will dictate that the action has finished
            {
                return childStatus;
            }
        }

        return BehaviourTreeStatus.Success;
    }
}
