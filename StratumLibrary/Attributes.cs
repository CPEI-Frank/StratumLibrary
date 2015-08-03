using System;

namespace Stratum
{
    /// <summary>
    /// Required to expose a method to the Stratum service.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class StratumMethodAttribute : Attribute
    {
        readonly string stratumMethodName;

        /// <summary>
        /// Required to expose a method to the Stratum service.
        /// </summary>
        /// <param name="stratumMethodName">Lets you specify the method name as it will be referred to by JsonRpc.</param>
        public StratumMethodAttribute(string MethodName = "")
        {
            this.stratumMethodName = MethodName;
        }

        public string StratumMethodName
        {
            get { return stratumMethodName; }
        }
    }
}
