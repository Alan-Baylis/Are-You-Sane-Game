/// <summary>
/// The interface implementation for ALL parent nodes on the tree, this needs to be 
/// </summary>
interface IParentBehaviour : IBehaviourTreeNode
{
    void AddChild<T>(T child) where T : IBehaviourTreeNode;
}

/// <summary>
/// The abstraction for all the parent nodes on the tree.
/// </summary>
/// <typeparam name="TChild"></typeparam>
public  abstract class ParentBehaviour<TChild> : IParentBehaviour where TChild : IBehaviourTreeNode 
{
    private TChild ChildrenType { get; set; }
    public abstract BehaviourTreeStatus Tick(TimeData time);
    public abstract void AddChild<U>(U child) where U : TChild;
    void IParentBehaviour.AddChild<T>(T child)
    {
        AddChild((TChild)(IBehaviourTreeNode)child);
    }
}

/// <summary>
/// Specific parent nodes for holding weights will have to re-assign the the IbehaviourWeightNode interface as the reference cannot be held through generic forms.
/// Regular IBehaviourNode iterfaces are fine however.
/// </summary>
/*
public abstract class ParentBehaviourNodeToNode : ParentBehaviour<IBehaviourTreeNode>

public abstract class ParentBehaviourNodeToWeight : ParentBehaviour<IBehaviourWeightNode>

public abstract class ParentBehaivourWeightToNode : ParentBehaviour<IBehaviourTreeNode>, IBehaviourWeightNode
{
    public abstract float GetWeight();
}

public abstract class ParentBehaviourWeightToWeight : ParentBehaviour<IBehaviourWeightNode>, IBehaviourWeightNode
{
    public abstract float GetWeight();
}
*/