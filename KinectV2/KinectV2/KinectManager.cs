using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Fusion;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KinectV2 {
  /// <summary>
  /// Manager for grabbing point clouds
  /// </summary>
  class KinectManager {
    // Constants for the reconstruction volume
    private readonly int VOXEL_RESOLUTION = 256;
    //private readonly int VOXEL_RESOLUTION = 384; // in voxels/meter
    private readonly int X_VOXELS = 256; // in voxels
    private readonly int Y_VOXELS = 384;
    //private readonly int Y_VOXELS = 256; // in voxels
    private readonly int Z_VOXELS = 256; // in voxels

    // Kinect sensor
    private KinectSensor sensor;

    // Locks and flags to prevent untimely access to resources
    private bool depthReady = true;
    private bool colorReady = true;
    private object depthLock = new object();
    private object colorLock = new object();
    private object matrixLock = new object();
    private object outPixelLock = new object();
    private object colorPointLock = new object();

    // Formats for Depth and Color
    private readonly DepthImageFormat DEPTH_FORMAT = DepthImageFormat.Resolution640x480Fps30;
    private readonly ColorImageFormat COLOR_FORMAT = ColorImageFormat.RgbResolution640x480Fps30;

    // Reconstruction volume to hold prescanned model
    private ColorReconstruction volume;

    // The current camera pose / guess for next frame's pose
    private Matrix4 currentMatrix = Matrix4.Identity;

    // Camera pose and alignment energy (how much error there is based on the pose)
    // Not used
    private Matrix4 worldToCameraTransform = Matrix4.Identity;
    private float alignmentEnergy;


    // byte array used as the container for data ready to be read
    private byte[] outColor = new byte[640 * 480 * 4];
    // byte array to read data from color frame into
    private byte[] color = new byte[640 * 480 * 4];

    // Temporary frames to copy data into
    private FusionFloatImageFrame frame = new FusionFloatImageFrame(640, 480);
    private FusionFloatImageFrame smoothDepthFloatFrame = new FusionFloatImageFrame(640, 480);
    private FusionPointCloudImageFrame observedPointCloud = new FusionPointCloudImageFrame(640, 480);
    private FusionPointCloudImageFrame imgFrame = new FusionPointCloudImageFrame(640, 480);


    // Use camera pose finder as another method of trying to locate the correct camera pose
    // Currently not used
    private CameraPoseFinder poseFinder = CameraPoseFinder.FusionCreateCameraPoseFinder(CameraPoseFinderParameters.Defaults);

    // Temporary frames to copy data into, used for camera pose finder, currently not used
    private int[] deltaFromReferenceFramePixelsArgb = new int[640 * 480];
    private FusionFloatImageFrame smoothDepthFloatFrameCamera = new FusionFloatImageFrame(640, 480);
    private FusionPointCloudImageFrame depthPointCloudFrame = new FusionPointCloudImageFrame(640, 480);
    private FusionPointCloudImageFrame raycastPointCloudFrame = new FusionPointCloudImageFrame(640, 480);
    private FusionColorImageFrame delta = new FusionColorImageFrame(640, 480);

    // Holder for frames passed from the sensor, ready for the background threads to handle
    private DepthImageFrameReadyEventArgs de;
    private ColorImageFrameReadyEventArgs ce;

    // Depth to Color mapping, aligns color image to depth image so the marker and overlay align better
    private ColorImagePoint[] points = new ColorImagePoint[640 * 480];
    private CoordinateMapper mapper;

    public KinectManager(string modelData) {
      // Read in the pose finder
      poseFinder.LoadCameraPoseFinderDatabase("poseFinder.txt");

      // Read in the model reconstruction (prescanned model)
      FileStream stream = System.IO.File.OpenRead(modelData);
      // Open bracket
      char ch = (char) stream.ReadByte();
      // Copy all the model data into a short array of the same size
      short[] modelVolumeData = new short[X_VOXELS * Y_VOXELS * Z_VOXELS];
      // Parse what is essentially a really big json array
      StringBuilder b = new StringBuilder();
      for (int i = 0; i < modelVolumeData.Length; i++) {
        ch = (char)stream.ReadByte();
        while (ch != ']' && ch != ',') {
          b.Append(ch);
          ch = (char)stream.ReadByte();
        }
        modelVolumeData[i] = short.Parse(b.ToString());
        b.Clear();
        // Update after every 100000 characters read
        if (i % 100000 == 0) {
          Console.WriteLine(i);
        }
      }

      // Build the reconstruction volume from the prescanned model
      // Now we have access to our prescanned model
      ReconstructionParameters rParams = new ReconstructionParameters(VOXEL_RESOLUTION, X_VOXELS, Y_VOXELS, Z_VOXELS);
      volume = ColorReconstruction.FusionCreateReconstruction(rParams, ReconstructionProcessor.Amp, -1, Matrix4.Identity);
      volume.ImportVolumeBlock(modelVolumeData);

      // Find a kinect
      foreach (KinectSensor potentialSensor in KinectSensor.KinectSensors) {
        if (potentialSensor.Status == KinectStatus.Connected && !potentialSensor.IsRunning) {
          sensor = potentialSensor;
          break;
        }
      }

      if (sensor == null) {
        Console.WriteLine("Can't find Kinect Sensor");
        return;
      }

      // Enable and set up the listeners for the sensor
      mapper = new CoordinateMapper(sensor);
      sensor.DepthStream.Enable(DEPTH_FORMAT);
      sensor.ColorStream.Enable(COLOR_FORMAT);

      sensor.ColorFrameReady += colorFrameReady;
      sensor.DepthFrameReady += depthFrameReady;

      // Start the sensor reading data and the background threads
      sensor.Start();
      new Thread(new ThreadStart(runDepth)).Start();
      new Thread(new ThreadStart(runColor)).Start();
    }

    // Gets camera data
    public byte[] ImageData {
      get {
        byte[] data = null;
        // Enter into a monitor to check copy the data from outColor
        Monitor.Enter(outPixelLock);
        if (outColor != null) {
          data = new byte[outColor.Length];
          for (int i = 0; i < outColor.Length; i++) {
            data[i] = outColor[i];
          }
        }
        Monitor.Exit(outPixelLock);
        return data;
      }
    }

    public Matrix Camera {
      get {
        Matrix m = Matrix.Identity;
        // Enter into a monitor to copy the data from currentMatrix
        Monitor.Enter(matrixLock);
        m.M11 = currentMatrix.M11;
        m.M12 = currentMatrix.M12;
        m.M13 = currentMatrix.M13;
        m.M14 = currentMatrix.M14;

        m.M21 = currentMatrix.M21;
        m.M22 = currentMatrix.M22;
        m.M23 = currentMatrix.M23;
        m.M24 = currentMatrix.M24;

        m.M31 = currentMatrix.M31;
        m.M32 = currentMatrix.M32;
        m.M33 = currentMatrix.M33;
        m.M34 = currentMatrix.M34;

        m.M44 = currentMatrix.M44;

        // We need to invert the matrix for our needs
        m = Matrix.Invert(m);

        // Due to odd axis definitions between xna and kinect, we need to flip a few things
        m.M41 = currentMatrix.M41;
        m.M42 = -currentMatrix.M42;
        m.M43 = -currentMatrix.M43;
        Monitor.Exit(matrixLock);
        return m;
      }
    }

    private void colorFrameReady(object sender, ColorImageFrameReadyEventArgs e) {
      // When the kinect sends a color frame, first make sure that it isn't being backlogged
      // Then, enter into a colorLock context to set the pending frame to be parsed next
      // If this is called multiple times before the frame is parsed, the pending frame is overwritten
      // meaning that non current frames are dropped
      if (colorReady) {
        Monitor.Enter(colorLock);
        colorReady = false;
        this.ce = e;
        colorReady = true;
        Monitor.Exit(colorLock);
      }
    }

    public void retry() {
      // Reset the current pose to identity and start over
      this.currentMatrix = Matrix4.Identity;
    }

    private void runColor() {
      // Loop indefinitely and parse the current frame every time
      while (true) {
        ColorImageFrameReadyEventArgs c = ce;
        if (c == null) {
          Thread.Sleep(100);
        } else {
          doColorFrameReady(c);
        }
      }
    }

    private void doColorFrameReady(ColorImageFrameReadyEventArgs readyFrame) {
      if (colorReady) {
        Monitor.Enter(colorLock);
        // Parse the current color frame
        using (ColorImageFrame colorFrame = readyFrame.OpenColorImageFrame()) {
          if (colorFrame == null) {
            Monitor.Exit(colorLock);
            return;
          }
          colorReady = false;
          colorFrame.CopyPixelDataTo(color);

          // Enter into a colorPointLock to make sure that the colorpoint array isn't
          // changed or updated while processing it
          Monitor.Enter(colorPointLock);
          if (points != null) {
            // for each color pixel (4 bytes) move it to the new mapping to match up with the depth pixel
            byte[] newColors = new byte[640 * 480 * 4];
            for (int y = 0; y < 480; y++) {
              for (int x = 0; x < 640; x++) {
                ColorImagePoint p = points[y * 640 + x];
                if (p.Y < 480 && p.Y >= 0 && p.X < 640 && p.X >= 0) {
                  newColors[4 * ((y * 640) + x)] = color[4 * ((p.Y * 640) + p.X)];
                  newColors[4 * ((y * 640) + x) + 1] = color[4 * ((p.Y * 640) + p.X) + 1];
                  newColors[4 * ((y * 640) + x) + 2] = color[4 * ((p.Y * 640) + p.X) + 2];
                  newColors[4 * ((y * 640) + x) + 3] = color[4 * ((p.Y * 640) + p.X) + 3];
                }
              }
            }
            // Set the ready data to the new array
            Monitor.Enter(outPixelLock);
            outColor = newColors;
            Monitor.Exit(outPixelLock);
          } else {
            // If no color mapping data is available, set the ready data to
            // our non-mapped color data
            Monitor.Enter(outPixelLock);
            outColor = color;
            Monitor.Exit(outPixelLock);
          }
          Monitor.Exit(colorPointLock);
          colorReady = true;
        }
        Monitor.Exit(colorLock);
      }
    }

    private void runDepth() {
      while (true) {
        DepthImageFrameReadyEventArgs d = de;
        if (d == null) {
          Thread.Sleep(100);
        } else {
          doDepthFrameReady(d);
        }
      }
    }

    private void doDepthFrameReady(DepthImageFrameReadyEventArgs readyFrame) {
      if (depthReady) {
        Monitor.Enter(depthLock);
        // Enter a context with both the depth and color frames
        using (DepthImageFrame depthFrame = readyFrame.OpenDepthImageFrame()) {
          if (depthFrame == null) {
            Monitor.Exit(depthLock);
            return;
          }

          // Lock resources and prevent further frame callbacks
          depthReady = false;

          // Init
          DepthImagePixel[] depth = new DepthImagePixel[depthFrame.PixelDataLength];

          // Get a mapping between color points and depth points, to align the color image so it looks like the depth image
          // This helps in aligning the object to the marker better
          Monitor.Enter(colorPointLock);
          mapper.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, depth,
              ColorImageFormat.RgbResolution640x480Fps30, points);
          Monitor.Exit(colorPointLock);

          // Obtain raw data from frames
          depthFrame.CopyDepthImagePixelDataTo(depth);

          // Construct into a fusionfloatimageframe
          FusionDepthProcessor.DepthToDepthFloatFrame(depth, 640, 480, frame, FusionDepthProcessor.DefaultMinimumDepth, FusionDepthProcessor.DefaultMaximumDepth, false);
          this.volume.SmoothDepthFloatFrame(frame, smoothDepthFloatFrame, 1, .04f);

          // Calculate point cloud
          FusionDepthProcessor.DepthFloatFrameToPointCloud(smoothDepthFloatFrame, observedPointCloud);

          // Get the current camera pose and calculate the point cloud (view of pre scanned model) from it
          Matrix4 t = currentMatrix;
          volume.CalculatePointCloud(imgFrame, t);

          // Try to align our two point clouds, with t containing the new camera pose if successful
          bool success = FusionDepthProcessor.AlignPointClouds(imgFrame, observedPointCloud, 10, null, ref t);
          if (success) {
            // If successful, set the current matrix to our new camera pose
            Monitor.Enter(matrixLock);
            this.currentMatrix = t;
            Monitor.Exit(matrixLock);
          }
          
          // Log out the current matrix
          Console.WriteLine(success);
          Console.WriteLine("{0} {1} {2} {3}", currentMatrix.M11, currentMatrix.M12, currentMatrix.M13, currentMatrix.M14);
          Console.WriteLine("{0} {1} {2} {3}", currentMatrix.M21, currentMatrix.M22, currentMatrix.M23, currentMatrix.M24);
          Console.WriteLine("{0} {1} {2} {3}", currentMatrix.M31, currentMatrix.M32, currentMatrix.M33, currentMatrix.M34);
          Console.WriteLine("{0} {1} {2} {3}", currentMatrix.M41, currentMatrix.M42, currentMatrix.M43, currentMatrix.M44);
          
          // Release resources, now ready for next callback
          depthReady = true;
        }
        Monitor.Exit(depthLock);
      }
    }

    // When both color and depth frames are ready
    private void depthFrameReady(object sender, DepthImageFrameReadyEventArgs e) {
      // Only get the frames when we're done processing the previous one, to prevent
      // frame callbacks from piling up
      if (depthReady) {
        Monitor.Enter(depthLock);
        depthReady = false;
        this.de = e;
        depthReady = true;
        Monitor.Exit(depthLock);
      }
    }

    // Not used
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
      
      // Smooth the depth frame
      this.volume.SmoothDepthFloatFrame(floatFrame, smoothDepthFloatFrameCamera, 1, .04f);

      // Calculate point cloud from the smoothed frame
      FusionDepthProcessor.DepthFloatFrameToPointCloud(smoothDepthFloatFrameCamera, depthPointCloudFrame);

      double smallestEnergy = double.MaxValue;
      int smallestEnergyNeighborIndex = -1;

      int bestNeighborIndex = -1;
      Matrix4 bestNeighborCameraPose = Matrix4.Identity;

      double bestNeighborAlignmentEnergy = 0.006f;

      // Run alignment with best matched poseCount (i.e. k nearest neighbors (kNN))
      int maxTests = Math.Min(5, poseCount);

      var neighbors = matchCandidates.GetMatchPoses();
      
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
  }
}
