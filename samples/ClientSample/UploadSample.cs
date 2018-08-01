// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.CommandLineUtils;

namespace ClientSample
{
    internal class UploadSample
    {
        internal static void Register(CommandLineApplication app)
        {
            app.Command("uploading", cmd =>
            {
                cmd.Description = "Tests a streaming invocation from client to hub";

                CommandArgument baseUrlArgument = cmd.Argument("<BASEURL>", "The URL to the Chat Hub to test");

                cmd.OnExecute(() => ExecuteAsync(baseUrlArgument.Value));
            });
        }

        public static async Task<int> ExecuteAsync(string baseUrl)
        {
            HubConnection connection = new HubConnectionBuilder()
                .WithUrl(baseUrl)
                .Build();
            await connection.StartAsync();

            //await BasicInvoke(connection);
            //await ScoreTrackerExample(connection);
            //await FileUploadExample(connection);
            await StreamingEcho(connection);

            return 0;
        }

        public static async Task BasicInvoke(HubConnection connection)
        {
            Channel<string> channel = Channel.CreateUnbounded<string>();
            Task<string> invokeTask = connection.InvokeAsync<string>("UploadWord", channel.Reader);

            foreach (char c in "hello")
            {
                await channel.Writer.WriteAsync(c.ToString());
                await Task.Delay(1000);
            }
            channel.Writer.TryComplete();

            string result = await invokeTask;
            Debug.WriteLine($"You message was: {result}");
        }

        public static async Task ScoreTrackerExample(HubConnection connection)
        {
            //// We've got three players, first to 10 points wins
            //var player1 = Channel.CreateUnbounded<int>();
            //var player2 = Channel.CreateUnbounded<int>();

            //// unfortunately, all channels need to be top level parameters, so no nesting them

            //var pointGoal = 10;
            //var invocation = connection.InvokeAsync<string>("ScoreTracker", pointGoal, player1, player2);

            //while (!invocation.IsCompleted)
            //{
            //    await player1.Writer.WriteAsync(1);
            //}

            //Debug.WriteLine(await invocation);

            var channel_one = Channel.CreateBounded<int>(2);
            var channel_two = Channel.CreateBounded<int>(2);
            _ = WriteItemsAsync(channel_one.Writer, new[] { 2, 2, 3 });
            _ = WriteItemsAsync(channel_two.Writer, new[] { -2, 5, 3 });

            var result = await connection.InvokeAsync<string>("ScoreTracker", channel_one.Reader, channel_two.Reader);
            Debug.WriteLine(result);


            async Task WriteItemsAsync(ChannelWriter<int> source, IEnumerable<int> scores)
            {
                await Task.Delay(1000);
                foreach (var c in scores)
                {
                    await source.WriteAsync(c);
                    await Task.Delay(250);
                }

                // tryComplete triggers the end of this upload's relayLoop
                // which sends a StreamComplete to the server
                source.TryComplete();
            }
        }

        public static async Task FileUploadExample(HubConnection connection)
        {
            string fileNameSource = @"C:\Users\t-dygray\Pictures\weeg.jpg";
            string fileNameDest = @"C:\Users\t-dygray\Pictures\TargetFolder\weeg.jpg";

            Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
            Task<string> invocation = connection.InvokeAsync<string>("UploadFile", fileNameDest, channel.Reader);

            using (FileStream file = new FileStream(fileNameSource, FileMode.Open, FileAccess.Read))
            {
                foreach (byte[] chunk in GetChunks(file, kilobytesPerChunk: 5))
                {
                    await channel.Writer.WriteAsync(chunk);
                }
            }
            channel.Writer.TryComplete();

            Debug.WriteLine(await invocation);
        }

        public static IEnumerable<byte[]> GetChunks(FileStream fileStream, double kilobytesPerChunk)
        {
            int chunkSize = (int)kilobytesPerChunk * 1024;

            int position = 0;
            while (true)
            {
                if (position + chunkSize > fileStream.Length)
                {
                    byte[] lastChunk = new byte[fileStream.Length - position];
                    fileStream.Read(lastChunk, 0, lastChunk.Length);
                    yield return lastChunk;
                    break;
                }

                byte[] chunk = new byte[chunkSize];
                position += fileStream.Read(chunk, 0, chunk.Length);
                yield return chunk;
            }
        }

        public static async Task StreamingEcho(HubConnection connection)
        {
            var channel = Channel.CreateUnbounded<string>();

            _ = Task.Run(async () =>
            {
                foreach (var phrase in new[] { "one fish", "two fish", "red fish", "blue fish" })
                {
                    await channel.Writer.WriteAsync(phrase);
                }
            });

            var outputs = await connection.StreamAsChannelAsync<string>("StreamEcho", channel.Reader);

            while (await outputs.WaitToReadAsync())
            {
                while (outputs.TryRead(out var item))
                {
                    Debug.WriteLine($"received '{item}'.");
                }
            }
        }
    }
}

