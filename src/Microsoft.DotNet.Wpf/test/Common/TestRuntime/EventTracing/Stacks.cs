// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.EventTracing;
using System.Collections.Generic;
using System.Text;

public enum StackIndex { };

public sealed class TraceStacks
{
    static StackIndex StackIndexForEvent(StackWalkTraceEvent data)
    {
        return (StackIndex)0;
    }

    TraceStack this[StackIndex id] { get { return frames[(int)id]; } }

    GrowableArray<TraceStack> frames;
}

[CLSCompliant(false)]
public sealed class TraceStack
{
    public TraceStack caller;
    public Address address;
}

/// <summary>
/// Absraction for rolling up by Location.  A Location is simply something that has a name (but may have more
/// information tacked on to it). 
/// </summary>
public interface ILocation
{
    string Name { get; }
}

public sealed class SampleStack
{
    public ILocation Location;
    public SampleStack Caller;
}

public interface ISample
{
    float metric { get; }              // The 'weight' of the sample
    SampleStack stack { get; }         // from callee to Caller.  
}

/// <summary>
/// Rollups of a set of samples by stack.  This represents the entire call tree.   You create an empty one in using
/// the default constructor and use 'AddSample' to add samples to it.   You traverse it by 
/// </summary>
public sealed class CallTree
{
    public CallTree()
    {
        // TODO should we give the top node a real 'Location'.
        top = new CallTreeNode(null, null);
    }
    public void AddSample(ISample sample)
    {
        // Find the bottom-most node, updating the inclusive times along the way.  
        CallTreeNode bottom = top.UpdateInclusive(sample.metric, sample.stack);
        bottom.exclusiveCount++;
        bottom.exclusiveMetric += sample.metric;
    }

    public CallTreeNode Top { get { return top; } }
    // A pre-order (callers before callees), traversal of the call tree. Note that doing this using explicit
    // recurision is more efficient if you are on a perf-critical path.
    IEnumerable<CallTreeNode> AllNodes { get { return Top.AllNodes; } }
    public override string ToString()
    {
        return "<CallTree>\r\n" +
            Top.ToString() + "\r\n" +
            "</CallTree>";
    }
    #region private
    private CallTreeNode top;
    #endregion
}

/// <summary>
/// The part of a calltreeNode that is common to Caller-callee and the calltree view.  
/// </summary>
public class CallTreeBaseNode
{
    internal CallTreeBaseNode(ILocation location)
    {
        this.location = location;
    }

    public ILocation Location { get { return location; } }
    public float InclusiveMetric { get { return inclusiveMetric; } }
    public float ExclusiveMetric { get { return exclusiveMetric; } }
    public float InclusiveCount { get { return inclusiveCount; } }
    public float ExclusiveCount { get { return inclusiveCount; } }
    public override string ToString()
    {
        return "<CallTreeBase " +
              "InclusiveMetric=" + XmlUtilities.XmlQuote(InclusiveMetric) + " " +
              "InclusiveCount=" + XmlUtilities.XmlQuote(InclusiveCount) + " " +
              "ExclusiveMetric=" + XmlUtilities.XmlQuote(ExclusiveMetric) + " " +
              "ExclusiveCount=" + XmlUtilities.XmlQuote(ExclusiveCount) + ">\r\n" +
              "  " + Location.ToString() + "\r\n" +
            "</CallTreeBase>";
    }
    #region private
    internal readonly ILocation location;
    internal float inclusiveMetric;
    internal float exclusiveMetric;
    internal float inclusiveCount;
    internal float exclusiveCount;
    #endregion
}

/// <summary>
/// Represents a single node in a code:CallTree 
/// 
/// TODO should a sort the Callees by name, or inclusive metric?
/// </summary>
public sealed class CallTreeNode : CallTreeBaseNode
{
    public CallTreeNode Caller { get { return caller; } }
    public int CalleesCount { get { return callees.Count; } }
    public IEnumerable<CallTreeNode> Callees { get { return callees; } }
    // TODO not sure I should provide these, as there are too many possible combinations.  
    // Probably better done by reflection in the GUI, which would would regular.   Still they are useful for
    // now. 
    public void SortCalleesByInclusiveMetricDecending()
    {
        callees.Sort(delegate(CallTreeNode x, CallTreeNode y)
        {
            return (y.InclusiveMetric.CompareTo(x.InclusiveMetric));
        });
    }
    public void SortCalleesByNameAscending()
    {
        callees.Sort(delegate(CallTreeNode x, CallTreeNode y)
        {
            return (x.Location.Name.CompareTo(y.Location.Name));
        });
    }

    /// <summary>
    /// Enumerates this tree node and all its callees recursively in pre-order (parents before children).
    /// This is a convinience function. It is not as efficient as doing the recursive walk explicitly.
    /// </summary>
    public IEnumerable<CallTreeNode> AllNodes
    {
        get
        {
            // return myself. 
            yield return this;

            // return the nodes of my children.  
            for (int i = 0; i < callees.Count; i++)
                foreach (CallTreeNode node in callees[i].AllNodes)
                    yield return node;
        }
    }
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<CallTree Name=\"").Append(location == null ? "" : location.Name);
        sb.Append(" InclusiveCount=" + XmlUtilities.XmlQuote(InclusiveCount));
        sb.Append(" InclusiveMetric=" + XmlUtilities.XmlQuote(InclusiveMetric));
        sb.Append(" ExclusiveCount=" + XmlUtilities.XmlQuote(ExclusiveCount));
        sb.Append(" ExclusiveMetric=" + XmlUtilities.XmlQuote(ExclusiveMetric));
        sb.Append(">").AppendLine();
        foreach (CallTreeBaseNode callee in Callees)
            sb.AppendLine(callee.ToString());
        sb.AppendLine("<CallTree>");
        return sb.ToString();
    }
    #region private
    internal CallTreeNode(ILocation location, CallTreeNode caller)
        : base(location)
    {
        this.caller = caller;
    }
    internal CallTreeNode UpdateInclusive(float metric, SampleStack stack)
    {
        CallTreeNode callerNode = this;
        SampleStack callerStack = stack.Caller;
        if (callerStack != null)
            callerNode = UpdateInclusive(metric, callerStack);

        callerNode.inclusiveCount++;
        callerNode.inclusiveMetric += metric;

        CallTreeNode node = FindCallee(stack.Location);
        callees.Add(node);
        return node;
    }
    private CallTreeNode FindCallee(ILocation location)
    {
        CallTreeNode callee;
        for (int i = 0; i < callees.Count; i++)
        {
            callee = callees[i];
            if (callee.Location == location)
                return callee;
        }
        callee = new CallTreeNode(location, this);
        callees.Add(callee);
        return callee;
    }

    // state;
    private readonly CallTreeNode caller;
    internal GrowableArray<CallTreeNode> callees;
    #endregion
}

/// <summary>
/// A code:CallerCalleeNode gives statistics that focus on a particular location (method, module, or other
/// grouping).   It takes all samples that have stacks that include that node and compute the metrics for
/// all the callers and all the callees for that node.  
/// </summary>
class CallerCalleeNode : CallTreeBaseNode
{
    /// <summary>
    /// Given a complete call tree, and a Location within that call tree to focus on, create a
    /// CallerCalleeNode that represents the single Caller-Callee view for that node. 
    /// </summary>
    public CallerCalleeNode(CallTree callTree, ILocation location)
        : base(location)
    {
        float totalMetric;
        float totalCount;
        AccumlateSamplesForNode(callTree.Top, 0, out totalMetric, out totalCount);
    }

    public IEnumerable<CallTreeBaseNode> Callers { get { return callers; } }
    public IEnumerable<CallTreeBaseNode> Callees { get { return callees; } }
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<CallerCallee Name=").Append(XmlUtilities.XmlQuote(location.Name)).Append(">").AppendLine();
        sb.AppendLine("  <Callers>");
        foreach (CallTreeBaseNode caller in Callers)
            sb.Append("    ").AppendLine(caller.ToString());
        sb.AppendLine("  </Callers>");
        sb.AppendLine("  <Callees>");
        foreach (CallTreeBaseNode callees in Callees)
            sb.Append("    ").AppendLine(callees.ToString());
        sb.AppendLine("  </Callees>");
        sb.Append("</CallerCallee>");
        return sb.ToString();
    }
    #region private
    /// <summary>
    /// Accumlate all the samples represented by 'node' and all its children into the current CallerCalleeNode
    /// represention for 'this.Location'   'recursionCount' is the number of times 'this.Location' has been
    /// seen as Caller of 'node'.  
    /// 
    /// TODO explain about splitting samples.  
    /// </summary>
    private void AccumlateSamplesForNode(CallTreeNode node, int recursionCount, out float inclusiveMetricRet, out float inclusiveCountRet)
    {
        inclusiveMetricRet = 0;
        inclusiveCountRet = 0;
        bool isFocusNode = (location != null && node.Location.Equals(location));
        if (isFocusNode)
            recursionCount++;

        if (recursionCount > 0)
        {
            // Compute exclusive count and metric (and initialize the inclusive count and metric). 
            inclusiveCountRet = node.ExclusiveMetric / recursionCount;
            inclusiveMetricRet = node.ExclusiveMetric / recursionCount;
            if (isFocusNode)
            {
                exclusiveCount += inclusiveCountRet;
                exclusiveMetric += inclusiveMetricRet;
            }
        }

        // Get all the samples for the children 
        for (int i = 0; i < node.callees.Count; i++)
        {
            CallTreeNode calleeNode = node.callees[i];

            float calleeInclusiveMetric;
            float calleeInclusiveCount;
            AccumlateSamplesForNode(calleeNode, recursionCount, out calleeInclusiveMetric, out calleeInclusiveCount);

            if (calleeInclusiveCount > 0)       // This condition is an optimization (avoid lookup)
            {
                inclusiveCountRet += calleeInclusiveCount;
                inclusiveMetricRet += calleeInclusiveMetric;
                if (isFocusNode)
                {
                    CallTreeBaseNode callee = Find(ref callees, calleeNode.location);
                    callee.inclusiveCount += calleeInclusiveCount;
                    callee.inclusiveMetric += calleeInclusiveMetric;

                    callee.exclusiveCount += calleeNode.ExclusiveCount;
                    callee.exclusiveMetric += calleeNode.exclusiveMetric;
                }
            }
        }

        // Set the Caller information now 
        if (isFocusNode && recursionCount > 0)
        {
            CallTreeNode callerNode = node.Caller;
            CallTreeBaseNode caller = Find(ref callers, callerNode.location);
            caller.exclusiveCount += node.Caller.ExclusiveCount;
            caller.exclusiveMetric += node.Caller.ExclusiveMetric;
            caller.inclusiveCount += inclusiveCountRet;
            caller.inclusiveMetric += inclusiveMetricRet;
        }
    }

    private CallTreeBaseNode Find(ref GrowableArray<CallTreeBaseNode> elems, ILocation location)
    {
        CallTreeBaseNode elem;
        for (int i = 0; i < elems.Count; i++)
        {
            elem = elems[i];
            if (elem.Location == location)
                return elem;
        }
        elem = new CallTreeBaseNode(location);
        elems.Add(elem);
        return elem;
    }

    // state;
    private GrowableArray<CallTreeBaseNode> callers;
    private GrowableArray<CallTreeBaseNode> callees;
    #endregion
}