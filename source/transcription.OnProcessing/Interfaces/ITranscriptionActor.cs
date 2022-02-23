using System.Net;
using System.Threading.Tasks;
using Dapr.Actors;

using transcription.models;
using transcription.common.cognitiveservices;

namespace transcription.actors {
    public interface ITranscriptionActor : IActor
    {
        Task SubmitAsync(string transcriptionId, string uri);
    }
}