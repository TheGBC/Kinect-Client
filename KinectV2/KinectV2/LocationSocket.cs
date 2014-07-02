using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace KinectV2 {
  class LocationSocket {
    private static readonly string LOCALHOST = "127.0.0.1";
    private static readonly int PORT = 8000;

    private static GlobeModel model;
    private static Thread thread;

    private LocationSocket() { }

    public static void start(GlobeModel model) {
      if (thread == null || !thread.IsAlive) {
        LocationSocket.model = model;
        thread = new Thread(new ThreadStart(run));
        thread.Start();
      }
    }

    private static void run() {
      try {
        TcpClient socketClient = new TcpClient();

        Console.WriteLine("Connecting to phone...");
        socketClient.Connect(new IPEndPoint(IPAddress.Parse(LOCALHOST), PORT));
        StreamReader reader = new StreamReader(socketClient.GetStream());
        Console.WriteLine("Connection Established");

        while (!reader.EndOfStream) {
          string line = reader.ReadLine();
          Console.WriteLine("Response: {0}", line);
          DataContractJsonSerializer deserialize = new DataContractJsonSerializer(typeof(Response));
          MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(line));
          Response response = deserialize.ReadObject(stream) as Response;

          Console.WriteLine("Parsed Response: {0} {1} {2}", response.latitude, response.longitude, response.location);
          Location location;
          if (response.location == "") {
            location = new Location(response.latitude, response.longitude);
          } else {
            location = new Location(response.location);
          }
          if (location != null && location.success) {
            model.rotateTo(location.latitude, location.longitude);
          }
        }
        reader.Close();
        socketClient.Close();
      } catch (Exception e) {
        Console.WriteLine(e.Message);
        Console.WriteLine(e.StackTrace);
      }
    }

    [DataContract]
    class Response {
      [DataMember(Name = "latitude", IsRequired = false)]
      public string latitude { get; set; }
      [DataMember(Name = "longitude", IsRequired = false)]
      public string longitude { get; set; }
      [DataMember(Name = "location", IsRequired = false)]
      public string location { get; set; }
    }
  }
}
