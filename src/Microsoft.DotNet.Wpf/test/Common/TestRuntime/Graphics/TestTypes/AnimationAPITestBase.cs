// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

using Microsoft.Test.Graphics.Factories;

using BindingFlags = System.Reflection.BindingFlags;

#if !STANDALONE_BUILD
using TrustedActivator = Microsoft.Test.Security.Wrappers.ActivatorSW;
using TrustedAssembly = Microsoft.Test.Security.Wrappers.AssemblySW;
using TrustedType = Microsoft.Test.Security.Wrappers.TypeSW;
#else
using TrustedActivator = System.Activator;
using TrustedAssembly = System.Reflection.Assembly;
using TrustedType = System.Type;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Base class for animation api testing
    /// </summary>

    public abstract class AnimationAPITestBase : RenderingTest
    {
        private const int sleepTime = 1000;


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public override void Init(Variation v)
        {
            base.Init(v);
            v.AssertExistenceOf("ClassName", "Property", "From", "To", "DefaultValue", "TestType");

            testType = (TestType)Enum.Parse(typeof(TestType), v["TestType"]);
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        protected abstract IAnimatable GetAnimatingObject(string property, string propertyOwner);


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        protected void PrepareAnimation(string className, string propertyName, string from, string to, string defaultValue)
        {
            animatingObject = GetAnimatingObject(propertyName, className);
            property = GetAnimatingProperty(propertyName);

            if (property.PropertyType.IsValueType)
            {
                this.from = MakeValue(from, property.PropertyType);
                this.to = MakeValue(to, property.PropertyType);
                this.defaultValue = MakeValue(defaultValue, property.PropertyType);
            }
            else
            {
                this.from = MakeObject(from);
                this.to = MakeObject(to);
                this.defaultValue = MakeObject(defaultValue);
            }

            // The FactoryParser already set this value.  Clear it!
            ((DependencyObject)animatingObject).ClearValue(property);
            AnimationTimeline animation = MakeAnimation(variation, property.PropertyType);
            animationClock = animation.CreateClock();

            switch (testType)
            {
                case TestType.AnimationFirst:
                case TestType.AnimationFirstAndClearFifo:
                case TestType.AnimationFirstAndClearLifo:
                    animatingObject.ApplyAnimationClock(property, animationClock);
                    ((DependencyObject)animatingObject).SetValue(property, this.defaultValue);
                    break;

                case TestType.BaseValueFirst:
                case TestType.BaseValueFirstAndClearFifo:
                case TestType.BaseValueFirstAndClearLifo:
                    ((DependencyObject)animatingObject).SetValue(property, this.defaultValue);
                    animatingObject.ApplyAnimationClock(property, animationClock);
                    break;

                case TestType.NoBaseValue:
                case TestType.NoBaseValueAndClearValue:
                    animatingObject.ApplyAnimationClock(property, animationClock);
                    break;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public AnimationTimeline MakeAnimation(Variation v, Type baseType)
        {
            v.AssertExistenceOf("From", "To", "DurationSeconds");

            Type animationType = GetAnimationType(baseType);
            object[] args = GetAnimationConstructorArgs(v, baseType);

            AnimationTimeline timeline = (AnimationTimeline)TrustedActivator.CreateInstance(animationType, args);
            timeline.RepeatBehavior = RepeatBehavior.Forever;

            return timeline;
        }

        private Type GetAnimationType(Type baseType)
        {
            if (baseType.IsValueType)
            {
                if (baseType.Name == "Matrix3D")
                {
                    // Matrix3DAnimation does not exist in Avalon, so use the one that we created
                    return typeof(Matrix3DAnimation);
                }
                else
                {
                    TrustedAssembly presentationCore = TrustedAssembly.GetAssembly(typeof(Animatable));
                    return PT.Untrust(presentationCore.GetType("System.Windows.Media.Animation." + baseType.Name + "Animation"));
                }
            }
            else
            {
                // Reference-type animations do not exist in Avalon, so use the one that we created
                return typeof(DiscreteAnimation);
            }
        }

        private object[] GetAnimationConstructorArgs(Variation v, Type baseType)
        {
            string from = v["From"];
            string to = v["To"];
            string seconds = v["DurationSeconds"];

            Duration duration = TimeSpan.FromSeconds(StringConverter.ToDouble(seconds));

            if (baseType.IsValueType)
            {
                return new object[] { MakeValue(from, baseType), MakeValue(to, baseType), duration };
            }
            else
            {
                return new object[] { MakeObject(from), MakeObject(to), duration };
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public object MakeValue(string value, Type type)
        {
            TrustedType t = PT.Trust(typeof(StringConverter));
            return t.InvokeMember("To" + type.Name, BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null, new object[] { value });
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public object MakeObject(string value)
        {
            return ObjectFactory.MakeObject(value);
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        protected IAnimatable GetAnimatable(string complexPropertyPath, object attachedTo)
        {
            return (IAnimatable)ObjectUtils.GetPropertyOwner(complexPropertyPath, attachedTo);
        }

        private DependencyProperty GetAnimatingProperty(string complexPropertyPath)
        {
            string propertyName = ObjectUtils.GetLastPropertyName(complexPropertyPath);
            return ObjectUtils.GetDependencyProperty(propertyName, animatingObject.GetType());
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public override void RunTheTest()
        {
            RenderWindowContent();

            pass = 0;

            // Give the animations a second to start so we don't have serious timing issues
            Thread.Sleep(sleepTime);
            ModifyWindowContent();

            Thread.Sleep(sleepTime);
            ModifyWindowContent();

            Thread.Sleep(sleepTime);
            ModifyWindowContent();

            Thread.Sleep(sleepTime);
            ModifyWindowContent();
        }


        /// <summary/>
        public override void ModifyWindowContent(Visual v)
        {
            // We actually don't modify anything.
            // We just use the callback to check the current status of our scene

            switch (pass++)
            {
                case 0:
                    Log("Pass 0, verifying that the animation has started");
                    VerifyIsAnimating();
                    break;

                case 1:
                    Log("Pass 1, verifying that the animation has started");
                    VerifyIsAnimating();
                    break;

                case 2:
                    Log("Pass 2, removing the animation from the object");
                    StopAnimating();
                    break;

                case 3:
                    Log("Pass 3, verifying that the animation has stopped");
                    VerifyAnimationStopped();
                    break;
            }
        }

        private object GetCurrentValue(AnimationClock a)
        {
            return (object)a.GetCurrentValue(from, to);
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        protected void VerifyIsAnimating()
        {
            // Check to make sure that the current value is not
            //  the default value specified by the test script

            object currentValue = GetCurrentValue(animationClock);
            if (currentValue.Equals(defaultValue))
            {
                AddFailure("GetCurrentValue failed");
                Log("*** Expected: Value between {0} - {1}", from, to);
                Log("***   Actual: {0}", currentValue.ToString());
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        protected void StopAnimating()
        {
            switch (testType)
            {
                case TestType.AnimationFirstAndClearFifo:
                case TestType.BaseValueFirstAndClearLifo:
                case TestType.NoBaseValueAndClearValue:
                    animationClock.Controller.Stop();
                    animatingObject.ApplyAnimationClock(property, null);
                    ((DependencyObject)animatingObject).ClearValue(property);
                    break;

                case TestType.AnimationFirstAndClearLifo:
                case TestType.BaseValueFirstAndClearFifo:
                    ((DependencyObject)animatingObject).ClearValue(property);
                    animatingObject.ApplyAnimationClock(property, null);
                    animationClock.Controller.Stop();
                    break;

                default:
                    animatingObject.ApplyAnimationClock(property, null);
                    animationClock.Controller.Stop();
                    break;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        protected void VerifyAnimationStopped()
        {
            // When the animation stops, its current value
            //  should be the default value specified by the test script

            object currentValue = ((DependencyObject)animatingObject).GetValue(property);
            if (IsAnimating(currentValue))
            {
                AddFailure("GetCurrentValue failed");
                Log("*** Expected: {0}", property.DefaultMetadata.DefaultValue);
                Log("***   Actual: {0}", currentValue.ToString());
            }
        }

        private bool IsAnimating(object currentValue)
        {
            switch (testType)
            {
                case TestType.AnimationFirst:
                case TestType.BaseValueFirst:
                    return !ObjectUtils.Equals(currentValue, defaultValue);

                case TestType.AnimationFirstAndClearFifo:
                case TestType.AnimationFirstAndClearLifo:
                case TestType.BaseValueFirstAndClearFifo:
                case TestType.BaseValueFirstAndClearLifo:
                case TestType.NoBaseValue:
                case TestType.NoBaseValueAndClearValue:
                    return !ObjectUtils.DeepEqualsToAnimatable(currentValue, property.DefaultMetadata.DefaultValue);
            }

            throw new ApplicationException("IsAnimating- Invalid test type specified");
        }

        /// <summary/>
        public sealed override void Verify()
        {
            // Do nothing.  This is an abstract method that must be overridden.
        }

        private enum TestType
        {
            AnimationFirst,
            AnimationFirstAndClearFifo,
            AnimationFirstAndClearLifo,
            BaseValueFirst,
            BaseValueFirstAndClearFifo,
            BaseValueFirstAndClearLifo,
            NoBaseValue,
            NoBaseValueAndClearValue,
        }

        private object defaultValue;
        private object from;
        private object to;
        private int pass;
        private IAnimatable animatingObject;
        private TestType testType;
        private DependencyProperty property;
        private AnimationClock animationClock;
    }
}
