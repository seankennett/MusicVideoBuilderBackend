using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using VideoDataAccess.DTOEntities;
using VideoDataAccess.Entities;
using VideoDataAccess.Helpers;
using VideoEntities.Entities;

namespace VideoDataAccess.Repositories
{
    public class VideoRepository : IVideoRepository
    {
        private readonly string _sqlConnection;

        public VideoRepository(IOptions<SqlConfig> connections)
        {
            _sqlConnection = connections.Value.DatabaseConnectionString;
        }

        public async Task<IEnumerable<Video>> GetAllAsync(Guid userObjectId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                var reader = await connection.QueryMultipleAsync("GetVideos", new { userObjectId }, commandType: CommandType.StoredProcedure);
                var videos = await reader.ReadAsync<VideoDTO>();
                var clips = await reader.ReadAsync<VideoClipDTO>();
                var clipDisplayLayers = await reader.ReadAsync<ClipDisplayLayerDTO>();
                var layerClipDisplayLayers = await reader.ReadAsync<LayerClipDisplayLayerDTO>();

                var groupedClips = clips.GroupBy(x => x.VideoId);
                var groupedClipDisplayLayers = clipDisplayLayers.GroupBy(x => x.ClipId);
                var groupedLayerClipDisplayLayers = layerClipDisplayLayers.GroupBy(x => x.ClipDisplayLayerId);

                return videos.Select(v => new Video
                {
                    Clips = groupedClips.First(gc => gc.Key == v.VideoId).OrderBy(gc => gc.Order).Select(gc =>
                    {
                        return ClipHelper.HydrateClip(groupedClipDisplayLayers, groupedLayerClipDisplayLayers, gc);
                    }),
                    BPM = v.BPM,
                    Format = (Formats)v.FormatId,
                    VideoId = v.VideoId,
                    VideoName = v.VideoName,
                    VideoDelayMilliseconds = v.VideoDelayMilliseconds,
                }
                );
            }
        }

        public async Task<Video> SaveAsync(Guid userObjectId, Video video)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("ForeignId");
            dataTable.Columns.Add("Order");
            for (short i = 0; i < video.Clips.Count(); i++)
            {
                dataTable.Rows.Add(video.Clips.ElementAt(i).ClipId, i);
            }

            using (var connection = new SqlConnection(_sqlConnection))
            {
                video.VideoId = await connection.QueryFirstAsync<int>("UpsertVideo", new
                {
                    userObjectId,
                    video.VideoId,
                    video.VideoName,
                    video.BPM,
                    FormatId = (byte)video.Format,
                    video.VideoDelayMilliseconds,
                    Clips = dataTable.AsTableValuedParameter("IntOrderType"),
                }, commandType: CommandType.StoredProcedure);
                return video;
            }
        }

        public async Task<Video?> GetAsync(Guid userObjectId, int videoId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                var reader = await connection.QueryMultipleAsync("GetVideo", new { userObjectId, VideoId = videoId }, commandType: CommandType.StoredProcedure);
                var video = await reader.ReadFirstOrDefaultAsync<VideoDTO>();

                if (video != null)
                {
                    var clips = await reader.ReadAsync<VideoClipDTO>();
                    var clipDisplayLayers = await reader.ReadAsync<ClipDisplayLayerDTO>();
                    var layerClipDisplayLayers = await reader.ReadAsync<LayerClipDisplayLayerDTO>();

                    var groupedClips = clips.GroupBy(x => x.VideoId);
                    var groupedClipDisplayLayers = clipDisplayLayers.GroupBy(x => x.ClipId);
                    var groupedLayerClipDisplayLayers = layerClipDisplayLayers.GroupBy(x => x.ClipDisplayLayerId);

                    return new Video
                    {
                        Clips = clips.OrderBy(c => c.Order).Select(c =>
                        {
                            var clip = new Clip
                            {
                                ClipId = c.ClipId,
                                ClipName = c.ClipName,
                                BackgroundColour = c.BackgroundColour,
                                BeatLength = c.BeatLength,
                                StartingBeat = c.StartingBeat
                            };

                            return ClipHelper.HydrateClip(groupedClipDisplayLayers, groupedLayerClipDisplayLayers, clip);
                        }),
                        BPM = video.BPM,
                        Format = (Formats)video.FormatId,
                        VideoId = video.VideoId,
                        VideoName = video.VideoName,
                        VideoDelayMilliseconds = video.VideoDelayMilliseconds
                    };
                }

                return null;
            }
        }       

        public async Task DeleteAsync(int videoId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                await connection.ExecuteAsync("DeleteVideo", new { VideoId = videoId }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}