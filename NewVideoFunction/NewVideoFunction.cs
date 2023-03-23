using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using DataAccessLayer.Repositories;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewVideoFunction.Interfaces;

namespace NewVideoFunction
{
    public class NewVideoFunction
    {
        private readonly IMailer _mailer;
        private readonly IUserService _userService;
        private readonly IBlobService _blobService;
        private readonly IBuildRepository _buildRepository;
        private readonly IChargeService _chargeService;

        public NewVideoFunction(IMailer mailer, IUserService userService, IBlobService blobService, IBuildRepository buildRepository, IChargeService chargeService)
        {
            _mailer = mailer;
            _userService = userService;
            _blobService = blobService;
            _buildRepository = buildRepository;
            _chargeService = chargeService;
        }
        /*
         https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-event-grid-trigger?tabs=in-process%2Cextensionv3&pivots=programming-language-csharp
        Put in fiddler:

         POST http://localhost:7207/runtime/webhooks/EventGrid?functionName=VideoNotifyFunction HTTP/1.1
User-Agent: Fiddler
aeg-event-type: Notification
Content-Type: application/json
Host: localhost:7207
Content-Length: 1008

[{
	"id": "741a105c-501e-007b-2436-25dfaa06188a",
	"topic": "/subscriptions/285ec89b-c6b0-46a6-9758-a0bce37bd2da/resourceGroups/music-video-builder/providers/Microsoft.Storage/storageAccounts/musicvideobuilderprivate",
	"subject": "/blobServices/default/containers/user-69d12eed-18c7-4763-8df2-ad828af710df/blobs/c636dff0-3c67-466e-a2cd-13afca023dc2/test2.mp4",
	"data": {
		"api": "PutBlockList",
		"requestId": "741a105c-501e-007b-2436-25dfaa000000",
		"eTag": "0x8DAF34DFFCADF62",
		"contentType": "application/octet-stream",
		"contentLength": 169910227,
		"blobType": "BlockBlob",
		"url": "https://musicvideobuilderprivate.blob.core.windows.net/user-69d12eed-18c7-4763-8df2-ad828af710df/user/69d12eed-18c7-4763-8df2-ad828af710df/1/2023-01-10T20-49-57/CircleOfLifeExtended.mp4",
		"sequencer": "0000000000000000000000000001493C00000000001b7609",
		"storageDiagnostics": {
			"batchId": "b98e3c68-e006-000c-0036-250a3e000000"
		}
	},
	"eventType": "Microsoft.Storage.BlobCreated",
	"eventTime": "2023-01-10T21:02:35.5883377Z",
	"dataVersion": ""
}]
         */
        [FunctionName("NewVideoFunction")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            log.LogInformation($"Processing EventGridEvent {JsonSerializer.Serialize(eventGridEvent)}");
            var blobPath = eventGridEvent.Subject;

            var splitBlob = blobPath.Split('/');
            var containerName = splitBlob[4];
            var buildId = splitBlob[6];

            log.LogInformation($"Container {containerName} buildId {buildId} subject {blobPath}");

            var build = await _buildRepository.GetAsync(Guid.Parse(buildId));
            if (build == null)
            {
                log.LogError($"Build {buildId} not in database");
                return;
            }

            build.BuildStatus = SharedEntities.Models.BuildStatus.PaymentChargePending;
            await _buildRepository.SaveAsync(build, build.UserObjectId);

            if (build.Resolution != SharedEntities.Models.Resolution.Free)
            {
                if (!await _chargeService.Charge(build.PaymentIntentId))
                {
                    return;
                }
            }

            var videoName = splitBlob.Last();
            var blobName = $"{buildId}/{videoName}";
            var blobPrefix = buildId;

            var userDetails = await _userService.GetDetails(build.UserObjectId.ToString());

            await _blobService.CleanTempFiles(containerName, blobPrefix);

            var blobSasUri = await _blobService.GetBlobSas(containerName, blobName);

            build.BuildStatus = SharedEntities.Models.BuildStatus.Complete;
            await _buildRepository.SaveAsync(build, build.UserObjectId);

            _mailer.Send(userDetails.username, userDetails.email, blobSasUri.ToString(), videoName);
        }
    }
}
