using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using VideoDataAccess.DTOEntities;
using VideoDataAccess.Entities;
using VideoDataAccess.Helpers;

namespace VideoDataAccess.Repositories
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
                return await ReadClips(reader);
            }
        }

        private static async Task<IEnumerable<Clip>> ReadClips(SqlMapper.GridReader reader)
        {
            var clips = await reader.ReadAsync<Clip>();
            var clipDisplayLayers = await reader.ReadAsync<ClipDisplayLayerDTO>();
            var layerClipDisplayLayers = await reader.ReadAsync<LayerClipDisplayLayerDTO>();

            var groupedClipDisplayLayers = clipDisplayLayers.GroupBy(x => x.ClipId);
            var groupedLayerClipDisplayLayers = layerClipDisplayLayers.GroupBy(x => x.ClipDisplayLayerId);

            return clips.Select(x =>
            {
                return ClipHelper.HydrateClip(groupedClipDisplayLayers, groupedLayerClipDisplayLayers, x);
            });
        }

        public async Task<Clip> SaveAsync(Guid userObjectId, Clip clip)
        {
            var clipDisplayLayerDataTable = new DataTable();
            clipDisplayLayerDataTable.Columns.Add("TempId");
            clipDisplayLayerDataTable.Columns.Add("DisplayLayerId");
            clipDisplayLayerDataTable.Columns.Add("Reverse");
            clipDisplayLayerDataTable.Columns.Add("FlipHorizontal");
            clipDisplayLayerDataTable.Columns.Add("FlipVertical");
            clipDisplayLayerDataTable.Columns.Add("FadeTypeId");
            clipDisplayLayerDataTable.Columns.Add("Colour");
            clipDisplayLayerDataTable.Columns.Add("Order");

            var layerClipDisplayLayerDataTable = new DataTable();
            layerClipDisplayLayerDataTable.Columns.Add("ClipDisplayLayerId");
            layerClipDisplayLayerDataTable.Columns.Add("LayerId");
            layerClipDisplayLayerDataTable.Columns.Add("Colour");
            layerClipDisplayLayerDataTable.Columns.Add("EndColour");

            for (short i = 0; i < clip.ClipDisplayLayers.Count(); i++)
            {
                var clipDisplayLayer = clip.ClipDisplayLayers.ElementAt(i);
                clipDisplayLayerDataTable.Rows.Add(i, 
                    clipDisplayLayer.DisplayLayerId, 
                    clipDisplayLayer.Reverse, 
                    clipDisplayLayer.FlipHorizontal, 
                    clipDisplayLayer.FlipVertical, 
                    (short?)clipDisplayLayer.FadeType, 
                    clipDisplayLayer.Colour, 
                    i);
                foreach (var layerClipDisplayLayer in clipDisplayLayer.LayerClipDisplayLayers)
                {
                    layerClipDisplayLayerDataTable.Rows.Add(i, 
                        layerClipDisplayLayer.LayerId, 
                        layerClipDisplayLayer.Colour,
                        layerClipDisplayLayer.EndColour);
                }
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
                    ClipDisplayLayers = clipDisplayLayerDataTable.AsTableValuedParameter("ClipDisplayLayerType"),
                    LayerClipDisplayLayers = layerClipDisplayLayerDataTable.AsTableValuedParameter("LayerClipDisplayLayerType")
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
                    var clipDisplayLayers = await reader.ReadAsync<ClipDisplayLayerDTO>();
                    var layerClipDisplayLayers = await reader.ReadAsync<LayerClipDisplayLayerDTO>();

                    var groupedClipDisplayLayers = clipDisplayLayers.GroupBy(x => x.ClipId);
                    var groupedLayerClipDisplayLayers = layerClipDisplayLayers.GroupBy(x => x.ClipDisplayLayerId);
                    return ClipHelper.HydrateClip(groupedClipDisplayLayers, groupedLayerClipDisplayLayers, clip);                   
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

        public async Task<IEnumerable<Clip>> GetAllByVideoIdAsync(Guid userObjectId, int videoId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                var reader = await connection.QueryMultipleAsync("GetClipsByVideoId", new { userObjectId, VideoId = videoId }, commandType: CommandType.StoredProcedure);
                return await ReadClips(reader);
            }
        }
    }
}
