using System.Collections.Generic;


public class SequenceNode : IParentBehaviourTreeNode
{
    /// <summary>
    /// Name of the node.
    /// </summary>
    protected string name;

    /// <summary>
    /// List of child nodes.
    /// </summary>
    protected List<IBehaviourTreeNode> children = new List<IBehaviourTreeNode>(); //todo: this could be optimized as a baked array.

    public SequenceNode(string name)
    {
        this.name = name;
    }

    public virtual BehaviourTreeStatus Tick(TimeData time)
    {
        foreach (var child in children)
        {
            var childStatus = child.Tick(time);
            if (childStatus != BehaviourTreeStatus.Success) // Success will dictate that the action has finished
            {
                return childStatus;
            }
        }

        return BehaviourTreeStatus.Success;
    }

    /// <summary>
    /// Add a child to the sequence.
    /// </summary>
    public void AddChild(IBehaviourTreeNode child)
    {
        children.Add(child);
    }

}
