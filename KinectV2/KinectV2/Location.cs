using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace KinectV2 {
  class Location {
    private string googleMapsAddress = "http://maps.googleapis.com/maps/api/geocode/json";
    private Dictionary<string, string> webParams = new Dictionary<string, string>();

    public float latitude;
    public float longitude;
    public bool success = true;

    public Location(string location) {
      webParams["sensor"] = "true";
      webParams["address"] = location;
      string res = NetworkDispatcher.SynchronizedGet(webParams, googleMapsAddress);
      Console.WriteLine(res);
      DataContractJsonSerializer deserialize = new DataContractJsonSerializer(typeof(ApiResponse));
      MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(res));
      ApiResponse apiResponseObject = deserialize.ReadObject(stream) as ApiResponse;
      stream.Close();

      if (apiResponseObject.results.Length > 0) {
        LatLng latlng = apiResponseObject.results[0].geometry.location;
        latitude = float.Parse(latlng.lat);
        longitude = float.Parse(latlng.lng);
      } else {
        success = false;
      }
    }

    public Location(string lat, string lng) {
      float? tLat = NumberParser.parseNumber(lat);
      float? tLng = NumberParser.parseNumber(lng);
      if (!tLat.HasValue || !tLng.HasValue) {
        success = false;
      } else {
        Console.WriteLine(tLat.Value + " " + tLng.Value);
        latitude = tLat.Value;
        longitude = tLng.Value;
      }
    }
  }
  [DataContract]
  class ApiResponse {
    [DataMember(Name = "results", IsRequired = false)]
    public Result[] results { get; set; }
    [DataMember(Name = "status", IsRequired = false)]
    public string status { get; set; }
  }

  [DataContract]
  class Result {
    [DataMember(Name = "address_components", IsRequired = false)]
    public AddressComponents[] address_components { get; set; }
    [DataMember(Name = "formatted_address", IsRequired = false)]
    public string formatted_address { get; set; }
    [DataMember(Name = "geometry", IsRequired = false)]
    public Geometry geometry { get; set; }
    [DataMember(Name = "types", IsRequired = false)]
    public string[] types { get; set; }
  }

  [DataContract]
  class AddressComponents {
    [DataMember(Name = "long_name", IsRequired = false)]
    public string long_name { get; set; }
    [DataMember(Name = "short_name", IsRequired = false)]
    public string short_name { get; set; }
    [DataMember(Name = "types", IsRequired = false)]
    public string[] types { get; set; }
  }

  [DataContract]
  class Geometry {
    [DataMember(Name = "bounds", IsRequired = false)]
    public Bounds bounds { get; set; }
    [DataMember(Name = "location", IsRequired = false)]
    public LatLng location { get; set; }
    [DataMember(Name = "location_type", IsRequired = false)]
    public string location_type { get; set; }
    [DataMember(Name = "viewport", IsRequired = false)]
    public Bounds viewport { get; set; }
  }

  [DataContract]
  class Bounds {
    [DataMember(Name = "northeast", IsRequired = false)]
    public LatLng northeast { get; set; }
    [DataMember(Name = "southwest", IsRequired = false)]
    public LatLng southwest { get; set; }
  }

  [DataContract]
  class LatLng {
    [DataMember(Name = "lat", IsRequired = false)]
    public string lat { get; set; }
    [DataMember(Name = "lng", IsRequired = false)]
    public string lng { get; set; }
  }
}
