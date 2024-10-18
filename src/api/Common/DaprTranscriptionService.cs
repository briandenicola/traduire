using System.Net;
using Dapr.Client;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace Transcription.Api.Dapr
{
    public class DaprTranscriptionService : IDaprTranscriptionService
    {
        private string safeFileName;
        private static DaprClient _client;


        public DaprTranscriptionService(DaprClient client)
        {
            _client = client;
        }

        private static async Task<string> ConvertFileToBase64Encoding(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public async Task<BlobBindingResponse> UploadFile(IFormFile file, CancellationToken cancellationToken)
        {
            safeFileName = WebUtility.HtmlEncode(file.FileName);

            Dictionary<string,string> metadata = new()
            {
                { "blobName", safeFileName }
            };

            var encodedFile = await ConvertFileToBase64Encoding(file);

            return await _client.InvokeBindingAsync<string, BlobBindingResponse>(
                Components.BlobStoreName,
                Components.CreateOperation,
                encodedFile,
                metadata,
                cancellationToken
            );
        }

        public async Task<Uri> GetBlobSasToken(string url, string userAssignedClientId)
        {
            var uri = new Uri(url);
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId });
            var blobClient = new BlobServiceClient(new Uri($"https://{uri.Host}"), credential);
            var accountName = blobClient.AccountName;

            var delegationKey = await blobClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7));
            BlobSasBuilder sasBuilder = new()
            {
                BlobContainerName = uri.Segments[1].Trim('/'),
                BlobName = uri.Segments[2],
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var sasQueryParams = sasBuilder.ToSasQueryParameters(delegationKey, accountName).ToString();

            UriBuilder sasUri = new()
            {
                Scheme = "https",
                Host = uri.Host,
                Path = uri.AbsolutePath,
                Query = sasQueryParams
            };

            return sasUri.Uri;
        }

        public async Task<StateEntry<TraduireTranscription>> UpdateState(string id, Uri url)
        {
            var state = await _client.GetStateEntryAsync<TraduireTranscription>(Components.StateStoreName, id);

            state.Value ??= new TraduireTranscription()
            {
                TranscriptionId = new Guid(id),
                CreateTime = DateTime.UtcNow,
                LastUpdateTime = DateTime.UtcNow,
                Status = TraduireTranscriptionStatus.Started,
                FileName = safeFileName,
                BlobUri = url.ToString()
            };
            await state.SaveAsync();

            return state;
        }

        public async Task<TraduireTranscription> GetState(string id)
        {
            var state = await _client.GetStateEntryAsync<TraduireTranscription>(Components.StateStoreName, id);
            return state.Value;
        }

        public async Task PublishEvent(string id, Uri url, CancellationToken cancellationToken)
        {
            var eventdata = new TradiureTranscriptionRequest()
            {
                TranscriptionId = new Guid(id),
                BlobUri = url.ToString()
            };

            await _client.PublishEventAsync(Components.PubSubName, Topics.TranscriptionSubmittedTopicName, eventdata, cancellationToken);
        }
    }
}