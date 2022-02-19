using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Windows.Input
{
    internal class LowEventManager
    {
        private struct Subscription
        {
            public readonly WeakReference Subscriber;

            public readonly MethodInfo Handler;

            public Subscription(WeakReference subscriber, MethodInfo handler)
            {
                Subscriber = subscriber;
                Handler = (handler ?? throw new ArgumentNullException("handler"));
            }
        }

        private readonly Dictionary<string, List<Subscription>> _eventHandlers = new Dictionary<string, List<Subscription>>();

        /// <summary>
        /// To be added.
        /// </summary>
        /// <typeparam name="TEventArgs">To be added.</typeparam>
        /// <param name="handler">To be added.</param>
        /// <param name="eventName">To be added.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks>To be added.</remarks>
#pragma warning disable CS8604
        public void AddEventHandler<TEventArgs>(EventHandler<TEventArgs> handler, [CallerMemberName] string eventName = "") where TEventArgs : EventArgs
        {
            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            AddEventHandler(eventName, handler.Target, handler.GetMethodInfo());
        }


        /// <summary>
        ///  To be added.
        /// </summary>
        /// <param name="handler"> To be added.</param>
        /// <param name="eventName"> To be added.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks> To be added.</remarks>
        public void AddEventHandler(EventHandler handler, [CallerMemberName] string eventName = "")
        {
            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            AddEventHandler(eventName, handler.Target, handler.GetMethodInfo());
        }

        /// <summary>
        /// To be added.
        /// </summary>
        /// <param name="sender">To be added.</param>
        /// <param name="args">To be added.param>
        /// <param name="eventName">To be added.</param>
        /// <remarks>
        /// To be added.
        /// </remars>
#pragma warning disable CS8600
#pragma warning disable CS8620
        public void HandleEvent(object sender, object args, string eventName)
        {
            List<(object, MethodInfo)> list = new List<(object, MethodInfo)>();
            List<Subscription> list2 = new List<Subscription>();
            if (_eventHandlers.TryGetValue(eventName, out List<Subscription> value))
            {
                for (int i = 0; i < value.Count; i++)
                {
                    Subscription item = value[i];
                    if (item.Subscriber == null)
                    {
                        list.Add((null, item.Handler));
                        continue;
                    }

                    object target = item.Subscriber.Target;
                    if (target == null)
                    {
                        list2.Add(item);
                    }
                    else
                    {
                        list.Add((target, item.Handler));
                    }
                }

                for (int j = 0; j < list2.Count; j++)
                {
                    Subscription item2 = list2[j];
                    value.Remove(item2);
                }
            }

            for (int k = 0; k < list.Count; k++)
            {
                (object, MethodInfo) tuple = list[k];
                object item3 = tuple.Item1;
                tuple.Item2.Invoke(item3, new object[2]
                {
                    sender,
                    args
                });
            }
        }
#pragma warning restore CS8620

        /// <summary>
        /// To be added.
        /// </summary>
        /// <typeparam name="TEventArgs">To be added.</typeparam>
        /// <param name="handler">To be added.</param>
        /// <param name="eventName">To be added.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks>To be added.</remarks>
        public void RemoveEventHandler<TEventArgs>(EventHandler<TEventArgs> handler, [CallerMemberName] string eventName = "") where TEventArgs : EventArgs
        {
            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            RemoveEventHandler(eventName, handler.Target, handler.GetMethodInfo());
        }

        /// <summary>
        /// To be added.
        /// </summary>
        /// <param name="handler">To be added.</param>
        /// <param name="eventName">To be added.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks>To be added.</remarks>
        public void RemoveEventHandler(EventHandler handler, [CallerMemberName] string eventName = "")
        {
            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            RemoveEventHandler(eventName, handler.Target, handler.GetMethodInfo());
        }
#pragma warning disable CS8625
        private void AddEventHandler(string eventName, object handlerTarget, MethodInfo methodInfo)
        {
            if (!_eventHandlers.TryGetValue(eventName, out List<Subscription> value))
            {
                value = new List<Subscription>();
                _eventHandlers.Add(eventName, value);
            }

            if (handlerTarget == null)
            {
                value.Add(new Subscription(null, methodInfo));
            }
            else
            {
                value.Add(new Subscription(new WeakReference(handlerTarget), methodInfo));
            }
        }
#pragma warning restore CS8625


        private void RemoveEventHandler(string eventName, object handlerTarget, MemberInfo methodInfo)
        {
            if (!_eventHandlers.TryGetValue(eventName, out List<Subscription> value))
            {
                return;
            }

            for (int num = value.Count; num > 0; num--)
            {
                Subscription item = value[num - 1];
                if (item.Subscriber?.Target == handlerTarget && !(item.Handler.Name != methodInfo.Name))
                {
                    value.Remove(item);
                    break;
                }
            }
        }
#pragma warning restore CS8600
    }
}
