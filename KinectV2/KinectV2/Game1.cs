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

namespace KinectV2 {
  /// <summary>
  /// This is the main type for your game
  /// </summary>
  public class Game1 : Microsoft.Xna.Framework.Game {
    GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;
    Texture2D cameraFeed;
    KinectManager manager;
    KeyboardState prevState;
    float aspectRatio;

    // Empirically gathered offset
    //float offsetX = 0.01f;
    //float offsetY = 0;
    //float offsetZ = 0.35f;
    float offsetX = 0f;
    float offsetY = -0.01f;
    float offsetZ = 0.35f;

    bool ENABLE_KINECT = true;
    bool CONTINUE_TRACK = false;

    GlobeModel model;

    public Game1() {
      graphics = new GraphicsDeviceManager(this);
      graphics.PreferredBackBufferWidth = 640;
      graphics.PreferredBackBufferHeight = 480;
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
      // Create a new SpriteBatch, which can be used to draw textures.
      spriteBatch = new SpriteBatch(GraphicsDevice);

      // Create a new model
      DataPoint[] data = new DataPoint[360 * 180];
      for (int lng = 0; lng < 360; lng ++) {
        for (int lat = 0; lat < 180; lat ++) {
          int ind = lng * 180 + lat;
          data[ind] = new DataPoint(lat - 90, lng - 180, 0);
        }
      }
      List<DataPoint> dataList = new List<DataPoint>();


      //model = new GlobeModel(Content, GraphicsDevice, Matrix.CreateTranslation(offsetX, offsetY, offsetZ));


      //string line;
      //StreamReader file = new StreamReader("earthquake.tsv");
      //while ((line = file.ReadLine()) != null) {
      //  string[] parts = line.Split(' ');
      //  int lat = (int)(float.Parse(parts[0]) + .5f);
      //  int lng = (int)(float.Parse(parts[1]) + .5f);
      //  int ind = (lng >= 180 ? 0 : lng + 180) * 180 + (lat + 90);
      //  data[ind].value++;
      //  dataList.Add(new DataPoint(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]) / 9.5f));
      //}
      //dataList.Clear();
      //dataList.Add(new DataPoint(35.673343f, 139.710388f, 1));
      //data = dataList.ToArray();
      //model.setData(data);

      aspectRatio = graphics.GraphicsDevice.Viewport.AspectRatio;

      // Set cullmode
      RasterizerState rs = new RasterizerState();
      rs.CullMode = CullMode.CullCounterClockwiseFace;
      GraphicsDevice.RasterizerState = rs;

      if (ENABLE_KINECT) {
        manager = new KinectManager("out.txt", CONTINUE_TRACK);
      }
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
      if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        this.Exit();

      // Hold down the enter key to reset the camera pose to identity
      if (Keyboard.GetState().IsKeyDown(Keys.Enter)) {
        if (ENABLE_KINECT) {
          manager.retry();
        }
      }

      foreach (Keys key in Keyboard.GetState().GetPressedKeys()) {
        if (key == Keys.D1) {
          offsetX += .01f;
        } else if (key == Keys.D2) {
          offsetX -= .01f;
        } else if (key == Keys.D3) {
          offsetY += .01f;
        } else if (key == Keys.D4) {
          offsetY -= .01f;
        } else if (key == Keys.D5) {
          offsetZ += .01f;
        } else if (key == Keys.D6) {
          offsetZ -= .01f;
        } else if (key == Keys.Space) {
          Console.WriteLine(offsetX + " " + offsetY + " " + offsetZ);
        }

        // 7 key toggles tiles vs bars
        if (key == Keys.D7 && prevState.IsKeyUp(Keys.D7)) {
          model.toggleSceneOcclusion();
        }

        // 8 key toggles tiles vs bars
        if (key == Keys.D8 && prevState.IsKeyUp(Keys.D8)) {
          model.toggleTile();
        }

        // 9 key toggles the chair
        if (key == Keys.D9 && prevState.IsKeyUp(Keys.D9)) {
          model.toggleChairDraw();
        }

        // 0 key toggles the model
        if (key == Keys.D0 && prevState.IsKeyUp(Keys.D0)) {
          model.toggleOverlayDraw();
        }

        if (key == Keys.OemMinus && prevState.IsKeyUp(Keys.OemMinus)) {
          model.toggleRenderOverlay();
        }

        if (key == Keys.S && prevState.IsKeyUp(Keys.S)) {
          LocationSocket.start(model);
        }
      }

      // Update the model and previous state
      prevState = Keyboard.GetState();
      model.setOffset(Matrix.CreateTranslation(offsetX, offsetY, offsetZ));
      model.update(gameTime.ElapsedGameTime.Milliseconds);
      base.Update(gameTime);
    }

    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime) {
      GraphicsDevice.Clear(Color.Black);
      // Draw the camera feed
      //Console.WriteLine(gameTime.ElapsedGameTime.Milliseconds);

      if (ENABLE_KINECT) {
        spriteBatch.Begin();
        byte[] imageData = manager.ImageData;
        if (imageData != null) {
          texture(640, 480, imageData);
        }
        spriteBatch.Draw(cameraFeed, new Rectangle(0, 0, 640, 480), Color.White);
        spriteBatch.End();

        // Change GraphicsDevice settings to allow for occlusion
        GraphicsDevice.BlendState = BlendState.AlphaBlend;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;

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

        // Render with the new transform
        model.updateOcclusion(manager.PointCloudData);
        model.render(transform, GraphicsDevice);
      } else {
        model.render(Matrix.Identity, GraphicsDevice);
      }
      //model.render(Matrix.Identity, GraphicsDevice);
      base.Draw(gameTime);
    }

    private void texture(int width, int height, byte[] data) {
      // Convert the byte data into a color array to set into the texture
      Color[] color = new Color[width * height];
      cameraFeed = new Texture2D(graphics.GraphicsDevice, width, height);

      // Go through each pixel and set the bytes correctly.
      // Remember, each pixel got a Red, Green and Blue channel.
      for (int y = 0; y < height; y++) {
        for (int x = 0; x < width; x++) {
          int index = 4 * (y * width + x);
          color[y * width + (width - x - 1)] = new Color(data[index + 2], data[index + 1], data[index + 0]);
        }
      }
      // Set pixeldata from the ColorImageFrame to a Texture2D
      cameraFeed.SetData(color);
    }
  }
}
