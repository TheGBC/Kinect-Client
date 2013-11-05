using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kinectv1 {
  class Launch {
    static string id = "1";
    static void Main(string[] args) {
      Thread thread = new Thread(Run);
      thread.Start();
      Wait();
    }

    /// <summary>
    /// Blocks the program from closing
    /// </summary>
    static void Wait() {
      while (true) ;
    }

    static void Run() {
      KinectManager manager = new KinectManager();
      Dictionary<string, string> map = new Dictionary<string,string>();
      map["kinectID"] = id;
      while (true) {
        Thread.Sleep(1000);
        string data = manager.SerializeCurrentFrame();
        if (data != null) {
          map["pointCloud"] = data;
          map["pointCloudWidth"] = "320";
          map["pointCloudHeight"] = "240";
          NetworkDispatcher.SynchronizedPost(map, "");
          Console.WriteLine("Sent frame");
        }
      }
    }
  }
}
