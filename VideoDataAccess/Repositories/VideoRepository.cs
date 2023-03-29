using Dapper;
using LayerEntities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using VideoDataAccess.DTOEntities;
using VideoDataAccess.Entities;

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
                var userLayers = await reader.ReadAsync<ClipLayerDTO>();

                var groupedClips = clips.GroupBy(x => x.VideoId);
                var groupedLayers = userLayers.GroupBy(x => x.ClipId);

                return videos.Select(v => new Video
                {
                    Clips = groupedClips.First(gc => gc.Key == v.VideoId).OrderBy(gc => gc.Order).Select(gc =>
                    {
                        var clip = new Clip
                        {
                            ClipId = gc.ClipId,
                            ClipName = gc.ClipName,
                            BackgroundColour = gc.BackgroundColour,
                            BeatLength = gc.BeatLength,
                            StartingBeat = gc.StartingBeat
                        };

                        var groupedLayer = groupedLayers.FirstOrDefault(gu => gu.Key == gc.ClipId);
                        if (groupedLayer != null)
                        {
                            clip.Layers = groupedLayer.OrderBy(gu => gu.Order).Select(gu => new Layer
                            {
                                LayerId = gu.LayerId,
                                LayerName = gu.LayerName
                            });
                        }

                        return clip;
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
                    var layers = await reader.ReadAsync<ClipLayerDTO>();
                    var groupedLayers = layers.GroupBy(x => x.ClipId);

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

                            var groupedLayer = groupedLayers.FirstOrDefault(gu => gu.Key == c.ClipId);
                            if (groupedLayer != null)
                            {
                                clip.Layers = groupedLayer.OrderBy(gu => gu.Order).Select(gu => new Layer
                                {
                                    LayerId = gu.LayerId,
                                    LayerName = gu.LayerName
                                });
                            }
                            return clip;
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