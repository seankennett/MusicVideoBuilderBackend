using System;

namespace BuilderFunction
{
    public class BuilderConfig
    {
        public int MaxConcurrentActivityFunctions { get; internal set; }
        public TimeSpan FunctionTimeOut { get; internal set; }
    }
}
