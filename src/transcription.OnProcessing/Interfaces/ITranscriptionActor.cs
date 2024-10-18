using Dapr.Actors;

namespace Transcription.Actors
{
    public interface ITranscriptionActor : IActor
    {
        Task SubmitAsync(string transcriptionId, string uri);
    }
}