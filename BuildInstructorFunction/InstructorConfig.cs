using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildInstructorFunction
{
    public class InstructorConfig
    {
        public string StripeWebhookKey { get; set; }
        public string BatchServiceEndpoint { get; set; }
        public string BatchServiceName { get; set; }
        public string BatchServiceKey { get; set; }
        public string PoolName { get; set; }
        public string ManagedIdentityIdReference { get; set; }
    }
}
