using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.IO;
using KinectV2;
using Henge3D.Physics;
using Henge3D;
using System.Net.Sockets;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;

namespace KinectV3 {
  /// <summary>
  /// This is the main type for your game
  /// </summary>
  public class Game1 : Microsoft.Xna.Framework.Game {
    GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;
    Texture2D cameraFeed;
    Texture2D overlay;
    SpriteFont font;

    Quad quad;

    KinectManager manager;
    KeyboardState prevState;
    List<Body> balls = new List<Body>();

    RpcBody bodies1;
    List<Matrix> bodiesTransforms1 = new List<Matrix>();
    RpcBody bodies2;
    List<Matrix> bodiesTransforms2 = new List<Matrix>();

    Matrix offset = Matrix.Identity;
    Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(48.6f), 62f / 48.6f, .01f, 100f);

    // Transform from physics coordinate space to world coordinate space
    Matrix Tpw = Matrix.CreateScale(.1f)
      * Matrix.CreateFromYawPitchRoll(
          MathHelper.ToRadians(-5),
          MathHelper.ToRadians(5),
          MathHelper.ToRadians(0))
      * Matrix.CreateTranslation(0, 0, 0);

    // Transform from world coordinate space to physics coordinate space
    Matrix Twp = Matrix.CreateTranslation(0, 0, 0)
      * Matrix.CreateFromYawPitchRoll(
          MathHelper.ToRadians(5),
          MathHelper.ToRadians(-5),
          MathHelper.ToRadians(0))
      * Matrix.CreateScale(10);

    float aspectRatio;

    int red = 0;
    int blue = 0;

    TcpClient tcpClient;

    // Empirically gathered offset
    Vector3 offsetPos = new Vector3(0, -.01f, .35f);

    bool FULLSCREEN = false;
    bool ENABLE_KINECT = true;
    bool CONTINUE_TRACK = false;
    bool show = true;

    string MODEL = "wall-model-small";

    float mag = 10;
    float left = 0;
    float down = 0;

    float xAngle = 0;
    float yAngle = 0;
    float zAngle = 0;
    Vector3 cameraTranslation;

    Model model;
    Model sphere;
    Model cup_bottom;
    Model cup_top;

    StaticBody modelBody;
    StaticBody cupTopBody;
    StaticBody cupBottomBody;

    private string addBalls = "";

    private object writeLock = new object();
    private object readLock = new object();

    private static readonly int BLUE_BALL = 0;
    private static readonly int RED_BALL = 1;
    private int player = BLUE_BALL;

    public Game1() {
      graphics = new GraphicsDeviceManager(this);
      graphics.PreferredBackBufferWidth = KinectManager.IMG_WIDTH;
      graphics.PreferredBackBufferHeight = KinectManager.IMG_HEIGHT;
      graphics.IsFullScreen = FULLSCREEN;
      Content.RootDirectory = "Content";
      prevState = Keyboard.GetState();
    }

    /// <summary>
    /// Allows the game to perform any initialization it needs to before starting to run.
    /// This is where it can query for any required services and load any non-graphic
    /// related content.  Calling base.Initialize will enumerate through any components
    /// and initialize them as well.
    /// </summary>
    protected override void Initialize() {
      // TODO: Add your initialization logic here
      base.Initialize();
    }

    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// </summary>
    protected override void LoadContent() {
      cameraFeed = new Texture2D(graphics.GraphicsDevice, KinectManager.IMG_WIDTH, KinectManager.IMG_HEIGHT);
      overlay = new Texture2D(graphics.GraphicsDevice, 1, 1);
      overlay.SetData<Microsoft.Xna.Framework.Color>(new Microsoft.Xna.Framework.Color[] { new Microsoft.Xna.Framework.Color(1, 1, 1, .1f) });

      quad = new Quad(graphics.GraphicsDevice, cameraFeed);

      // Create a new SpriteBatch, which can be used to draw textures.
      spriteBatch = new SpriteBatch(GraphicsDevice);
      aspectRatio = graphics.GraphicsDevice.Viewport.AspectRatio;

      if (ENABLE_KINECT) {
        manager = new KinectManager("out.txt", CONTINUE_TRACK);
      }

      font = Content.Load<SpriteFont>("text");

      model = Content.Load<Model>(MODEL);
      sphere = Content.Load<Model>("sphere");
      cup_bottom = Content.Load<Model>("cup_bottom");
      cup_top = Content.Load<Model>("cup_top");
      bodies1 = new RpcBody(sphere, new Vector3(0, 0, 1), "sphere");
      bodies2 = new RpcBody(sphere, new Vector3(1, 0, 0), "sphere");

      modelBody = new StaticBody(model, transform(true), "wall", true);
      cupTopBody = new StaticBody(cup_top, transform(), "cup_top", false);
      cupBottomBody = new StaticBody(cup_bottom, transform(), "cup_bottom", false);

      new Thread(new ThreadStart(tcpPhysics)).Start();
    }

    private Matrix transform(bool rotate = false) {
      return (rotate ? Matrix.CreateRotationZ(MathHelper.ToRadians(-90)) : Matrix.CreateRotationY(MathHelper.ToRadians(-90))) * Twp;
    }

    /// <summary>
    /// UnloadContent will be called once per game and is the place to unload
    /// all content.
    /// </summary>
    protected override void UnloadContent() { }

    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Update(GameTime gameTime) {
      // Allows the game to exit
      if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
        this.Exit();

      // Hold down the enter key to reset the camera pose to identity
      if (Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Enter)) {
        if (ENABLE_KINECT) {
          manager.retry();
        }
      }

      foreach (Microsoft.Xna.Framework.Input.Keys key in Keyboard.GetState().GetPressedKeys()) {
        if (key == Microsoft.Xna.Framework.Input.Keys.D1) {
          offsetPos.X += .01f;
        } else if (key == Microsoft.Xna.Framework.Input.Keys.D2) {
          offsetPos.X -= .01f;
        } else if (key == Microsoft.Xna.Framework.Input.Keys.D3) {
          offsetPos.Y += .01f;
        } else if (key == Microsoft.Xna.Framework.Input.Keys.D4) {
          offsetPos.Y -= .01f;
        } else if (key == Microsoft.Xna.Framework.Input.Keys.D5) {
          offsetPos.Z += .01f;
        } else if (key == Microsoft.Xna.Framework.Input.Keys.D6) {
          offsetPos.Z -= .01f;
        } else if (key == Microsoft.Xna.Framework.Input.Keys.Space) {
          Console.WriteLine(offsetPos);
        }
      }

      offset = Matrix.CreateTranslation(offsetPos);
      Matrix m = Matrix.Invert(manager.Camera) * Matrix.Invert(offset) * Twp;
      Vector3 scale = new Vector3();
      cameraTranslation = new Vector3();
      Quaternion rotation = new Quaternion();
      m.Decompose(out scale, out rotation, out cameraTranslation);

      double sqw = rotation.W * rotation.W;
      double sqx = rotation.X * rotation.X;
      double sqy = rotation.Y * rotation.Y;
      double sqz = rotation.Z * rotation.Z;
      xAngle = (float)Math.Atan2(2f * rotation.X * rotation.W + 2f * rotation.Y * rotation.Z, 1 - 2f * (sqz + sqw));
      yAngle = (float)Math.Asin(2f * (rotation.X * rotation.Z - rotation.W * rotation.Y));
      zAngle = (float)Math.Atan2(2f * rotation.X * rotation.Y + 2f * rotation.Z * rotation.W, 1 - 2f * (sqy + sqz));

      if (Keyboard.GetState().IsKeyUp(Microsoft.Xna.Framework.Input.Keys.I) && prevState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.I)) {
        show = !show;
      }

      if (Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.OemPlus) && prevState.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.OemPlus)) {
        addBall(cameraTranslation);
      }

      // Update the model and previous state
      prevState = Keyboard.GetState();
      base.Update(gameTime);
    }

    private void addBall(Vector3 translation) {
      Monitor.Enter(writeLock);
      addBalls = string.Format("{0},{1},{2},{3},{4},{5},{6}", player, translation.X, translation.Y, translation.Z, mag, xAngle, yAngle);
      Console.WriteLine(addBalls);
      Monitor.Exit(writeLock);
    }

    private void tcpPhysics() {
      tcpClient = new TcpClient("127.0.0.1", 9000);
      StreamReader reader = new StreamReader(tcpClient.GetStream());
      StreamWriter writer = new StreamWriter(tcpClient.GetStream());
      while (true) {
        Monitor.Enter(writeLock);
        writer.WriteLine(addBalls);
        writer.Flush();
        addBalls = "";
        Monitor.Exit(writeLock);
        string data = reader.ReadLine();
        Monitor.Enter(readLock);
        string[] colors = data.Split('*');
        blue = int.Parse(colors[0]);
        red = int.Parse(colors[1]);
        if (!colors[2].Equals("")) {
          setBalls(colors[2], bodiesTransforms1);
        }
        if (!colors[3].Equals("")) {
          setBalls(colors[3], bodiesTransforms2);
        }
        Monitor.Exit(readLock);
      }
    }

    private void setBalls(string ballList, List<Matrix> transforms) {
      transforms.Clear();
      string[] parts = ballList.Split('|');
      foreach (string part in parts) {
        string[] matParts = part.Split(',');
        transforms.Add(new Matrix(
          float.Parse(matParts[0]), float.Parse(matParts[1]), float.Parse(matParts[2]), float.Parse(matParts[3]),
          float.Parse(matParts[4]), float.Parse(matParts[5]), float.Parse(matParts[6]), float.Parse(matParts[7]),
          float.Parse(matParts[8]), float.Parse(matParts[9]), float.Parse(matParts[10]), float.Parse(matParts[11]),
          float.Parse(matParts[12]), float.Parse(matParts[13]), float.Parse(matParts[14]), float.Parse(matParts[15])
        ));
      }
    }

    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime) {
      GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

      // Draw the camera feed
      if (ENABLE_KINECT) {
      graphics.GraphicsDevice.Textures[0] = null;
      graphics.GraphicsDevice.Textures[1] = null;
        byte[] imageData = manager.ImageData;
        if (imageData != null) {
          texture(KinectManager.IMG_WIDTH, KinectManager.IMG_HEIGHT, imageData);
        }

        // Change GraphicsDevice settings to allow for occlusion
        GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        GraphicsDevice.BlendState = BlendState.AlphaBlend;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;

        // Draw the camera feed
        quad.Draw(projection);

        if (!manager.isLoading) {
          // Get camera pose from kinect manager
          Matrix m = manager.Camera;

          // Decompose into its scale, rotation, and translation components
          Vector3 s = new Vector3();
          Quaternion r = new Quaternion();
          Vector3 t = new Vector3();
          m.Decompose(out s, out r, out t);

          // Invert the x axis rotation
          r.X *= -1;

          // Rebuild the transform
          Matrix transform = Matrix.CreateFromQuaternion(r);
          transform.M41 = m.M41;
          transform.M42 = m.M42;
          transform.M43 = m.M43;

          modelBody.Draw(transform, projection, Tpw * offset, show);

          Monitor.Enter(readLock);
          bodies1.Draw(transform, projection, Tpw * offset, bodiesTransforms1);
          bodies2.Draw(transform, projection, Tpw * offset, bodiesTransforms2);
          Monitor.Exit(readLock);

          cupBottomBody.Draw(transform, projection, Tpw * offset);
          cupTopBody.Draw(transform, projection, Tpw * offset);
        }
      } else {
        Matrix view = Matrix.CreateLookAt(offsetPos, Vector3.Zero, Vector3.Up);
        foreach (Body body in balls) {
          body.Draw(view, projection, Tpw);
        }
      }

      spriteBatch.Begin();
      if (!manager.isLoading) {
        drawVerticalTick();
        drawHorizontalTick();
        drawScore();
      } else {
        drawProgress(manager.progress, manager.max);
      }
      spriteBatch.End();
      base.Draw(gameTime);
    }

    private void drawVerticalTick() {
      float offset = -20 * MathHelper.ToDegrees((float) ((xAngle > 0) ? Math.PI : -Math.PI) - xAngle) / 9;
      spriteBatch.Draw(overlay, rect(34, 0, 1, 480), color(.70196f, 0.67843f, 0.67843f, .1f));
      for (int i = -2; i < 26; i++) {
        spriteBatch.Draw(overlay, rect(30, (int)(20 * i + offset), 10, 2), color(.70196f, 0.67843f, 0.67843f, .05f));
      }
    }

    private void drawHorizontalTick() {
      float offset = -20 * MathHelper.ToDegrees(yAngle) / 9;
      spriteBatch.Draw(overlay, rect(0, 450, 640, 1), color(.70196f, 0.67843f, 0.67843f, .05f));
      for (int i = -2; i < 34; i++) {
        spriteBatch.Draw(overlay, rect((int)(20 * i + offset), 444, 2, 10), color(.70196f, 0.67843f, 0.67843f, .05f));
      }
    }

    private void drawScore() {
      spriteBatch.Draw(overlay, rect(0, 0, 640, 30), color(0, 0, 0, .3f));
      string scoreString = "Red: " + red + " Blue: " + blue;
      Vector2 size = font.MeasureString(scoreString);

      spriteBatch.DrawString(font, scoreString, new Vector2(630 - size.X, (30 - size.Y) / 2), color(1, 1, 1, 1));
    }

    private void drawProgress(float progress, float max) {
      string loading = "Loading";
      Vector2 size = font.MeasureString(loading);
      spriteBatch.DrawString(font, "Loading", new Vector2(320 - size.X / 2, 200), color(.70196f, 0.67843f, 0.67843f, .05f));
      spriteBatch.Draw(overlay, rect(118, 223, 404, 2), color(.70196f, 0.67843f, 0.67843f, .05f));
      spriteBatch.Draw(overlay, rect(118, 255, 404, 2), color(.70196f, 0.67843f, 0.67843f, .05f));
      spriteBatch.Draw(overlay, rect(118, 225, 2, 30), color(.70196f, 0.67843f, 0.67843f, .05f));
      spriteBatch.Draw(overlay, rect(520, 225, 2, 30), color(.70196f, 0.67843f, 0.67843f, .05f));
      spriteBatch.Draw(overlay, rect(120, 225, (int)(400 * (progress / max)), 30), color(.70196f, 0.67843f, 0.67843f, .05f));
    }

    private Microsoft.Xna.Framework.Rectangle rect(int x, int y, int w, int h) {
      return new Microsoft.Xna.Framework.Rectangle(x, y, w, h);
    }

    private Microsoft.Xna.Framework.Color color(float r, float g, float b, float a) {
      return new Microsoft.Xna.Framework.Color(r, g, b, a);
    }

    private void texture(int width, int height, byte[] data) {
      // Convert the byte data into a color array to set into the texture
      Microsoft.Xna.Framework.Color[] color = new Microsoft.Xna.Framework.Color[width * height];

      // Go through each pixel and set the bytes correctly.
      // Remember, each pixel got a Red, Green and Blue channel.
      for (int y = 0; y < height; y++) {
        for (int x = 0; x < width; x++) {
          int index = 4 * (y * width + x);
          color[y * width + (width - x - 1)] = new Microsoft.Xna.Framework.Color(data[index + 2], data[index + 1], data[index + 0]);
        }
      }
      // Set pixeldata from the ColorImageFrame to a Texture2D

      cameraFeed.SetData(color);
    }
  }
}
