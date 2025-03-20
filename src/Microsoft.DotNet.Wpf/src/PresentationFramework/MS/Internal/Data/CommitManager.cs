// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Media;
using System.Windows.Data;
using System.Windows;

namespace MS.Internal.Data;

/// <summary>
/// <see cref="CommitManager"/> provides global services for committing dirty bindings.
/// </summary>
internal sealed class CommitManager
{
    private readonly HashSet<BindingGroup> _bindingGroups = new();
    private readonly HashSet<BindingExpressionBase> _bindings = new();

    private static readonly List<BindingGroup> s_emptyBindingGroupList = new();
    private static readonly List<BindingExpressionBase> s_emptyBindingList = new();

    internal bool IsEmpty
    {
        get => _bindings.Count == 0 && _bindingGroups.Count == 0;
    }

    internal void AddBindingGroup(BindingGroup bindingGroup)
    {
        _bindingGroups.Add(bindingGroup);
    }

    internal void RemoveBindingGroup(BindingGroup bindingGroup)
    {
        _bindingGroups.Remove(bindingGroup);
    }

    internal void AddBinding(BindingExpressionBase binding)
    {
        _bindings.Add(binding);
    }

    internal void RemoveBinding(BindingExpressionBase binding)
    {
        _bindings.Remove(binding);
    }

    internal List<BindingGroup> GetBindingGroupsInScope(DependencyObject element)
    {
        // iterate over a copy of the full list - callouts can change the original list
        BindingGroup[] fullList = new BindingGroup[_bindingGroups.Count];
        _bindingGroups.CopyTo(fullList);

        List<BindingGroup> list = s_emptyBindingGroupList;

        foreach (BindingGroup bindingGroup in fullList)
        {
            DependencyObject owner = bindingGroup.Owner;
            if (owner is not null && IsInScope(element, owner))
            {
                if (list == s_emptyBindingGroupList)
                {
                    list = new List<BindingGroup>();
                }

                list.Add(bindingGroup);
            }
        }

        return list;
    }

    internal List<BindingExpressionBase> GetBindingsInScope(DependencyObject element)
    {
        // iterate over a copy of the full list - calling TargetElement can change the original list
        BindingExpressionBase[] fullList = new BindingExpressionBase[_bindings.Count];
        _bindings.CopyTo(fullList);

        List<BindingExpressionBase> list = s_emptyBindingList;

        foreach (BindingExpressionBase binding in fullList)
        {
            DependencyObject owner = binding.TargetElement;
            if (owner is not null && binding.IsEligibleForCommit && IsInScope(element, owner))
            {
                if (list == s_emptyBindingList)
                {
                    list = new List<BindingExpressionBase>();
                }

                list.Add(binding);
            }
        }

        return list;
    }

    // remove stale entries
    internal bool Purge()
    {
        int count = _bindings.Count;
        if (count > 0)
        {
            BindingExpressionBase[] list = new BindingExpressionBase[_bindings.Count];
            _bindings.CopyTo(list);

            foreach (BindingExpressionBase binding in list)
            {
                // fetching TargetElement may detach the binding, removing it from _bindings
                _ = binding.TargetElement;
            }
        }

        bool foundDirt = _bindings.Count < count;

        count = _bindingGroups.Count;
        if (count > 0)
        {
            BindingGroup[] list = new BindingGroup[_bindingGroups.Count];
            _bindingGroups.CopyTo(list);

            foreach (BindingGroup bindingGroup in list)
            {
                // fetching Owner may detach the binding group, removing it from _bindingGroups
                _ = bindingGroup.Owner;
            }
        }

        return foundDirt || (_bindingGroups.Count < count);
    }

    // return true if element is a descendant of ancestor
    private static bool IsInScope(DependencyObject ancestor, DependencyObject element)
    {
        return ancestor is null || VisualTreeHelper.IsAncestorOf(ancestor, element);
    }
}

