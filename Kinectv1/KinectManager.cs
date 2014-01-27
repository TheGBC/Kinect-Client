using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Fusion;
using System;
using System.Collections.Generic;
using System.IO;
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

    // Locks to prevent untimely access to resources
    private bool frameReady = true;
    private object frameLock = new object();

    // Formats for Depth and Color
    private readonly DepthImageFormat DEPTH_FORMAT = DepthImageFormat.Resolution640x480Fps30;
    private readonly ColorImageFormat COLOR_FORMAT = ColorImageFormat.RgbResolution640x480Fps30;

    private ColorReconstruction volume;

    private Matrix4 currentMatrix = Matrix4.Identity;

    private readonly int VOXEL_RESOLUTION = 256;
    private readonly int X_VOXELS = 256;
    private readonly int Y_VOXELS = 384;
    private readonly int Z_VOXELS = 256;

    private CameraPoseFinder poseFinder = CameraPoseFinder.FusionCreateCameraPoseFinder(CameraPoseFinderParameters.Defaults);

    private byte[] color = new byte[640 * 480 * 4];

    private Matrix4 worldToCameraTransform = Matrix4.Identity;

    public KinectManager(string modelData) {
      poseFinder.LoadCameraPoseFinderDatabase("poseFinder.txt");
      FileStream stream = System.IO.File.OpenRead(modelData);
      // Open bracket
      char ch = (char) stream.ReadByte();

      short[] modelVolumeData = new short[X_VOXELS * Y_VOXELS * Z_VOXELS];

      StringBuilder b = new StringBuilder();
      for (int i = 0; i < modelVolumeData.Length; i++) {
        ch = (char)stream.ReadByte();
        while (ch != ']' && ch != ',') {
          b.Append(ch);
          ch = (char)stream.ReadByte();
        }
        modelVolumeData[i] = short.Parse(b.ToString());
        b.Clear();
        if (i % 100000 == 0) {
          Console.WriteLine(i);
        }
      }

      /*
      string str = System.IO.File.ReadAllText(modelData).Trim();
      str = str.Substring(1, str.Length - 2);
      string[] parts = str.Split(',');
      short[] modelVolumeData = new short[parts.Length];
      for (int i = 0; i < parts.Length; i++) {
        modelVolumeData[i] = short.Parse(parts[i]);
      }*/

      ReconstructionParameters rParams = new ReconstructionParameters(VOXEL_RESOLUTION, X_VOXELS, Y_VOXELS, Z_VOXELS);
      volume = ColorReconstruction.FusionCreateReconstruction(rParams, ReconstructionProcessor.Amp, -1, Matrix4.Identity);
      volume.ImportVolumeBlock(modelVolumeData);

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

      sensor.DepthStream.Enable(DEPTH_FORMAT);
      sensor.ColorStream.Enable(COLOR_FORMAT);
      sensor.AllFramesReady += onFrameReady;
      sensor.Start();
    }

    public byte[] ImageData {
      get {
        byte[] data = null;
        Monitor.Enter(frameLock);
        if (color != null) {
          data = new byte[color.Length];
          for (int i = 0; i < color.Length; i++) {
            data[i] = color[i];
          }
        }
        Monitor.Exit(frameLock);
        return data;
      }
    }

    /// <summary>
    /// Returns the 1d array of the current matrix, or null
    /// </summary>
    /// <returns>json array serialized as a string, or null if current frame is empty</returns>
    public string SerializeCurrentTransformation() {
      StringBuilder builder = new StringBuilder();
      Monitor.Enter(frameLock);
      builder.Append('[');
      builder.Append(currentMatrix.M11 + "," + currentMatrix.M12 + "," + currentMatrix.M13 + "," + currentMatrix.M41 + ",");
      builder.Append(currentMatrix.M21 + "," + currentMatrix.M22 + "," + currentMatrix.M23 + "," + -currentMatrix.M42 + ",");
      builder.Append(currentMatrix.M31 + "," + currentMatrix.M32 + "," + currentMatrix.M33 + "," + -currentMatrix.M43 + ",");
      builder.Append(currentMatrix.M14 + "," + currentMatrix.M24 + "," + currentMatrix.M34 + "," + currentMatrix.M44);
      builder.Append(']');
      Monitor.Exit(frameLock);
      return builder.ToString();
    }

    // When both color and depth frames are ready
    private void onFrameReady(object sender, AllFramesReadyEventArgs e) {
      // Only get the frames when we're done processing the previous one, to prevent
      // frame callbacks from piling up
      if (frameReady) {

        Monitor.Enter(frameLock);
        // Enter a context with both the depth and color frames
        using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
        using (ColorImageFrame colorFrame = e.OpenColorImageFrame()) {
          // Lock resources and prevent further frame callbacks
          frameReady = false;

          // Init
          DepthImagePixel[] depth = new DepthImagePixel[depthFrame.PixelDataLength];
          
          // Obtain raw data from frames
          depthFrame.CopyDepthImagePixelDataTo(depth);
          colorFrame.CopyPixelDataTo(color);

          // Construct into a fusionfloatimageframe
          FusionFloatImageFrame frame = new FusionFloatImageFrame(640, 480);
          FusionFloatImageFrame smoothDepthFloatFrame = new FusionFloatImageFrame(640, 480);
          FusionDepthProcessor.DepthToDepthFloatFrame(depth, 640, 480, frame, FusionDepthProcessor.DefaultMinimumDepth, 2.5f, false);

          this.volume.SmoothDepthFloatFrame(frame, smoothDepthFloatFrame, 1, .04f);

          /*
          byte[] pixelData = new byte[4 * 640 * 480];

          colorFrame.CopyPixelDataTo(pixelData);
          
          int[] intPixelData = new int[640 * 480];
          Buffer.BlockCopy(pixelData, 0, intPixelData, 0, 640 * 480 * sizeof(int));
          FusionColorImageFrame imageFrame = new FusionColorImageFrame(640, 480);
          imageFrame.CopyPixelDataFrom(intPixelData);

          bool success = FindCameraPoseAlignPointClouds(frame, imageFrame);

          Matrix4 t = worldToCameraTransform;
          Console.WriteLine("{0} {1} {2} {3}", t.M11, t.M12, t.M13, t.M14);
          Console.WriteLine("{0} {1} {2} {3}", t.M21, t.M22, t.M23, t.M24);
          Console.WriteLine("{0} {1} {2} {3}", t.M31, t.M32, t.M33, t.M34);
          Console.WriteLine("{0} {1} {2} {3}", t.M41, t.M42, t.M43, t.M44);
          if (success) {
            currentMatrix = t;
          }*/


          // Calculate point cloud
          FusionPointCloudImageFrame observedPointCloud = new FusionPointCloudImageFrame(640, 480);
          FusionDepthProcessor.DepthFloatFrameToPointCloud(smoothDepthFloatFrame, observedPointCloud);

          
          FusionPointCloudImageFrame imgFrame = new FusionPointCloudImageFrame(640, 480);
          volume.CalculatePointCloud(imgFrame, currentMatrix);
          Matrix4 t = currentMatrix;
          float alignmentEnergy = 0;
          bool success = volume.AlignPointClouds(imgFrame, observedPointCloud, 100, null, out alignmentEnergy, ref t);

          Console.WriteLine(success);
          Console.WriteLine("{0}", alignmentEnergy);
          Console.WriteLine("{0} {1} {2} {3}", t.M11, t.M12, t.M13, t.M14);
          Console.WriteLine("{0} {1} {2} {3}", t.M21, t.M22, t.M23, t.M24);
          Console.WriteLine("{0} {1} {2} {3}", t.M31, t.M32, t.M33, t.M34);
          Console.WriteLine("{0} {1} {2} {3}", t.M41, t.M42, t.M43, t.M44);
          if (success) {
            currentMatrix = t;
          }

          frame.Dispose();
          smoothDepthFloatFrame.Dispose();
          observedPointCloud.Dispose();
          imgFrame.Dispose();

          // Release resources, now ready for next callback
          frameReady = true;
        }
        Monitor.Exit(frameLock);
      }
    }

    /*
    /// <summary>
    /// Perform camera pose finding when tracking is lost using AlignPointClouds.
    /// This is typically more successful than FindCameraPoseAlignDepthFloatToReconstruction.
    /// </summary>
    /// <returns>Returns true if a valid camera pose was found, otherwise false.</returns>
    private bool FindCameraPoseAlignPointClouds(FusionFloatImageFrame floatFrame, FusionColorImageFrame imageFrame) {
      MatchCandidates matchCandidates = poseFinder.FindCameraPose(floatFrame, imageFrame);

      if (null == matchCandidates) {
        return false;
      }

      int poseCount = matchCandidates.GetPoseCount();
      float minDistance = matchCandidates.CalculateMinimumDistance();

      if (0 == poseCount || minDistance >= 1) {
        return false;
      }

      FusionFloatImageFrame smoothDepthFloatFrame = new FusionFloatImageFrame(640, 480);
      FusionPointCloudImageFrame depthPointCloudFrame = new FusionPointCloudImageFrame(640, 480);
      FusionPointCloudImageFrame raycastPointCloudFrame = new FusionPointCloudImageFrame(640, 480);
      
      // Smooth the depth frame
      this.volume.SmoothDepthFloatFrame(floatFrame, smoothDepthFloatFrame, 1, .04f);

      // Calculate point cloud from the smoothed frame
      FusionDepthProcessor.DepthFloatFrameToPointCloud(smoothDepthFloatFrame, depthPointCloudFrame);

      double smallestEnergy = double.MaxValue;
      int smallestEnergyNeighborIndex = -1;

      int bestNeighborIndex = -1;
      Matrix4 bestNeighborCameraPose = Matrix4.Identity;

      double bestNeighborAlignmentEnergy = 0.006f;

      // Run alignment with best matched poseCount (i.e. k nearest neighbors (kNN))
      int maxTests = Math.Min(5, poseCount);

      var neighbors = matchCandidates.GetMatchPoses();

      float alignmentEnergy;
      for (int n = 0; n < maxTests; n++) {
        // Run the camera tracking algorithm with the volume
        // this uses the raycast frame and pose to find a valid camera pose by matching the raycast against the input point cloud
        Matrix4 poseProposal = neighbors[n];

        // Get the saved pose view by raycasting the volume
        this.volume.CalculatePointCloud(raycastPointCloudFrame, poseProposal);

        bool success = this.volume.AlignPointClouds(
            raycastPointCloudFrame,
            depthPointCloudFrame,
            FusionDepthProcessor.DefaultAlignIterationCount,
            imageFrame,
            out alignmentEnergy,
            ref poseProposal);

        bool relocSuccess = success && alignmentEnergy < bestNeighborAlignmentEnergy && alignmentEnergy > 0;

        if (relocSuccess) {
          bestNeighborAlignmentEnergy = alignmentEnergy;
          bestNeighborIndex = n;

          // This is after tracking succeeds, so should be a more accurate pose to store...
          bestNeighborCameraPose = poseProposal;

          // Update the delta image
          imageFrame.CopyPixelDataTo(this.deltaFromReferenceFramePixelsArgb);
        }

        // Find smallest energy neighbor independent of tracking success
        if (alignmentEnergy < smallestEnergy) {
          smallestEnergy = alignmentEnergy;
          smallestEnergyNeighborIndex = n;
        }
      }

      matchCandidates.Dispose();

      // Use the neighbor with the smallest residual alignment energy
      // At the cost of additional processing we could also use kNN+Mean camera pose finding here
      // by calculating the mean pose of the best n matched poses and also testing this to see if the 
      // residual alignment energy is less than with kNN.
      if (bestNeighborIndex > -1) {
        this.worldToCameraTransform = bestNeighborCameraPose;

        return true;
      } else {
        this.worldToCameraTransform = neighbors[smallestEnergyNeighborIndex];
        return false;
      }
    }
    */
  }
}
