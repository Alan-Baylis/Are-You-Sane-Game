/// <summary>
/// The interface implementation for ALL parent nodes on the tree, this needs to be 
/// </summary>
interface IParentBehaviour
{
    void AddChild<T>(T child) where T : IBehaviourTreeNode;
}

interface MParentBehviourNode : IParentBehaviour, IBehaviourTreeNode
{

}

interface MParentBehaviourWeighted : IParentBehaviour, IbehaviourWeightNode
{

}

interface IGenericParentBehaviour<T> where T : IParentBehaviour
{
    
}

interface IParentBehaviourWeighted<T> : IGenericParentBehaviour<MParentBehaviourWeighted>, MParentBehaviourWeighted where T : IBehaviourTreeNode
{
    new void AddChild<U>(U child) where U : T;
}

interface IParentBehaviourNode<T> : IGenericParentBehaviour<MParentBehviourNode>, MParentBehviourNode where T : IBehaviourTreeNode
{
    new void AddChild<U>(U child) where U : T;
}

public abstract class TestParentNodeToWeight : IParentBehaviourNode<IbehaviourWeightNode>
{
    public abstract void AddChild<U>(U child) where U : IBehaviourTreeNode;
    public abstract BehaviourTreeStatus Tick(TimeData time);
}



/// <summary>
/// The abstraction for all the parent nodes on the tree.
/// </summary>
/// <typeparam name="TChild"></typeparam>
public abstract class ParentBehaviour<TChild> : IParentBehaviour where TChild : IBehaviourTreeNode 
{
    private TChild ChildrenType { get; set; }
    public abstract void AddChild<U>(U child) where U : TChild;
    void IParentBehaviour.AddChild<T>(T child)
    {
        AddChild((TChild)(IBehaviourTreeNode)child);
    }
}

public abstract class ParentBehaviourNodeToWeight : ParentBehaviour<IBehaviourTreeNode>, IBehaviourTreeNode
{
    public abstract BehaviourTreeStatus Tick(TimeData time);
}

public abstract class ParentBehaivourWeightToNode : ParentBehaviour<IBehaviourTreeNode>, IbehaviourWeightNode
{
    public abstract BehaviourTreeStatus Tick(TimeData time);
    public abstract float GetWeight();
}

public abstract class ParentBehaviourWeightToWeight : ParentBehaviour<IbehaviourWeightNode>, IbehaviourWeightNode
{
    public abstract BehaviourTreeStatus Tick(TimeData time);
    public abstract float GetWeight();
}