// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MS.Utility;
using MS.Internal;

namespace System.Windows
{
    //
    // The list of Dependents that depend on a Source[ID]
    //
    // Steps are taken to guard against list corruption due to re-entrancy when
    // the Invalidation callbacks call Add / Remove.   But multi-threaded
    // access is not expected and so locks are not used.
    //
    internal class DependentList: MS.Utility.FrugalObjectList<Dependent>
    {
        public void Add(DependencyObject d, DependencyProperty dp, Expression expr)
        {
            // don't clean up every time.  This would make Add() cost O(N),
            // which would cause building a list to cost O(N^2).  yuck!
            // Clean the list less often the longer it gets.
            if(Count == Capacity)
                CleanUpDeadWeakReferences();

            Dependent dep = new Dependent(d, dp, expr);
            base.Add(dep);
        }

        public void Remove(DependencyObject d, DependencyProperty dp, Expression expr)
        {
            Dependent dep = new Dependent(d, dp, expr);
            base.Remove(dep);
        }

        public bool IsEmpty
        {
            get
            {
                for(int i = Count-1; i >= 0; i--)
                {
                    if(this[i].IsValid())
                    {
                        return false;
                    }
                }

                // there are no valid entries.   All callers immediately discard the
                // empty DependentList in this case, so there's no need to clean out
                // the list.  We can just GC collect the WeakReferences.
                return true;
            }
        }

        public void InvalidateDependents(DependencyObject source, DependencyPropertyChangedEventArgs sourceArgs)
        {
            // Take a snapshot of the list to protect against re-entrancy via Add / Remove.
            Dependent[] snapList = base.ToArray();

            for(int i=0; i<snapList.Length; i++)
            {
                Expression expression = snapList[i].Expr;
                if(null != expression)
                {
                    expression.OnPropertyInvalidation(source, sourceArgs);

                    // Invalidate dependent, unless expression did it already
                    if (!expression.ForwardsInvalidations)
                    {
                        DependencyObject dependencyObject = snapList[i].DO;
                        DependencyProperty dependencyProperty = snapList[i].DP;

                        if(null != dependencyObject && null != dependencyProperty)
                        {
                            // recompute expression
                            dependencyObject.InvalidateProperty(dependencyProperty);
                        }
                    }
                }
            }
        }

        private void CleanUpDeadWeakReferences()
        {
            int newCount = 0;

            // determine how many entries are valid
            for (int i=Count-1; i>=0; --i)
            {
                if (this[i].IsValid())
                {
                    ++ newCount;
                }
            }

            // if all the entries are valid, there's nothing to do
            if (newCount == Count)
                return;

            // compact the valid entries
            Compacter compacter = new Compacter(this, newCount);
            int runStart = 0;           // starting index of current run
            bool runIsValid = false;    // whether run contains valid or invalid entries

            for (int i=0, n=Count; i<n; ++i)
            {
                if (runIsValid != this[i].IsValid())    // run has ended
                {
                    if (runIsValid)
                    {
                        // emit a run of valid entries to the compacter
                        compacter.Include(runStart, i);
                    }

                    // start a new run
                    runStart = i;
                    runIsValid = !runIsValid;
                }
            }

            // emit the last run of valid entries
            if (runIsValid)
            {
                compacter.Include(runStart, Count);
            }

            // finish the job
            compacter.Finish();
        }
    }

    internal struct Dependent
    {
        private DependencyProperty _DP;
        private WeakReference _wrDO;
        private WeakReference _wrEX;

        public bool IsValid()
        {
            // Expression is never null (could Assert that but throw is fine)
            if(!_wrEX.IsAlive)
                return false;

            // It is OK to be null but if it isn't, then the target mustn't be dead.
            if(null != _wrDO && !_wrDO.IsAlive)
                return false;

            return true;
        }

        public Dependent(DependencyObject o, DependencyProperty p, Expression e)
        {
            _wrEX = (null == e) ? null : new WeakReference(e);
            _DP = p;
            _wrDO = (null == o) ? null : new WeakReference(o);
        }

        public DependencyObject DO
        {
            get
            {
                if(null == _wrDO)
                    return null;
                else
                    return (DependencyObject)_wrDO.Target;
            }
        }

        public DependencyProperty DP
        {
            get { return _DP; }
        }

        public Expression Expr
        {
            get
            {
                if(null == _wrEX)
                    return null;
                else
                    return (Expression)_wrEX.Target;
            }
        }

        override public bool Equals(object o)
        {
            if(! (o is Dependent))
                return false;

            Dependent d = (Dependent)o;

            // Not equal to Dead values.
            // This is assuming that at least one of the compared items is live.
            // This assumtion comes from knowing that Equal is used by FrugalList.Remove()
            // and if you look at DependentList.Remove()'s arguments, it can only
            // be passed strong references.
            // Therefore: Items being removed (thus compared here) will not be dead.
            if(!IsValid() || !d.IsValid())
                return false;

            if(_wrEX.Target != d._wrEX.Target)
                return false;

            if(_DP != d._DP)
                return false;

            // if they are both non-null then the Targets must match.
            if(null != _wrDO && null != d._wrDO)
            {
                if(_wrDO.Target != d._wrDO.Target)
                    return false;
            }
            // but only one is non-null then they are not equal
            else if(null != _wrDO || null != d._wrDO)
                return false;

            return true;
        }

        public static bool operator== (Dependent first, Dependent second)
        {
            return first.Equals(second);
        }

        public static bool operator!= (Dependent first, Dependent second)
        {
            return !(first.Equals(second));
        }

        // We don't expect to need this function. [Required when overriding Equals()]
        // Write a good HashCode anyway (if not a fast one)
        override public int GetHashCode()
        {
            int hashCode;
            Expression ex = (Expression)_wrEX.Target;
            hashCode = (null == ex) ? 0 : ex.GetHashCode();

            if(null != _wrDO)
            {
                DependencyObject DO = (DependencyObject)_wrDO.Target;
                hashCode += (null == DO) ? 0 : DO.GetHashCode();
            }

            hashCode += (null == _DP) ? 0 : _DP.GetHashCode();
            return hashCode;
        }
    }
}

