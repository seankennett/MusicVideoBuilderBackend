﻿using CollectionEntities.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PublicDataApiFunction.DTOEntities;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PublicDataApiFunction.Repositories
{
    public class CollectionRepository : ICollectionRepository
    {
        private readonly string _sqlConnection;
        private readonly IDirectionRepository _directionRepository;

        public CollectionRepository(IOptions<SqlConfig> connections, IDirectionRepository directionRepository)
        {
            _sqlConnection = connections.Value.DatabaseConnectionString;
            _directionRepository = directionRepository;
        }

        public async Task<IEnumerable<Collection>> GetAllCollectionsAsync()
        {
            var directions = await _directionRepository.GetAllDirections();
            using (var connection = new SqlConnection(_sqlConnection))
            {
                var reader = await connection.QueryMultipleAsync("GetCollections", commandType: CommandType.StoredProcedure);
                var collections = await reader.ReadAsync<CollectionDTO>();
                var displayLayers = await reader.ReadAsync<DisplayLayerDTO>();
                var layers = await reader.ReadAsync<LayerDTO>();
                var collectionDisplayLayers = await reader.ReadAsync<CollectionDisplayLayerDTO>();
                var layerCollectionDisplayLayers = await reader.ReadAsync<LayerCollectionDisplayLayerDTO>();


                var groupedDisplayLayers = displayLayers.GroupBy(x => x.CollectionId);
                var groupedLayers = layers.GroupBy(x => x.DisplayLayerId);
                var groupedLayerCollectionDisplayLayers = layerCollectionDisplayLayers.GroupBy(x => x.DisplayLayerId);

                return collections.Select(l => new Collection
                {
                    CollectionId = l.CollectionId,
                    CollectionName = l.CollectionName,
                    CollectionType = (CollectionType)l.CollectionTypeId,
                    CollectionDisplayLayer = new CollectionDisplayLayer
                    {
                        DisplayLayerId = collectionDisplayLayers.First(x => x.CollectionId == l.CollectionId).DisplayLayerId,
                        LayerCollectionDisplayLayers = groupedLayerCollectionDisplayLayers.First(lc => lc.Key == collectionDisplayLayers.First(x => x.CollectionId == l.CollectionId).DisplayLayerId).Select(cd => new LayerCollectionDisplayLayer
                        {
                            Colour = cd.Colour,
                            LayerId = cd.LayerId
                        })
                    },
                    DisplayLayers = groupedDisplayLayers.First(x => x.Key == l.CollectionId).Select(d => new DisplayLayer
                    {
                        DisplayLayerId = d.DisplayLayerId,
                        Layers = groupedLayers.First(l => l.Key == d.DisplayLayerId).OrderByDescending(l => l.IsOverlay).ThenBy(l => l.Order).Select(l => new Layer
                        {
                            IsOverlay = l.IsOverlay,
                            LayerId = l.LayerId
                        }),
                        Direction = directions.First(dr => dr.DirectionId == d.DirectionId),
                        LinkedPreviousDisplayLayerId = d.LinkedPreviousDisplayLayerId,
                        NumberOfSides = d.NumberOfSides
                    })
                });
            }
        }
    }
}
