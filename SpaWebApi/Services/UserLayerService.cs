using DataAccessLayer.Repositories;
using SharedEntities.Models;

namespace SpaWebApi.Services
{
    public class UserLayerService : IUserLayerService
    {
        private readonly IUserLayerRepository _userLayerRepository;
        //private readonly IClipRepository _clipRepository;

        public UserLayerService(IUserLayerRepository userLayerRepository, IClipRepository clipRepository)
        {
            _userLayerRepository = userLayerRepository;
            // _clipRepository = clipRepository;
        }
        //public async Task DeleteAsync(Guid userObjectId, int userLayerId)
        //{
        //    var userLayer = await _userLayerRepository.GetAsync(userObjectId, userLayerId);
        //    if (userLayer == null)
        //    {
        //        throw new Exception($"User doesn't own user layer id {userLayerId}");
        //    }

        //    await _userLayerRepository.DeleteAsync(userLayerId);
        //}

        public Task<IEnumerable<UserLayer>> GetAllAsync(Guid userObjectId)
        {
            return _userLayerRepository.GetAllAsync(userObjectId);
        }

        //public Task<UserLayer> SaveAsync(Guid userObjectId, Guid layerId)
        //{
        //    return _userLayerRepository.SaveAsync(userObjectId, layerId);
        //}

        //public async Task<UserLayer> UpdateAsync(Guid userObjectId, UserLayer userLayer)
        //{
        //    var databaseUserLayer = await _userLayerRepository.GetAsync(userObjectId, userLayer.UserLayerId);
        //    if (databaseUserLayer == null)
        //    {
        //        throw new Exception($"User doesn't own user layer id {userLayer.UserLayerId}");
        //    }

        //    // maybe need verification logic here to do with purchase
        //    return await _userLayerRepository.UpdateAsync(databaseUserLayer.UserLayerId, userLayer.UserLayerStatus);
        //}
    }
}
