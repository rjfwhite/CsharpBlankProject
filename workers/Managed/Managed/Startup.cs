using System;
using System.Reflection;
using Improbable.Worker;
using Newtonsoft.Json;
using Improbable;
using Newtonsoft.Json.Linq;

namespace Managed
{

    internal class Startup
    {

        private const string WorkerType = "Managed";

        private const string LoggerName = "Startup.cs";

        private const int ErrorExitStatus = 1;

        private const uint GetOpListTimeoutInMilliseconds = 100;

        private static int Main(string[] args)
        {
            var vec = new Position.Data(new PositionData(new Coordinates(1,2,3)));

            var output = JsonConvert.SerializeObject(vec);

            var data = JsonConvert.DeserializeObject<Position.Data>(output);

            foreach(var pair in Newtonsoft.Json.Linq.JObject.Parse(output))
            {
                System.Console.WriteLine(pair.Key);
                if(pair.Value is JObject)
                {
                    var obj = pair.Value as JObject;
                    foreach(var pair2 in obj)
                    {
                        System.Console.WriteLine(pair2.Key);
                        if (pair2.Value is JObject)
                        {
                            var obj2 = pair2.Value as JObject;
                            foreach (var pair3 in obj2)
                            {
                                System.Console.WriteLine(pair3.Key);
                            }
                        }
                    }
                }
            }

            System.Console.WriteLine(output);
            System.Console.WriteLine(vec.Value == data.Value);
            


            System.Console.ReadKey();

            return 0;

            if (args.Length != 4) {
                PrintUsage();
                return ErrorExitStatus;
            }

            // Avoid missing component errors because no components are directly used in this project
            // and the GeneratedCode assembly is not loaded but it should be
            Assembly.Load("GeneratedCode");

            var connectionParameters = new ConnectionParameters
            {
                WorkerType = WorkerType,
                Network =
                {
                    ConnectionType = NetworkConnectionType.Tcp
                }
            };

            using (var connection = ConnectWithReceptionist(args[1], Convert.ToUInt16(args[2]), args[3], connectionParameters))
            using (var dispatcher = new Dispatcher())
            {
                var isConnected = true;

                dispatcher.OnDisconnect(op =>
                {
                    Console.Error.WriteLine("[disconnect] " + op.Reason);
                    isConnected = false;
                });

                var view = new View();

                foreach(var entity in view.Entities)
                {
                    var d = entity.Value;
                    var type = Type.GetType("improbable.Position");
                }

                dispatcher.OnLogMessage(op =>
                {
                    connection.SendLogMessage(op.Level, LoggerName, op.Message);
                    if (op.Level == LogLevel.Fatal)
                    {
                        Console.Error.WriteLine("Fatal error: " + op.Message);
                        Environment.Exit(ErrorExitStatus);
                    }
                });

                while (isConnected)
                {
                    using (var opList = connection.GetOpList(GetOpListTimeoutInMilliseconds))
                    {
                        dispatcher.Process(opList);
                    }
                }
            }

            // This means we forcefully disconnected
            return ErrorExitStatus;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: mono Managed.exe receptionist <hostname> <port> <worker_id>");
            Console.WriteLine("Connects to SpatialOS");
            Console.WriteLine("    <hostname>      - hostname of the receptionist to connect to.");
            Console.WriteLine("    <port>          - port to use");
            Console.WriteLine("    <worker_id>     - name of the worker assigned by SpatialOS.");
        }

        private static Connection ConnectWithReceptionist(string hostname, ushort port,
            string workerId, ConnectionParameters connectionParameters)
        {
            Connection connection;

            // You might want to change this to true or expose it as a command-line option
            // if using `spatial cloud connect external` for debugging
            connectionParameters.Network.UseExternalIp = false;

            using (var future = Connection.ConnectAsync(hostname, port, workerId, connectionParameters))
            {
                connection = future.Get();
            }

            connection.SendLogMessage(LogLevel.Info, LoggerName, "Successfully connected using the Receptionist");

            return connection;
        }
    }
}