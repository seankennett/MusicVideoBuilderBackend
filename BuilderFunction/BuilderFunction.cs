using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Linq;
using BuilderFunction.Models;
using SharedEntities.Models;
using SharedEntities.Extensions;

namespace BuilderFunction
{
    public class BuilderFunction
    {
        private readonly Connections _connections;

        private const byte FramesPerLayer = 64;
        private const double FunctionOutboundConnectionLimit = 600;

        private string _layerStorageConnectionString;

        public BuilderFunction(IOptions<Connections> options)
        {
            _connections = options.Value;

            _layerStorageConnectionString = _connections.PrivateStorageConnectionString;
            if (_connections.Resolution == Resolution.Free.GetBlobPrefixByResolution())
            {
                _layerStorageConnectionString = _connections.PublicStorageConnectionString;
            }
        }

        [FunctionName("BuilderExecutor")]
        public async Task RunExecutor([QueueTrigger("%QueueName%", Connection = "ConnectionString")] BuilderMessage builderMessage, [DurableClient] IDurableOrchestrationClient starter)
        {
            await starter.StartNewAsync("BuilderOrchastrator", builderMessage);
        }

        [FunctionName("BuilderOrchastrator")]
        public async Task RunOrchastrator([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var builderMessage = context.GetInput<BuilderMessage>();
            log.LogInformation($"Processing message for {builderMessage.UserContainerName}/{builderMessage.OutputBlobPrefix}/{builderMessage.ClipMergeCommand.VideoName}");

            var workingDirectory = await context.CallActivityWithRetryAsync<string>("CreateWorkingDirectoryActivity", new RetryOptions(TimeSpan.FromSeconds(10), 1), builderMessage.TemporaryBlobPrefix);

            try
            {
                var assetsDownloadDTO = (AssetsDownloadDTO)builderMessage.AssetsDownload;
                assetsDownloadDTO.WorkingDirectory = workingDirectory;
                assetsDownloadDTO.TemporaryBlobPrefix = builderMessage.TemporaryBlobPrefix;
                assetsDownloadDTO.UserContainerName = builderMessage.UserContainerName;

                await context.CallActivityWithRetryAsync("AssetsDownloadActivity", new RetryOptions(TimeSpan.FromSeconds(10), 1), assetsDownloadDTO);

                var clipTasks = new List<Task>();
                foreach (var clipCommand in builderMessage.ClipCommands)
                {
                    var builtClipCommand = BuildFfmpegIOCommandDTO(clipCommand, builderMessage, workingDirectory);
                    clipTasks.Add(context.CallActivityWithRetryAsync("FfmpegIOCommandActivity", new RetryOptions(TimeSpan.FromSeconds(10), 1), builtClipCommand));
                }

                await Task.WhenAll(clipTasks);

                var builtClipMergeCommand = BuildFfmpegIOCommandDTO(builderMessage.ClipMergeCommand, builderMessage, workingDirectory);
                await context.CallActivityWithRetryAsync("FfmpegIOCommandActivity", new RetryOptions(TimeSpan.FromSeconds(10), 1), builtClipMergeCommand);

                var splitFrameTasks = new List<Task>();
                foreach (var splitFrameCommand in builderMessage.SplitFrameCommands)
                {
                    var builtSplitFrameCommand = BuildFfmpegIOCommandDTO(splitFrameCommand, builderMessage, workingDirectory);
                    splitFrameTasks.Add(context.CallActivityWithRetryAsync("FfmpegIOCommandActivity", new RetryOptions(TimeSpan.FromSeconds(10), 1), builtSplitFrameCommand));
                }

                await Task.WhenAll(splitFrameTasks);

                var builtSplitFrameMergeCommand = BuildFfmpegIOCommandDTO(builderMessage.SplitFrameMergeCommand, builderMessage, workingDirectory);
                await context.CallActivityWithRetryAsync("FfmpegIOCommandActivity", new RetryOptions(TimeSpan.FromSeconds(10), 1), builtSplitFrameMergeCommand);

                var assetUpload = new AssetUpload { UserContainerName = builderMessage.UserContainerName, OutputBlobPrefix = builderMessage.OutputBlobPrefix, OutputFileName = builderMessage.SplitFrameMergeCommand.VideoName, WorkingDirectory = workingDirectory };
                await context.CallActivityWithRetryAsync("AssetsUploadActivity", new RetryOptions(TimeSpan.FromSeconds(10), 1), assetUpload);
            }
            catch
            {
                await context.CallActivityWithRetryAsync("DeleteWorkingDirectoryActivity", new RetryOptions(TimeSpan.FromSeconds(10), 1), workingDirectory);
                throw;
            }

            await context.CallActivityWithRetryAsync("DeleteWorkingDirectoryActivity", new RetryOptions(TimeSpan.FromSeconds(10), 1), workingDirectory);
        }

        [FunctionName("CreateWorkingDirectoryActivity")]
        public string CreateWorkingDirectoryActivity([ActivityTrigger] IDurableActivityContext activityContext, ILogger log, ExecutionContext context)
        {
            var tempPath = activityContext.GetInput<string>();
            var randomFolder = Guid.NewGuid().ToString();
            var workingDirectory = Path.Combine(context.FunctionAppDirectory, randomFolder);
            var temporaryFolderPath = Path.Combine(workingDirectory, tempPath);
            log.LogInformation($"Creating working directory temporary folder structure {temporaryFolderPath}");
            Directory.CreateDirectory(temporaryFolderPath);
            return workingDirectory;
        }

        [FunctionName("DeleteWorkingDirectoryActivity")]
        public void DeleteWorkingDirectoryActivity([ActivityTrigger] IDurableActivityContext activityContext, ILogger log)
        {
            var workingDirectory = activityContext.GetInput<string>();
            log.LogInformation($"Deleting working directory {workingDirectory}");
            Directory.Delete(workingDirectory, true);
        }

        private FfmpegIOCommandDTO BuildFfmpegIOCommandDTO(FfmpegIOCommand command, BuilderMessage builderMessage, string workingDirectory)
        {
            var commandDTO = (FfmpegIOCommandDTO)command;
            commandDTO.TemporaryBlobPrefix = builderMessage.TemporaryBlobPrefix;
            commandDTO.WorkingDirectory = workingDirectory;

            return commandDTO;
        }

        [FunctionName("FfmpegIOCommandActivity")]
        public async Task RunFfmpegIOCommandActivity([ActivityTrigger] IDurableActivityContext activityContext, ILogger log, ExecutionContext context)
        {
            var ffmpegIOCommand = activityContext.GetInput<FfmpegIOCommandDTO>();

            var psi = new ProcessStartInfo();
            psi.FileName = Path.Combine(context.FunctionAppDirectory, "ffmpeg.exe");
            psi.WorkingDirectory = ffmpegIOCommand.WorkingDirectory;
            psi.Arguments = ffmpegIOCommand.FfmpegCode;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;

            var process = Process.Start(psi);

            log.LogInformation($"Running ffmpeg command {ffmpegIOCommand.FfmpegCode}");

            var standardErrorTask = process.StandardError.ReadToEndAsync();

            // function will have a timeout so take away execution so far and 10 secconds margin for error
            var internalTimeOut = _connections.FunctionTimeOut - TimeSpan.FromSeconds(10);
            if (process.WaitForExit((int)internalTimeOut.TotalMilliseconds))
            {
                var standardError = await standardErrorTask;
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Proccess failed. Exit Code {process.ExitCode}. StdError {standardError}");
                }

                log.LogInformation($"Finished process for {ffmpegIOCommand.FfmpegCode} StdError {standardError}");
            }
            else
            {
                process.Kill();
                process.WaitForExit();

                throw new Exception($"Process timed out internally cleanly after {internalTimeOut.TotalSeconds} seconds.");
            }
        }

        [FunctionName("AssetsUploadActivity")]
        public async Task AssetsUploadActivity([ActivityTrigger] IDurableActivityContext activityContext, ILogger log)
        {
            var assetUpload = activityContext.GetInput<AssetUpload>();

            var outputBlobClient = new BlobClient(_connections.PrivateStorageConnectionString, assetUpload.UserContainerName, $"{assetUpload.OutputBlobPrefix}/{assetUpload.OutputFileName}");
            await outputBlobClient.UploadAsync(Path.Combine(assetUpload.WorkingDirectory, assetUpload.OutputBlobPrefix, assetUpload.OutputFileName), overwrite: true);
        }

        [FunctionName("AssetsDownloadActivity")]
        public async Task AssetsDownloadActivity([ActivityTrigger] IDurableActivityContext activityContext, ILogger log)
        {
            var assetsDownload = activityContext.GetInput<AssetsDownloadDTO>();
            var workingDirectory = assetsDownload.WorkingDirectory;

            var blobClients = new List<(BlobClient blobClient, string filePath)>();
            foreach (var layerId in assetsDownload.LayerIds)
            {
                var tempImageDirectory = Path.Combine(workingDirectory, layerId, _connections.Resolution);

                log.LogInformation($"Creating directory {tempImageDirectory}");
                Directory.CreateDirectory(tempImageDirectory);

                for (int j = 0; j < FramesPerLayer; j++)
                {
                    var imageName = $"{j}.png";
                    var blobClient = new BlobClient(_layerStorageConnectionString, layerId, $"{_connections.Resolution}/{imageName}");
                    var filePath = Path.Combine(tempImageDirectory, imageName);
                    log.LogInformation($"Will download {imageName} to filepath {filePath}");
                    blobClients.Add((blobClient, filePath));
                }
            }

            foreach (var temporaryFile in assetsDownload.TemporaryFiles)
            {
                var blobClient = new BlobClient(_connections.PrivateStorageConnectionString, assetsDownload.UserContainerName, $"{assetsDownload.TemporaryBlobPrefix}/{temporaryFile}");
                var filePath = Path.Combine(workingDirectory, assetsDownload.TemporaryBlobPrefix, temporaryFile);
                log.LogInformation($"Will download {temporaryFile} to filepath {filePath}");
                blobClients.Add((blobClient, filePath));
            }

            var batchSize = (int)Math.Floor(FunctionOutboundConnectionLimit / _connections.MaxConcurrentActivityFunctions);
            int numberOfBatches = (int)Math.Ceiling((double)blobClients.Count / batchSize);
            log.LogInformation($"Batch size {batchSize}, number of batches {numberOfBatches}");

            for (int i = 0; i < numberOfBatches; i++)
            {
                log.LogInformation($"Starting batch number {i + 1} of batches {numberOfBatches}");
                var currentBlobClients = blobClients.Skip(i * batchSize).Take(batchSize);
                var tasks = currentBlobClients.Select(x => x.blobClient.DownloadToAsync(x.filePath));

                await Task.WhenAll(tasks);
                log.LogInformation($"Finished batch number {i + 1} of batches {numberOfBatches}");
            }

            log.LogInformation($"Downloaded all assets");
        }
    }
}
