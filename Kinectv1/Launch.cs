using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kinectv1 {
  class Launch {
    static KinectManager manager;
    static string id = "1";
    static void Main(string[] args) {
      Thread kinectThread = new Thread(KinectRun);
      Thread netThread = new Thread(NetworkRun);
      kinectThread.Start();
      netThread.Start();
      Wait();
    }

    /// <summary>
    /// Blocks the program from closing
    /// </summary>
    static void Wait() {
      while (true) ;
    }

    static void KinectRun() {
      manager = new KinectManager();
    }

    static void NetworkRun() {
      Dictionary<string, string> map = new Dictionary<string,string>();
      map["kinectID"] = id;
      while (true) {
        Thread.Sleep(1000);
        if (manager == null) {
          continue;
        }
        string data = manager.SerializeCurrentFrame();
        if (data != null) {
          Console.WriteLine(data.Substring(0, 200));
          map["pointCloud"] = data;
          Console.WriteLine("Sending");
          int t = Environment.TickCount;
          string resp = NetworkDispatcher.SynchronizedPost(map, "http://54.200.15.13:8080/ICP/post");
          Console.WriteLine(resp);
          Console.WriteLine(Environment.TickCount - t);
        } else {
          Console.WriteLine("NULL");
        }
      }
    }
  }
}
