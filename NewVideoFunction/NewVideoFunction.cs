using System;
using System.Threading.Tasks;
using BuildDataAccess.Extensions;
using BuildDataAccess.Repositories;
using BuildEntities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NewVideoFunction.Interfaces;
using SharedEntities.Models;

namespace NewVideoFunction
{
    public class NewVideoFunction
    {
        private readonly IMailer _mailer;
        private readonly IUserService _userService;
        private readonly IBlobService _blobService;
        private readonly IBuildRepository _buildRepository;
        private readonly IChargeService _chargeService;
        private readonly IUserDisplayLayerRepository _userLayerRepository;

        public NewVideoFunction(IMailer mailer, IUserService userService, IBlobService blobService, IBuildRepository buildRepository, IChargeService chargeService, IUserDisplayLayerRepository userLayerRepository)
        {
            _mailer = mailer;
            _userService = userService;
            _blobService = blobService;
            _buildRepository = buildRepository;
            _chargeService = chargeService;
            _userLayerRepository = userLayerRepository;
        }
        
        [FunctionName("NewVideoFunction")]
        public async Task Run([QueueTrigger("%QueueName%", Connection = "ConnectionString")] Guid buildId, ILogger log)
        {
            var userBuild = await _buildRepository.GetAsync(buildId);
            if (userBuild == null)
            {
                log.LogError($"Build {buildId} not in database");
                return;
            }

            userBuild.BuildStatus = BuildStatus.PaymentChargePending;
            await _buildRepository.SaveAsync(userBuild, userBuild.UserObjectId);

            if (userBuild.Resolution != Resolution.Free)
            {
                await _userLayerRepository.ConfirmPendingUserLayers(buildId);
                if (!await _chargeService.Charge(userBuild.PaymentIntentId))
                {
                    return;
                }
            }

            var videoName = $"{userBuild.VideoName}.{userBuild.Format}";
            var blobName = $"{buildId}/{videoName}";
            var containerName = GuidHelper.GetUserContainerName(userBuild.UserObjectId);

            var userDetails = await _userService.GetDetails(userBuild.UserObjectId.ToString());

            await _blobService.CleanTempFiles(containerName, buildId.ToString());

            var blobSasUri = await _blobService.GetBlobSas(containerName, blobName);

            userBuild.BuildStatus = BuildStatus.Complete;
            await _buildRepository.SaveAsync(userBuild, userBuild.UserObjectId);

            _mailer.Send(userDetails.username, userDetails.email, blobSasUri.ToString(), videoName);
        }
    }
}
