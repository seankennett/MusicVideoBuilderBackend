using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageMagick;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedEntities;
using SharedEntities.Models;
using UploadLayerFunction.Interfaces;
using UploadLayerFunction.Models;

namespace UploadLayerFunction
{
    public class UploadLayerFunction
    {
        private readonly Connections _connections;
        private readonly IDatabaseWriter _databaseWriter;
        private const int NumberOfImages = 64;
        private const int SpriteHeight = 216;
        private const int SpriteWidth = 384;
        public UploadLayerFunction(IOptions<Connections> options, IDatabaseWriter databaseWriter)
        {
            _connections = options.Value;
            _databaseWriter = databaseWriter;
        }

        [FunctionName("UploadLayerFunctionExecutor")]
        public async Task RunExecutor([QueueTrigger(SharedConstants.UploadLayerQueue, Connection = "ConnectionString")] LayerUploadMessage layerUploadMessage, [DurableClient] IDurableOrchestrationClient starter)
        {
            await starter.StartNewAsync("UploadLayerFunctionOrchastrator", layerUploadMessage);
        }

        [FunctionName("UploadLayerFunctionOrchastrator")]
        public async Task RunOrchastrator([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var layerUploadMessage = context.GetInput<LayerUploadMessage>();
            log.LogInformation($"Processing message {layerUploadMessage.LayerName} {layerUploadMessage.LayerId}");
            var containerName = layerUploadMessage.LayerId.ToString();

            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(10), 2);
            var orderedBlobNames = await context.CallActivityWithRetryAsync<List<string>>("ValidateAndGetBlobs", retryOptions, containerName);

            var shouldImageBeOpaque = layerUploadMessage.LayerType == LayerTypes.Background;

            var spriteImageTasks = new Task<byte[]>[NumberOfImages];
            for (int i = 0; i < orderedBlobNames.Count; i++)
            {
                spriteImageTasks[i] = context.CallActivityWithRetryAsync<byte[]>("UploadLayerFunctionActivity", retryOptions, new ProcessImageDTO { Index = i, OriginalName = orderedBlobNames[i], ShouldImageBeOpaque = shouldImageBeOpaque, ContainerName = containerName });
            }

            await Task.WhenAll(spriteImageTasks);

            await context.CallActivityAsync("CreateSpriteSaveAndTidy", new SpriteSaveCleanDTO { SpriteImages = spriteImageTasks.Select(x => x.Result).ToArray(), ContainerName = containerName, BlobNames = orderedBlobNames, LayerUploadMessage = layerUploadMessage });
        }

        [FunctionName("ValidateAndGetBlobs")]
        public async Task<List<string>> RunValidateAndGetBlobs([ActivityTrigger] IDurableActivityContext activityContext)
        {
            var containerName = activityContext.GetInput<string>();
            var privateContainerClient = new BlobContainerClient(_connections.PrivateStorageConnectionString, containerName);
            var publicContainerClient = new BlobContainerClient(_connections.PublicStorageConnectionString, containerName);
            await publicContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob); // won't exist on live

            var blobs = privateContainerClient.GetBlobs(BlobTraits.None, BlobStates.None, "4k/raw").ToList();
            if (blobs.Count != NumberOfImages || blobs.Any(x => !x.Name.EndsWith(".png")))
            {
                throw new ArgumentException("Invalid files");
            }

            return blobs.OrderBy(x => int.Parse(string.Concat(x.Name.Replace(".png", "").ToArray().Reverse().TakeWhile(char.IsNumber).Reverse()))).Select(x => x.Name).ToList();
        }

        [FunctionName("CreateSpriteSaveAndTidy")]
        public async Task RunCreateSpriteSaveAndTidy([ActivityTrigger] IDurableActivityContext activityContext, ILogger log)
        {
            var input = activityContext.GetInput<SpriteSaveCleanDTO>();
            using (MagickImageCollection imageCollection = new MagickImageCollection())
            {
                for (int i = 0; i < NumberOfImages; i++)
                {
                    MagickImage image = new MagickImage(input.SpriteImages[i]);
                    imageCollection.Add(image);
                }
                var publicContainerClient = new BlobContainerClient(_connections.PublicStorageConnectionString, input.ContainerName);
                var spriteBlobClient = publicContainerClient.GetBlobClient($"sprite.png");

                using (var spriteImage = imageCollection.Montage(
                    new MontageSettings
                    {
                        Geometry = new MagickGeometry($"{SpriteWidth}x{SpriteHeight}+0+0"),
                        TileGeometry = new MagickGeometry("64x1"),
                        BackgroundColor = new MagickColor("none")
                    }))
                {
                    spriteImage.Format = MagickFormat.Png;
                    spriteImage.Depth = 8;

                    await spriteBlobClient.UploadAsync(new BinaryData(spriteImage.ToByteArray()));
                }
            }

            await _databaseWriter.InsertLayer(input.LayerUploadMessage);

            foreach (var blobName in input.BlobNames)
            {
                var containerClient = new BlobContainerClient(_connections.PrivateStorageConnectionString, input.ContainerName);
                var deleteClient = containerClient.GetBlobClient(blobName);
                await deleteClient.DeleteAsync();
            }
        }

        [FunctionName("UploadLayerFunctionActivity")]
        public async Task<byte[]> RunActivity([ActivityTrigger] IDurableActivityContext activityContext, ILogger log, ExecutionContext context)
        {
            var input = activityContext.GetInput<ProcessImageDTO>();
            var privateContainerClient = new BlobContainerClient(_connections.PrivateStorageConnectionString, input.ContainerName);
            var publicContainerClient = new BlobContainerClient(_connections.PublicStorageConnectionString, input.ContainerName);

            var blobClient = privateContainerClient.GetBlobClient(input.OriginalName);
            string watermarkFilePath = Path.Combine(context.FunctionAppDirectory, "watermark.png");

            using (var memoryStream = new MemoryStream())
            {
                await blobClient.DownloadToAsync(memoryStream);
                memoryStream.Position = 0;

                ImageOptimizer imageOptimizer = new ImageOptimizer();
                imageOptimizer.LosslessCompress(memoryStream);

                using (MagickImage image = new MagickImage(memoryStream))
                {
                    if (image.Width != 3840 || image.Height != 2160 || input.ShouldImageBeOpaque == !image.IsOpaque)
                    {
                        throw new ArgumentException("Invalid image");
                    }
                    // AddPixelToNonOpaque(image); doesnt seem to be enough

                    var blobClient4k = privateContainerClient.GetBlobClient($"4k/{input.Index}.png");
                    if (!await blobClient4k.ExistsAsync())
                    {
                        await blobClient4k.UploadAsync(new BinaryData(image.ToByteArray()));
                    }

                    image.Resize(1920, 1080);
                    // AddPixelToNonOpaque(image); doesnt seem to be enough

                    var blobClientHd = privateContainerClient.GetBlobClient($"hd/{input.Index}.png");
                    if (!await blobClientHd.ExistsAsync())
                    {
                        await blobClientHd.UploadAsync(new BinaryData(image.ToByteArray()));
                    }

                    image.Resize(384, 216);
                    var blobClientFree = publicContainerClient.GetBlobClient($"free/{input.Index}.png");

                    using (var watermark = new MagickImage(watermarkFilePath))
                    {
                        // Draw the watermark in the middle
                        image.Composite(watermark, Gravity.Southeast, 5, 0, CompositeOperator.Over);
                        var output = image.ToByteArray();
                        if (!await blobClientFree.ExistsAsync())
                        {
                            await blobClientFree.UploadAsync(new BinaryData(output));
                        }

                        return output;
                    }
                }
            }

        }

        private void AddPixelToNonOpaque(IMagickImage<ushort> image)
        {
            // would love to know how to detect fully clear images
            if (!image.IsOpaque)
            {
                new Drawables()
                    .FillColor(new MagickColor("#007BFF"))
                    .Rectangle(0, 0, 1, 1)
                    .Draw(image);
            }
        }
    }
}
