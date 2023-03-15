using System.Threading.Tasks;

namespace NewVideoFunction.Interfaces
{
    public interface IChargeService
    {
        Task<bool> Charge(string paymentIntentId);
    }
}
