using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Fluent API for building a behaviour tree.
/// </summary>
public class BehaviourTreeBuilder
{
    /// <summary>
    /// Last node created.
    /// </summary>
    private IBehaviourTreeNode curNode = null;

    /// <summary>
    /// Stack node nodes that we are build via the fluent API.
    /// </summary>
    private Stack<IParentBehaviour> parentNodeStack = new Stack<IParentBehaviour>();

    /********************************* ACTION NODE ******************************************/

    /// <summary>
    /// Create an action node.
    /// </summary>
    public BehaviourTreeBuilder Do(string name, Func<TimeData, BehaviourTreeStatus> fn)
    {
        if (parentNodeStack.Count <= 0)
        {
            Debug.LogError("Can't create an unnested ActionNode, it must be a leaf node.");
        }

        var actionNode = new ActionNode(name, fn);
        parentNodeStack.Peek().AddChild(actionNode);
        return this;
    }

    /********************************* UTILITY NODE TYPES ******************************************/

    /// <summary>
    /// Like an action node... but the function can return true/false and is mapped to success/failure.
    /// </summary>
    public BehaviourTreeBuilder Condition(string name, Func<TimeData, bool> fn)
    {
        return Do(name, t => fn(t) ? BehaviourTreeStatus.Success : BehaviourTreeStatus.Failure);
    }

    /// <summary>
    /// Splice a sub tree into the parent tree.
    /// </summary>
    public BehaviourTreeBuilder Splice(IBehaviourTreeNode subTree)
    {
        if (subTree == null)
        {
            Debug.LogError("Sub-Tree is NULL and cannot combine with the tree");
        }

        if (parentNodeStack.Count <= 0)
        {
            Debug.LogError("Can't splice an unnested sub-tree, there must be a parent-tree.");
        }

        parentNodeStack.Peek().AddChild(subTree);
        return this;
    }


    /********************************* PARENT NODE TYPES ******************************************/

    /// <summary>
    /// Create an inverter node that inverts the success/failure of its children. Currently this Node is LIMITED to ONE child.
    /// </summary>
    public BehaviourTreeBuilder Inverter(string name)
    {
        var inverterNode = new InverterNode(name);
        AddParentToTop(inverterNode);
        return this;
    }

    public BehaviourTreeBuilder Repeater(string name, Func<int, bool> repeater)
    {
        var repeaterNode = new RepeaterNode(name, repeater);
        AddParentToTop(repeaterNode);
        return this;
    }

    public BehaviourTreeBuilder RepeatUntilFail(string name, Func<int, bool> repeater)
    {
        var repeatUntilFail = new UntilFailNode(name, repeater);
        AddParentToTop(repeatUntilFail);
        return this;
    }

    /// <summary>
    /// Create a sequence node.
    /// </summary>
    public BehaviourTreeBuilder Sequence(string name)
    {
        var sequenceNode = new SequenceNode(name);
        AddParentToTop(sequenceNode);
        return this;
    }

    public BehaviourTreeBuilder RandomSequence(string name)
    {
        var sequenceNode = new RandomSequenceNode(name);
        AddParentToTop(sequenceNode);
        return this;
    }

    /// <summary>
    /// Create a parallel node.
    /// </summary>
    public BehaviourTreeBuilder Parallel(string name, int numRequiredToFail, int numRequiredToSucceed) // This applies depth search
    {
        var parallelNode = new ParallelNode(name, numRequiredToFail, numRequiredToSucceed);
        AddParentToTop(parallelNode);
        return this;
    }

    /// <summary>
    /// Create a selector node.
    /// </summary>
    public BehaviourTreeBuilder Selector(string name)
    {
        var selectorNode = new SelectorNode(name);
        AddParentToTop(selectorNode);
        return this;
    }

    public BehaviourTreeBuilder RandomSelector(string name)
    {
        var selectorNode = new RandomSelectorNode(name);
        AddParentToTop(selectorNode);
        return this;
    }

    /******************************** WEIGHTED NODE INVOLVED ****************************************/


    public BehaviourTreeBuilder SelectorLowestWeight(string name)
    {
        var selector = new SelectorLowestWeight(name);
        AddParentToTop(selector);
        return this;
    }




    /********************************* TREE FUNCTIONALITY ******************************************/

    /// <summary>
    /// Build the actual tree.
    /// </summary>
    public IBehaviourTreeNode Build()
    {
        if (curNode == null)
        {
            Debug.LogError("Can't create a behaviour tree with zero nodes");
        }

        return curNode;
    }

    /// <summary>
    /// Ends a sequence of children.
    /// </summary>
    public BehaviourTreeBuilder End()
    {
        curNode = parentNodeStack.Pop();
        return this;
    }

    /// <summary>
    /// Function to add a recently create parent node to the top of the tree
    /// </summary>
    /// <param name="node"></param>
    private void AddParentToTop(IParentBehaviour node)
    {
        if (parentNodeStack.Count > 0)
        {
            parentNodeStack.Peek().AddChild(node);
        }

        parentNodeStack.Push(node);
    }

}
