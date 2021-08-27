using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSlope.Api.Common.Exceptions
{
    /// <summary>
    /// Allows you to do switch statements against types, with auto-casting.
    /// </summary>
    public static class TypeSwitch
    {
        /// <summary>
        /// Evaluates the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="cases">The cases.</param>
        public static void Eval(object source, params CaseInfo[] cases)
        {
            var type = source.GetType();

            foreach (var entry in cases)
            {
                if (entry.IsDefault || entry.Target.IsAssignableFrom(type))
                {
                    entry.Action(source);
                    break;
                }
            }
        }

        /// <summary>
        /// Case statement for a given action.
        /// </summary>
        /// <typeparam name="T">The action</typeparam>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public static CaseInfo Case<T>(Action action)
        {
            return new CaseInfo()
            {
                Action = x => action(),
                Target = typeof(T),
            };
        }

        /// <summary>
        /// Case statement for a given generic action.
        /// </summary>
        /// <typeparam name="T">The action</typeparam>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public static CaseInfo Case<T>(Action<T> action)
        {
            return new CaseInfo()
            {
                Action = (x) => action((T)x),
                Target = typeof(T),
            };
        }

        /// <summary>
        /// Default action taken when no cases evaluate.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public static CaseInfo Default(Action action)
        {
            return new CaseInfo()
            {
                Action = x => action(),
                IsDefault = true,
            };
        }

        public class CaseInfo
        {
            public bool IsDefault { get; set; }

            public Type Target { get; set; }

            public Action<object> Action { get; set; }
        }
    }
}
