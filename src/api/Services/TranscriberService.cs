
using Grpc.Core;

namespace Traduire.Webapi
{
    public class TranscriberService : Transcriber.TranscriberBase
    {
        private readonly int waitTime = 15;
        private readonly ILogger<TranscriberService> _logger;
        private static DaprTranscriptionService _client;

        public TranscriberService(ILogger<TranscriberService> logger, DaprTranscriptionService client)
        {
            _client = client;
            _logger = logger;
        }

        public override async Task TranscribeAudioStream(TranscriptionRequest req, IServerStreamWriter<TranscriptionReply> responseStream, ServerCallContext context)
        {
            var id = Guid.NewGuid().ToString();
            var createdTime = DateTime.UtcNow.ToString();

            var reply = new TranscriptionReply
            {
                TranscriptionId = id,
                CreateTime = createdTime,
                LastUpdateTime = createdTime,
                Status = nameof(TraduireTranscriptionStatus.Started),
                BlobUri = req.BlobUri,
                TranscriptionText = string.Empty
            };

            _logger.LogInformation($"Transcription request was received.");
            try
            {

                var state = await _client.UpdateState(id, new Uri(req.BlobUri) );
                _logger.LogInformation($"{id}. Transcription request was saved as to {Components.StateStoreName} State Store");
                await responseStream.WriteAsync(reply);

                await _client.PublishEvent(id, new Uri(req.BlobUri), context.CancellationToken) ;
                _logger.LogInformation($"{id}. {req.BlobUri} was published to {Components.PubSubName} pubsub store");
                reply.Status = nameof(TraduireTranscriptionStatus.SentToCognitiveServices);
                reply.LastUpdateTime = DateTime.UtcNow.ToString();
                await responseStream.WriteAsync(reply);

                TraduireTranscription currentState;
                do
                {
                    await Task.Delay(TimeSpan.FromSeconds(waitTime));

                    currentState = await _client.GetState(id);

                    _logger.LogInformation($"{id}. Transcription status is {currentState.Status}");
                    reply.Status = currentState.Status.ToString();
                    reply.LastUpdateTime = DateTime.UtcNow.ToString();
                    await responseStream.WriteAsync(reply);

                } while (currentState.Status != TraduireTranscriptionStatus.Completed);

                _logger.LogInformation($"{id}. Attempting to download completed transcription");
                reply.TranscriptionText = currentState.TranscriptionText;
                await responseStream.WriteAsync(reply);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to transcribe {req.BlobUri} - {ex.Message}");
                reply.Status = TraduireTranscriptionStatus.Failed.ToString();
                reply.LastUpdateTime = DateTime.UtcNow.ToString();
                await responseStream.WriteAsync(reply);
            }

        }
    }
}