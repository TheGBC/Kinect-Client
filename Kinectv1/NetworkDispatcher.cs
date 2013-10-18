using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kinectv1 {
  class NetworkDispatcher {
    public static readonly string URL = "http://domain.com/page.aspx";

    private NetworkDispatcher() { }

    public static string syncGet(Dictionary<string, string> reqParams, string url) {
      string getUrl = url;
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
      HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(getUrl);
      httpWReq.Method = "GET";
      HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse();
      return new StreamReader(response.GetResponseStream()).ReadToEnd();
    }

    public static string syncPost(Dictionary<string, string> reqParams, string url) {
      HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(url);
      ASCIIEncoding encoding = new ASCIIEncoding();
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

      using (Stream stream = httpWReq.GetRequestStream()) {
        stream.Write(data,0,data.Length);
      }

      HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse();
      return new StreamReader(response.GetResponseStream()).ReadToEnd();
    }
  }
}
