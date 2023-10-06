using System;

namespace BuilderFunction
{
    public class BuilderConfig
    {
        public int MaxConcurrentActivityFunctions { get; set; }
        public TimeSpan FunctionTimeOut { get; set; }
        public string NewVideoQueueName { get; set; }
    }
}
