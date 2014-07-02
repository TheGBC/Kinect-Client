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

    public static void Normalize(DataPoint[] points) {
      float min = float.MaxValue;
      float max = float.MinValue;
      foreach (DataPoint point in points) {
        if (point.value > max) {
          max = point.value;
        }
        if (point.value < min) {
          min = point.value;
        }
      }

      foreach (DataPoint point in points) {
        point.value -= min;
        point.value /= max;
      }
    }
  }
}
