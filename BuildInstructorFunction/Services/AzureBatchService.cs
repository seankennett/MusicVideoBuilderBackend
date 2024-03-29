﻿using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Batch.Common;
using Microsoft.Extensions.Options;
using BuilderEntities.Entities;
using BuilderEntities.Extensions;
using BuildEntities;
using BuildDataAccess;
using System.Text;

namespace BuildInstructorFunction.Services
{
    public class AzureBatchService : IAzureBatchService
    {
        private readonly IStorageService _storageService;
        private readonly BatchSharedKeyCredentials _batchCredentials;
        private readonly string _poolId;
        private readonly ComputeNodeIdentityReference _computeNodeIdentityReference;
        private readonly InstructorConfig _instructorConfig;
        private const int TopPriority = 1000;
        private const int BottomPriority = -1000;

        public AzureBatchService(IStorageService storageService, IOptions<InstructorConfig> config)
        {
            _storageService = storageService;

            // waiting on MS to allow identity stuff here :(
            _batchCredentials = new BatchSharedKeyCredentials(config.Value.BatchServiceEndpoint, config.Value.BatchServiceName, config.Value.BatchServiceKey);
            _poolId = config.Value.PoolName;
            _computeNodeIdentityReference =new ComputeNodeIdentityReference { ResourceId = config.Value.ManagedIdentityIdReference };
            _instructorConfig = config.Value;
        }

        public async Task SendBatchRequest(string userContainerName, bool hasAudio, Guid buildId, Resolution resolution, string outputBlobPrefix, string tempBlobPrefix, Dictionary<int, IEnumerable<string>> layerIdsPerClip, List<FfmpegIOCommand> clipCommands, FfmpegIOCommand clipMergeCommand, List<FfmpegIOCommand> splitFrameCommands, FfmpegIOCommand splitFrameMergeCommand)
        {
            var outputContainerUri = _storageService.GetContainerUri(userContainerName);

            List<ResourceFile> allFrameVideoInputs = new List<ResourceFile> { ResourceFile.FromAutoStorageContainer(userContainerName, null, $"{tempBlobPrefix}/{InstructorConstants.AllFramesConcatFileName}") };
            var clipTasks = new List<CloudTask>();
            for (var i = 0; i < clipCommands.Count(); i++)
            {
                var clipCommand = clipCommands[i];

                var clipTaskId = $"c-{buildId}-{i}";
                var clipFileName = clipCommand.VideoName;

                List<ResourceFile> inputFiles = new List<ResourceFile>();
                if (layerIdsPerClip.ContainsKey(i))
                {
                    var layers = layerIdsPerClip[i];
                    foreach (var layer in layers)
                    {
                        inputFiles.Add(ResourceFile.FromAutoStorageContainer(layer, layer, resolution.GetBlobPrefixByResolution()));
                    }
                }

                var outputBlobName = $"{tempBlobPrefix}/{clipFileName}";
                CloudTask clipTask = SetUpTask(outputBlobName, outputContainerUri, $"{tempBlobPrefix}/{i}", clipCommand.FfmpegCode, clipTaskId, inputFiles, clipFileName);
                clipTasks.Add(clipTask);

                allFrameVideoInputs.Add(ResourceFile.FromAutoStorageContainer(userContainerName, null, outputBlobName));
            }

            var allFrameVideoCommand = clipMergeCommand.FfmpegCode;
            var allFramesTaskId = $"a-{buildId}";
            var allFramesOutputBlobName = $"{tempBlobPrefix}/{clipMergeCommand.VideoName}";
            var allFramesTask = SetUpTask(allFramesOutputBlobName, outputContainerUri, $"{tempBlobPrefix}/{InstructorConstants.AllFramesVideoName}", allFrameVideoCommand, allFramesTaskId, allFrameVideoInputs, allFramesOutputBlobName);
            allFramesTask.DependsOn = TaskDependencies.OnTasks(clipTasks);

            List<ResourceFile> splitFrameInputs = new List<ResourceFile> { ResourceFile.FromAutoStorageContainer(userContainerName, null, allFramesOutputBlobName) };
            List<ResourceFile> splitFrameMergeInputs = new List<ResourceFile> { ResourceFile.FromAutoStorageContainer(userContainerName, null, $"{tempBlobPrefix}/{InstructorConstants.SplitFramesConcatFileName}") };
            if (hasAudio)
            {
                splitFrameMergeInputs.Add(ResourceFile.FromAutoStorageContainer(userContainerName, null, $"{tempBlobPrefix}/{BuildDataAccessConstants.AudioFileName}"));
            }

            var splitFramesTasks = new List<CloudTask>();
            for (int i = 0; i < splitFrameCommands.Count; i++)
            {
                var splitFrameCommand = splitFrameCommands[i];
                var splitTaskId = $"s-{buildId}-{i}";
                var splitFrameOutputBlobName = $"{tempBlobPrefix}/{splitFrameCommand.VideoName}";
                CloudTask splitFrameTask = SetUpTask(splitFrameOutputBlobName, outputContainerUri, $"{tempBlobPrefix}/split", splitFrameCommand.FfmpegCode, splitTaskId, splitFrameInputs, splitFrameOutputBlobName);
                splitFrameTask.DependsOn = TaskDependencies.OnTasks(allFramesTask);
                splitFramesTasks.Add(splitFrameTask);

                splitFrameMergeInputs.Add(ResourceFile.FromAutoStorageContainer(userContainerName, null, splitFrameOutputBlobName));
            }

            var splitFrameMergeVideoCommand = splitFrameMergeCommand.FfmpegCode;
            var splitMergeTaskId = $"v-{buildId}";
            var splitMergeBlobName = $"{outputBlobPrefix}/{splitFrameMergeCommand.VideoName}";
            var splitMergeTask = SetUpTask(splitMergeBlobName, outputContainerUri, tempBlobPrefix, splitFrameMergeVideoCommand, splitMergeTaskId, splitFrameMergeInputs, splitMergeBlobName);
            splitMergeTask.DependsOn = TaskDependencies.OnTasks(splitFramesTasks);

            var newVideoMessageTaskId = $"m-{buildId}";
            var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(buildId.ToString()));
            var newVideoMessageCommand = $"/bin/bash -c 'az login --identity -u {_instructorConfig.ManagedIdentityIdReference};az storage message put --content {base64Content} --queue-name {_instructorConfig.NewVideoQueueName} --account-name {_instructorConfig.PrivateAccountName} --auth-mode login'";
            var sendNewVideoMessageTask = SetupTaskBase(newVideoMessageTaskId, newVideoMessageCommand, new List<OutputFile>(), outputContainerUri, tempBlobPrefix);
            sendNewVideoMessageTask.DependsOn = TaskDependencies.OnTasks(splitMergeTask);

            var allTasks = clipTasks.Union(splitFramesTasks).Union(new List<CloudTask> { allFramesTask, splitMergeTask, sendNewVideoMessageTask });
            using (var batchClient = BatchClient.Open(_batchCredentials))
            {                              
                var job = batchClient.JobOperations.CreateJob(splitMergeTaskId, new PoolInformation { PoolId = _poolId }); // get pool from config
                job.Priority = TopPriority;
                job.OnAllTasksComplete = OnAllTasksComplete.TerminateJob;
                job.UsesTaskDependencies = true;
                job.Constraints = new JobConstraints { MaxTaskRetryCount = 1, MaxWallClockTime = TimeSpan.FromDays(1) };

                var detailLevel = new ODATADetailLevel
                {
                    SelectClause = "id, priority",
                    FilterClause = "state eq 'active'"
                };
                var existingJobs = await batchClient.JobOperations.ListJobs(detailLevel).ToListAsync();
                if (existingJobs != null && existingJobs.Any())
                {
                    job.Priority = existingJobs.Min(j => j.Priority.HasValue ? j.Priority.Value : 0) - 1;
                    if (job.Priority < BottomPriority)
                    {
                        var priority = TopPriority;
                        foreach (var existingJob in existingJobs.OrderByDescending(e => e.Priority.HasValue ? e.Priority.Value : 0))
                        {
                            existingJob.Priority = priority;
                            await existingJob.CommitChangesAsync();
                            priority--;
                        }
                        job.Priority = priority;
                    }
                }

                await job.CommitAsync();
                await batchClient.JobOperations.AddTaskAsync(job.Id, allTasks);
            }
        }

        private CloudTask SetUpTask(string outputBlobName, Uri outputContainerUri, string errorBlobPrefix, string command, string taskId, List<ResourceFile> inputFiles, string filePath)
        {
            List<OutputFile> outputFileList = new List<OutputFile>();
            OutputFileBlobContainerDestination outputFileBlobContainerDestination = new OutputFileBlobContainerDestination(outputContainerUri.ToString(), _computeNodeIdentityReference, outputBlobName);
            outputFileList.Add(new OutputFile(filePath,
                                                   new OutputFileDestination(outputFileBlobContainerDestination),
                                                   new OutputFileUploadOptions(OutputFileUploadCondition.TaskSuccess)));

            var cloudTask = SetupTaskBase(taskId, command, outputFileList, outputContainerUri, errorBlobPrefix);
            cloudTask.ResourceFiles = inputFiles;

            return cloudTask;
        }

        private CloudTask SetupTaskBase(string taskId, string command, List<OutputFile> outputFileList, Uri outputContainerUri, string errorBlobPrefix)
        {
            CloudTask cloudTask = new CloudTask(taskId, command);
            cloudTask.Constraints = new TaskConstraints { MaxWallClockTime = TimeSpan.FromHours(1), RetentionTime = TimeSpan.Zero };
            OutputFileBlobContainerDestination errorOutputFileBlobContainerDestination = new OutputFileBlobContainerDestination(outputContainerUri.ToString(), _computeNodeIdentityReference, errorBlobPrefix);
            outputFileList.Add(new OutputFile(@"../std*.txt",
                                                   new OutputFileDestination(errorOutputFileBlobContainerDestination),
                                                   new OutputFileUploadOptions(OutputFileUploadCondition.TaskFailure)));
            cloudTask.OutputFiles = outputFileList;
            return cloudTask;
        }
    }
}
