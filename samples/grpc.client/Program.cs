using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

using Grpc.Net.Client;
using Grpc.Core;
using traduire.webapi;

namespace GrpcTraduireClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder().AddCommandLine(args);
            var config = builder.Build();
            
            var address = config["ApiServer"]; //"https://api.bjdazure.tech";
//            var apikey = config["ApiKey"];

            Console.WriteLine($"Connecting to {address}");

/*            var credentials = CallCredentials.FromInterceptor((context, metadata) =>
            {
                metadata.Add("apikey", apikey);
                return Task.CompletedTask;
            });


            var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
            });
*/        
            var channel = GrpcChannel.ForAddress(address);
            var client =  new Transcriber.TranscriberClient(channel);

            var replies = client.TranscribeAudioStream(
                new TranscriptionRequest {BlobUri = "https://traffic.libsyn.com/historyofrome/01-_In_the_Beginning.mp3"},
                deadline: DateTime.UtcNow.AddMinutes(10)
            );
            
            try 
            {
                await foreach (var streamreply in replies.ResponseStream.ReadAllAsync())
                {
                    Console.WriteLine("Transcription ID: " + streamreply.TranscriptionId);
                    Console.WriteLine("Create Time: " + streamreply.CreateTime);
                    Console.WriteLine("Last Update Time: " + streamreply.LastUpdateTime);
                    Console.WriteLine("Blob Uri: " + streamreply.BlobUri);
                    Console.WriteLine("Transcription Status: " + streamreply.Status);
                    Console.WriteLine("Transcription Text: " + streamreply.TranscriptionText);
                    Console.WriteLine();
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                Console.WriteLine("Transcription timed out after 10 minutes.");
            }
        }
    }
}
