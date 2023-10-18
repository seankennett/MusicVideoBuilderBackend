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
                var videoClips = await reader.ReadAsync<VideoClipDTO>();

                var groupedClips = videoClips.GroupBy(x => x.VideoId);

                return videos.Select(v => new Video
                {
                    VideoClips = groupedClips.First(gc => gc.Key == v.VideoId).OrderBy(gc => gc.Order).Select(gc => new VideoClip { ClipId = gc.ClipId }),
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
            for (short i = 0; i < video.VideoClips.Count(); i++)
            {
                dataTable.Rows.Add(video.VideoClips.ElementAt(i).ClipId, i);
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
                    var videoClips = await reader.ReadAsync<VideoClipDTO>();
                    var groupedClips = videoClips.GroupBy(x => x.VideoId);

                    return new Video
                    {
                        VideoClips = videoClips.OrderBy(c => c.Order).Select(c => new VideoClip { ClipId = c.ClipId }),
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