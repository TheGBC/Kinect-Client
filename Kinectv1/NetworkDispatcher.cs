using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kinectv1 {
  /// <summary>
  /// Utility class for making network requests
  /// </summary>
  class NetworkDispatcher {
    public static readonly string URL = "http://domain.com/page.aspx";

    private NetworkDispatcher() { }

    /// <summary>
    /// Synchronous GET HTTP request. Blocks the calling thread until a response is received.
    /// </summary>
    /// <param name="reqParams">Map of key to value params to attach to the request</param>
    /// <param name="url">The request url</param>
    /// <returns></returns>
    public static string syncGet(Dictionary<string, string> reqParams, string url) {
      string getUrl = url;
      // Buid the param string
      if (reqParams != null) {
        string paramData = "";
        foreach (KeyValuePair<string, string> entry in reqParams) {
          if (!paramData.Equals("")) {
            paramData += '&';
          }
          paramData += entry.Key + '=' + entry.Value;
        }
        getUrl += "?" + paramData;
      }
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
    public static string syncPost(Dictionary<string, string> reqParams, string url) {
      HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(url);
      ASCIIEncoding encoding = new ASCIIEncoding();
      // Build the param string
      string postData = "";
      if (reqParams != null) {
        foreach (KeyValuePair<string, string> entry in reqParams) {
          if (!postData.Equals("")) {
            postData += '&';
          }
          postData += entry.Key + '=' + entry.Value;
        }
      }
      byte[] data = encoding.GetBytes(postData);
      httpWReq.Method = "POST";
      httpWReq.ContentType = "application/x-www-form-urlencoded";
      httpWReq.ContentLength = data.Length;

      // Write post data to through the request stream
      using (Stream stream = httpWReq.GetRequestStream()) {
        stream.Write(data,0,data.Length);
      }

      // Get the response
      HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse();
      return new StreamReader(response.GetResponseStream()).ReadToEnd();
    }
  }
}
