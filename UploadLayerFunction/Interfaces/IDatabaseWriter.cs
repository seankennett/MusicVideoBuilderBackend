using SharedEntities.Models;
using System.Threading.Tasks;

namespace UploadLayerFunction.Interfaces
{
    public interface IDatabaseWriter
    {
        Task InsertLayer(LayerUploadMessage layerUploadMessage);
    }
}