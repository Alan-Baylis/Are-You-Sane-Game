/// <summary>
/// The interface implementation for ALL parent nodes on the tree
/// </summary>
interface IParentBehaviour : IBehaviourTreeNode
{
    void AddChild<T>(T child) where T : IBehaviourTreeNode;
}

/// <summary>
/// The abstraction for all the parent nodes on the tree.
/// </summary>
/// <typeparam name="ChildType"></typeparam>
public abstract class ParentBehaviourNode<ChildType> : IParentBehaviour where ChildType : IBehaviourTreeNode 
{
    private ChildType ChildrenType { get; set; }
    public abstract BehaviourTreeStatus Tick(TimeData time);
    public abstract void AddChild<U>(U child) where U : ChildType;
    void IParentBehaviour.AddChild<T>(T child)
    {
        AddChild((ChildType)(IBehaviourTreeNode)child);
    }
}



public abstract class ParentBehaviourWeighted : ParentBehaviourNode<IbehaviourWeightNode>
{
    public abstract float GetWeight();
}