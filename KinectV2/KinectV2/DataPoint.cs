using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectV2 {
  class DataPoint {
    public float lat;
    public float lng;
    public float value;

    public DataPoint(float lat, float lng, float value) {
      this.lat = lat;
      this.lng = lng;
      this.value = value;
    }
  }
}
