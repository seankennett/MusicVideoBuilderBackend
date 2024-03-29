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
        private readonly IUserCollectionRepository _userLayerRepository;

        public NewVideoFunction(IMailer mailer, IUserService userService, IBlobService blobService, IBuildRepository buildRepository, IChargeService chargeService, IUserCollectionRepository userLayerRepository)
        {
            _mailer = mailer;
            _userService = userService;
            _blobService = blobService;
            _buildRepository = buildRepository;
            _chargeService = chargeService;
            _userLayerRepository = userLayerRepository;
        }

        [FunctionName("NewVideoFunction")]
        public async Task Run([QueueTrigger("%QueueName%", Connection = "ConnectionString")] string buildIdString, ILogger log)
        {
            var buildId = Guid.Parse(buildIdString);
            var userBuild = await _buildRepository.GetAsync(buildId);
            if (userBuild == null)
            {
                log.LogError($"Build {buildIdString} not in database");
                return;
            }

            userBuild.BuildStatus = BuildStatus.PaymentChargePending;
            await _buildRepository.SaveAsync(userBuild, userBuild.UserObjectId);

            // maybe unecessary and could check against subscription but it is a safe call and subscription may change during build
            if (userBuild.License != License.Personal)
            {
                await _userLayerRepository.ConfirmPendingCollections(buildId);
            }

            if (userBuild.PaymentIntentId != null && !await _chargeService.Charge(userBuild.PaymentIntentId))
            {
                log.LogError($"Could not charge payment intent {userBuild.PaymentIntentId}");
                return;
            }

            var videoName = $"{userBuild.VideoName}.{userBuild.Format}";
            var blobName = $"{buildIdString}/{videoName}";
            var containerName = GuidHelper.GetUserContainerName(userBuild.UserObjectId);

            var userDetails = await _userService.GetDetails(userBuild.UserObjectId.ToString());

            await _blobService.CleanTempFiles(containerName, buildIdString);

            var blobSasUri = await _blobService.GetBlobSas(containerName, blobName);

            userBuild.BuildStatus = BuildStatus.Complete;
            await _buildRepository.SaveAsync(userBuild, userBuild.UserObjectId);

            _mailer.Send(userDetails.username, userDetails.email, blobSasUri.ToString(), videoName);
        }
    }
}
