using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Dapr;
using Dapr.Client;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

using transcription.models;

namespace transcription.api.dapr
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
