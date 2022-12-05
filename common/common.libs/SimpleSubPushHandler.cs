using System;
using System.Collections.Generic;

namespace common.libs
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class SimpleSubPushHandler<T>
    {
        List<Action<T>> actions = new List<Action<T>>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        public void Sub(Action<T> action)
        {
            if (!actions.Contains(action))
            {
                actions.Add(action);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void Push(T data)
        {
            if (actions.Count == 1)
            {
                actions[0](data);
            }
            else
            {
                for (int i = 0, len = actions.Count; i < len; i++)
                {
                    actions[i](data);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        public void Remove(Action<T> action)
        {
            actions.Remove(action);
        }

    }
}
