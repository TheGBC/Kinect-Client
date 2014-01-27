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
      manager = new KinectManager("out.txt");
      Thread networkThread = new Thread(NetworkRun);
      networkThread.Start();
      Wait();
    }

    /// <summary>
    /// Blocks the program from closing
    /// </summary>
    static void Wait() {
      while (true) ;
    }

    static void NetworkRun() {
      Dictionary<string, string> map = new Dictionary<string,string>();
      map["kinectID"] = id;
      map["reqType"] = "SET_MATRIX";
      Thread.Sleep(5000);
      while (true) {
        Thread.Sleep(1000);
        if (manager == null) {
          continue;
        }
        string data = manager.SerializeCurrentTransformation();
        if (data != null) {
          map["matrix"] = data;
          Console.WriteLine(data);
          string resp = NetworkDispatcher.SynchronizedPost(map, "http://54.200.15.13:8080/ICP/post");
          Console.WriteLine(resp);
        } else {
          Console.WriteLine("NULL");
        }
      }
    }
  }
}
