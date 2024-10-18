using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Messaging.WebPubSub;

namespace Transcription.Common
{
    public class TraduireNotificationService
    {
        private readonly WebPubSubServiceClient _serviceClient;

        public TraduireNotificationService(WebPubSubServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
        }
        public async Task PublishNotification(string id, string message)
        {
            await _serviceClient.SendToUserAsync(id, RequestContent.Create(new
            {
                transcriptionId = id,
                statusMessage = message,
                lastUpdated = DateTime.UtcNow
            }
            ), ContentType.ApplicationJson);
        }
    }
}
