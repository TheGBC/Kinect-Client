using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KinectV2 {
  /// <summary>
  /// Utility class for making network requests
  /// </summary>
  class NetworkDispatcher {
    private NetworkDispatcher() { }

    /// <summary>
    /// Synchronous GET HTTP request. Blocks the calling thread until a response is received.
    /// </summary>
    /// <param name="reqParams">Map of key to value params to attach to the request</param>
    /// <param name="url">The request url</param>
    /// <returns></returns>
    public static string SynchronizedGet(Dictionary<string, string> reqParams, string url) {
      string getUrl = url;
      // Buid the param string
      if (reqParams != null) {
        getUrl += "?" + paramString(reqParams);
      }
      Console.WriteLine("Getting " + getUrl);
      // Make the get request
      HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(getUrl);
      httpWReq.Method = "GET";
      HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse();
      return new StreamReader(response.GetResponseStream()).ReadToEnd();
    }

    /// <summary>
    /// Synchronous POST HTTP request. Blocks the calling thread until a response is received.
    /// </summary>
    /// <param name="reqParams">Map of key to value params to attach to the request</param>
    /// <param name="url">The request url</param>
    /// <returns></returns>
    public static string SynchronizedPost(Dictionary<string, string> reqParams, string url) {
      HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(url);
      httpWReq.Timeout = 5 * 60 * 1000;
      ASCIIEncoding encoding = new ASCIIEncoding();
      string postData = paramString(reqParams);
      byte[] data = encoding.GetBytes(postData);
      httpWReq.Method = "POST";
      httpWReq.ContentType = "application/x-www-form-urlencoded";
      httpWReq.ContentLength = data.Length;
      Console.WriteLine("Sending data");
      // Write post data to through the request stream
      using (Stream stream = httpWReq.GetRequestStream()) {
        stream.Write(data,0,data.Length);
      }
      Console.WriteLine("Wrote data");
      // Get the response
      HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse();
      Console.WriteLine("Reading Response");
      return new StreamReader(response.GetResponseStream()).ReadToEnd();
    }

    // Builds the param string
    private static string paramString(Dictionary<string, string> reqParams) {
      string paramString = "";
      if (reqParams == null) {
        return paramString;
      }
      foreach (KeyValuePair<string, string> entry in reqParams) {
        if (!paramString.Equals("")) {
          paramString += '&';
        }
        paramString += entry.Key + '=' + entry.Value;
      }
      return paramString;
    }
  }
}
