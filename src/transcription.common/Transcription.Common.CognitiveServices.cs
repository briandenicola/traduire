using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace Transcription.Common.CognitiveServices
{
    public class AzureCognitiveServicesClient
    {
        private const int Port = 443;

        private const string SpeechToTextBasePath = "speechtotext/v3.1/";
        private HttpClient client;
        private string _azCognitiveServicesUri;

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters ={
                new JsonStringEnumConverter( JsonNamingPolicy.CamelCase)
            },
        };

        public AzureCognitiveServicesClient(string key, string region)
        {
            _azCognitiveServicesUri = $"{region}.api.cognitive.microsoft.com";
            client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(25),
                BaseAddress = new UriBuilder(Uri.UriSchemeHttps, _azCognitiveServicesUri, Port).Uri,
                DefaultRequestHeaders =
                {
                    { "Ocp-Apim-Subscription-Key", key }
                }
            };
        }

        public async Task<(Transcription, HttpStatusCode)> SubmitTranscriptionRequestAsync(Uri blob)
        {
            var request = new AzureCognitiveServicesTextToSpeechRequest();
            request.ContentUrls.Add(blob.AbsoluteUri);

            var res = JsonSerializer.Serialize(request, options);
            var content = new StringContent(res);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (var response = await client.PostAsync($"{SpeechToTextBasePath}transcriptions", content))
            {
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return (JsonSerializer.Deserialize<Transcription>(json, options), response.StatusCode);
                }

                return (null, response.StatusCode);
            }
        }
        public async Task<(Transcription, HttpStatusCode)> CheckTranscriptionRequestAsync(Uri location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var response = await client.GetAsync(location);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return (JsonSerializer.Deserialize<Transcription>(json, options), response.StatusCode);
            }

            return (null, response.StatusCode);
        }

        public async Task<(TranscriptionResults, HttpStatusCode)> DownloadTranscriptionResultAsync(Uri location)
        {
            TranscriptionFiles files;
            TranscriptionResults results;

            using (var response = await client.GetAsync(location))
            {
                if (!response.IsSuccessStatusCode)
                {
                    return (null, HttpStatusCode.BadRequest);
                }
                var json = await response.Content.ReadAsStringAsync();
                files = JsonSerializer.Deserialize<TranscriptionFiles>(json, options);
            }

            string contentUri = (from TranscriptionFilesValues file in files.Values
                                 where file.Kind == TranscriptionFileType.Transcription
                                 select file.Links.ContentUrl).FirstOrDefault();

            using (var response = await client.GetAsync(contentUri))
            {
                if (!response.IsSuccessStatusCode)
                {
                    return (null, HttpStatusCode.BadRequest);
                }
                var json = await response.Content.ReadAsStringAsync();
                results = JsonSerializer.Deserialize<TranscriptionResults>(json, options);

                return (results, HttpStatusCode.OK);
            }

        }
    }
}