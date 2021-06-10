﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Core;
using traduire.webapi;

namespace GrpcTraduireClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var address = "https://api.bjdazure.tech";
            var apikey = "";

            var credentials = CallCredentials.FromInterceptor((context, metadata) =>
            {
                metadata.Add("apikey", apikey);
                return Task.CompletedTask;
            });

            var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
            });
        
            var client =  new Transcriber.TranscriberClient(channel);

            var replies = client.TranscribeAudioStream(new TranscriptionRequest { 
                BlobUri = "https://traffic.libsyn.com/historyofrome/01-_In_the_Beginning.mp3"
            });
            
            await foreach (var streamreply in replies.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine("Transcription ID: " + streamreply.TranscriptionId);
                Console.WriteLine("Create Time: " + streamreply.CreateTime);
                Console.WriteLine("Create Time: " + streamreply.LastUpdateTime);
                Console.WriteLine("Blob Uri: " + streamreply.BlobUri);
                Console.WriteLine("Transcription Status: " + streamreply.Status);
                Console.WriteLine("Transcription Text: " + streamreply.TranscriptionText);
                Console.WriteLine();
            }
        }
    }
}