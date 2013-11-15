using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kinectv1 {
  /// <summary>
  /// Manager for grabbing point clouds
  /// </summary>
  class KinectManager {
    private KinectSensor sensor;
    private CoordinateMapper mapper;

    // Locks to prevent untimely access to resources
    private bool frameReady = true;
    private object frameLock = new object();

    // Formats for Depth and Color
    private readonly DepthImageFormat DEPTH_FORMAT = DepthImageFormat.Resolution80x60Fps30;
    private readonly ColorImageFormat COLOR_FORMAT = ColorImageFormat.RgbResolution640x480Fps30;

    private List<Point3D> points = new List<Point3D>();

    public KinectManager() {
      foreach (KinectSensor potentialSensor in KinectSensor.KinectSensors) {
        if (potentialSensor.Status == KinectStatus.Connected) {
          sensor = potentialSensor;
          break;
        }
      }

      if (sensor == null) {
        Console.WriteLine("Can't find Kinect Sensor");
        return;
      }
      mapper = new CoordinateMapper(sensor);

      sensor.DepthStream.Enable(DEPTH_FORMAT);
      sensor.ColorStream.Enable(COLOR_FORMAT);

      sensor.AllFramesReady += onFrameReady;
      sensor.Start();
    }

    /// <summary>
    /// Returns the json array of the points serialized as a string
    /// </summary>
    /// <returns>json array serialized as a string, or null if current frame is empty</returns>
    public string SerializeCurrentFrame() {
      StringBuilder builder = new StringBuilder();
      Monitor.Enter(frameLock);
      if (points.Count == 0) {
        return null;
      }
      builder.Append('[');
      int pointCnt = 0;
      foreach (Point3D point in points) {
        if (pointCnt++ > 0) {
          builder.Append(',');
        }
        builder.Append(point.X);
        builder.Append(',');
        builder.Append(point.Y);
        builder.Append(',');
        builder.Append(point.Z);
      }
      builder.Append(']');
      Monitor.Exit(frameLock);
      return builder.ToString();
    }

    /// <summary>
    /// Gets the current frame, or null if no frames have been captured
    /// </summary>
    public Point3D[] CurrentPointCloud {
      get {
        Point3D[] res = null;
        Monitor.Enter(frameLock);
        if (points.Count > 0) {
          res = new Point3D[points.Count];
          for (int i = 0; i < points.Count; i++) {
            res[i] = points[i];
          }
        }
        Monitor.Exit(frameLock);
        return res;
      }
    }

    // When both color and depth frames are ready
    private void onFrameReady(object sender, AllFramesReadyEventArgs e) {
      // Only get the frames when we're done processing the previous one, to prevent
      // frame callbacks from piling up
      if (frameReady) {
        // Enter a context with both the depth and color frames
        using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
        using (ColorImageFrame colorFrame = e.OpenColorImageFrame()) {
          // Lock resources and prevent further frame callbacks
          frameReady = false;
          Monitor.Enter(frameLock);

          // Init
          SkeletonPoint[] realPoints = new SkeletonPoint[depthFrame.PixelDataLength];
          DepthImagePixel[] depth = new DepthImagePixel[depthFrame.PixelDataLength];

          // Clear the coordinates from the previous frame
          points.Clear();

          // Obtain raw data from frames
          depthFrame.CopyDepthImagePixelDataTo(depth);

          // Map depth to real world skeleton points
          mapper.MapDepthFrameToSkeletonFrame(DEPTH_FORMAT, depth, realPoints);

          // Select the points that are within range and add them to coordinates
          for (int i = 0; i < realPoints.Length; i++) {
            if (depth[i].Depth >= 1800 || depth[i].Depth <= depthFrame.MinDepth) {
              continue;
            }

            Point3D point = new Point3D();
            point.X = realPoints[i].X;
            point.Y = realPoints[i].Y;
            point.Z = realPoints[i].Z;
            points.Add(point);
          }
          // Release resources, now ready for next callback
          Monitor.Exit(frameLock);
          frameReady = true;
        }
      }
    }
  }
}
