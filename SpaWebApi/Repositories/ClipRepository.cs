using Dapper;
using LayerEntities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using VideoDataAccess.DTOEntities;
using VideoDataAccess.Entities;

namespace SpaWebApi.Repositories
{
    public class ClipRepository : IClipRepository
    {
        private readonly string _sqlConnection;

        public ClipRepository(IOptions<SqlConfig> connections)
        {
            _sqlConnection = connections.Value.DatabaseConnectionString;
        }

        public async Task<IEnumerable<Clip>> GetAllAsync(Guid userObjectId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                var reader = await connection.QueryMultipleAsync("GetClips", new { userObjectId }, commandType: CommandType.StoredProcedure);
                var clips = await reader.ReadAsync<Clip>();
                var userLayers = await reader.ReadAsync<ClipLayerDTO>();

                var groupedUserLayers = userLayers.GroupBy(x => x.ClipId);

                return clips.Select(x =>
                {
                    var groupedUserLayer = groupedUserLayers.FirstOrDefault(gu => gu.Key == x.ClipId);
                    if (groupedUserLayer != null)
                    {
                        x.Layers = groupedUserLayer.OrderBy(x => x.Order).Select(x => new Layer
                        {
                            LayerId = x.LayerId,
                            LayerName = x.LayerName
                        });
                    }
                    return x;
                });
            }
        }

        public async Task<Clip> SaveAsync(Guid userObjectId, Clip clip)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("ForeignId");
            dataTable.Columns.Add("Order");
            for (short i = 0; i < clip.Layers.Count(); i++)
            {
                dataTable.Rows.Add(clip.Layers.ElementAt(i).LayerId, i);
            }

            using (var connection = new SqlConnection(_sqlConnection))
            {
                clip.ClipId = await connection.QueryFirstAsync<int>("UpsertClip", new
                {
                    userObjectId,
                    clip.ClipId,
                    clip.ClipName,
                    clip.BackgroundColour,
                    clip.BeatLength,
                    clip.StartingBeat,
                    Layers = dataTable.AsTableValuedParameter("GuidOrderType")
                }, commandType: CommandType.StoredProcedure);
                return clip;
            }
        }

        public async Task<Clip?> GetAsync(Guid userObjectId, int clipId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                var reader = await connection.QueryMultipleAsync("GetClip", new { userObjectId, ClipId = clipId }, commandType: CommandType.StoredProcedure);
                var clip = await reader.ReadFirstOrDefaultAsync<Clip>();

                if (clip != null)
                {
                    var userLayers = await reader.ReadAsync<ClipLayerDTO>();

                    if (userLayers != null)
                    {
                        clip.Layers = userLayers.Select(x => new Layer
                        {
                            LayerId = x.LayerId,
                            LayerName = x.LayerName
                        });
                    }

                    return clip;
                }

                return null;
            }
        }

        public async Task DeleteAsync(int clipId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                await connection.ExecuteAsync("DeleteClip", new { ClipId = clipId }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
