using Transcription.Models;

namespace Transcription.Api.Dapr
{
    public interface IDaprTranscriptionService
    {
        public Task<BlobBindingResponse> UploadFile(IFormFile file, CancellationToken cancellationToken);

        public Task<Uri> GetBlobSasToken(string url, string userAssignedClientId);

        public Task<StateEntry<TraduireTranscription>> UpdateState(string id, Uri url);

        public Task<TraduireTranscription> GetState(string id);

        public Task PublishEvent(string id, Uri url, CancellationToken cancellationToken);
    }
}
