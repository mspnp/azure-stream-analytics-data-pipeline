namespace taxi
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;
    using Newtonsoft.Json;

    class Program
    {
        private static async Task ReadData<T>(string path, Func<string, T> factory,
            EventHubClient client, int randomSeed, AsyncConsole console, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"{nameof(path)} cannot be null, empty, or only whitespace");
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException($"File '{path}' does not exist");
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (console == null)
            {
                throw new ArgumentNullException(nameof(console));
            }

            string typeName = typeof(T).Name;
            Random random = new Random(randomSeed);
            ZipArchive archive = new ZipArchive(
                File.OpenRead(path),
                ZipArchiveMode.Read);
            //Console.WriteLine(archive.Entries.Count);
            foreach (var entry in archive.Entries)
            {
                using (var reader = new StreamReader(entry.Open()))
                {
                    int lines = 0;
                    var batches = reader.ReadLines()
                        .Skip(1)
                        .Select(s => {
                            lines++;
                            return new EventData(Encoding.UTF8.GetBytes(
							    JsonConvert.SerializeObject(factory(s))));
                        })
                        .Partition();
                    int i = 0;
                    foreach (var batch in batches)
                    {
                        // Wait for a random interval to introduce some delays.
                        await Task.Delay(random.Next(100, 1000))
                            .ConfigureAwait(false);
                        await client.SendAsync(batch)
                            .ConfigureAwait(false);
                        if (++i % 10 == 0)
                        {
                            await console.WriteLine($"{typeName} lines consumed: {lines}")
                                .ConfigureAwait(false);
                            await console.WriteLine($"Created {i} {typeName} batches")
                                .ConfigureAwait(false);
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                    }

                    await console.WriteLine($"Created {i} total {typeName} batches")
                        .ConfigureAwait(false);
                }
            }
        }

        private static (string RideConnectionString,
                        string FareConnectionString,
                        string RideDataFile,
                        string FareDataFile,
                        int MillisecondsToRun) ParseArguments(string[] args)
        {
            // Do simple command line parsing so we don't need an external library
            if (args.Length < 4)
            {
                throw new ArgumentException(
                    "USAGE: DataLoader <ride-event-hub-connection-string> <fare-event-hub-connection-string> <ride-filename> <fare-filename> [number-of-seconds-to-run]");
            }

            var rideConnectionString = args[0];
            var fareConnectionString = args[1];
            var rideDataFile = args[2];
            var fareDataFile = args[3];
            var numberOfMillisecondsToRun = (args.Length == 5 ? int.TryParse(args[4], out int temp) ? temp : 0 : 0) * 1000;

            if (string.IsNullOrWhiteSpace(rideConnectionString))
            {
                throw new ArgumentException("rideConnectionString must be provided");
            }

            if (string.IsNullOrWhiteSpace(fareConnectionString))
            {
                throw new ArgumentException("rideConnectionString must be provided");
            }

            if (string.IsNullOrWhiteSpace(rideDataFile))
            {
                throw new ArgumentException("rideDataFile must be provided");
            }

            if (!File.Exists(rideDataFile))
            {
                throw new ArgumentException($"Ride data file {rideDataFile} does not exist");
            }

            if (string.IsNullOrWhiteSpace(fareDataFile))
            {
                throw new ArgumentException("fareDataFile must be provided");
            }

            if (!File.Exists(fareDataFile))
            {
                throw new ArgumentException($"Fare data file {fareDataFile} does not exist");
            }

            return (rideConnectionString, fareConnectionString, rideDataFile, fareDataFile, numberOfMillisecondsToRun);
        }

        private class AsyncConsole
        {
            private BlockingCollection<string> _blockingCollection = new BlockingCollection<string>();
            private CancellationToken _cancellationToken;
            private Task _writerTask;

            public AsyncConsole(CancellationToken cancellationToken = default(CancellationToken))
            {
                _cancellationToken = cancellationToken;
                _writerTask = Task.Factory.StartNew((state) => {
                    var token = (CancellationToken)state;
                    string msg;
                    while (!token.IsCancellationRequested)
                    {
                        if (_blockingCollection.TryTake(out msg, 500))
                        {
                            Console.WriteLine(msg);
                        }
                    }

                    while (_blockingCollection.TryTake(out msg, 100))
                    {
                        Console.WriteLine(msg);
                    }
                }, _cancellationToken, TaskCreationOptions.LongRunning);
            }

            public Task WriteLine(string toWrite)
            {
                _blockingCollection.Add(toWrite);
                return Task.FromResult(0);
            }

            public Task WriterTask
            {
                get { return _writerTask; }
            }
        }
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var arguments = ParseArguments(args);
                var rideClient = EventHubClient.CreateFromConnectionString(
                    arguments.RideConnectionString
                );
                var fareClient = EventHubClient.CreateFromConnectionString(
                    arguments.FareConnectionString
                );

                CancellationTokenSource cts = arguments.MillisecondsToRun == 0 ? new CancellationTokenSource() :
                    new CancellationTokenSource(arguments.MillisecondsToRun);
                Console.CancelKeyPress += (s, e) => {
                    //Console.WriteLine("Cancelling data generation");
                    cts.Cancel();
                    e.Cancel = true;
                };
                AsyncConsole console = new AsyncConsole(cts.Token);
                var rideTask = ReadData<TaxiRide>(arguments.RideDataFile,
                    TaxiRide.FromString, rideClient, 100, console, cts.Token);
                var fareTask = ReadData<TaxiFare>(arguments.FareDataFile,
                    TaxiFare.FromString, fareClient, 200, console, cts.Token);
                await Task.WhenAll(rideTask, fareTask, console.WriterTask);
                Console.WriteLine("Data generation complete");
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine(ae.Message);
                return 1;
            }

            return 0;
        }
    }
}
