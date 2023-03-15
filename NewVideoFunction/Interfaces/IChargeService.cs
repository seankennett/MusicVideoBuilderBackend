using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewVideoFunction.Interfaces
{
    public interface IChargeService
    {
        Task<bool> Charge(string paymentIntentId);
    }
}
