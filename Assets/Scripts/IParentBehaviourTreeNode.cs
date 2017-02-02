
/// <summary>
/// Interface for behaviour tree nodes.
/// </summary>
public interface IParentBehaviourTreeNode : IBehaviourTreeNode
{
    /// <summary>
    /// Add a child to the parent node.
    /// </summary>
    void AddChild(IBehaviourTreeNode child);    
}

public interface IParentBehaviourNodeToWeight : IParentBehaviourTreeNode // No weight
{
    void AddChild(IBehaviourWeightNode weightedChild); // Weighted
}

public interface IParentBehaviourWeightToNode : IParentBehaviourTreeNode, IWeight// Weighted
{
    //void AddChild(IBehaviourTreeNode child); // No weight
}

public interface IParentBehaviourWeightToWeight : IParentBehaviourTreeNode, IWeight // Weighted
{
    void AddChild(IBehaviourWeightNode child); // Weighted
}
