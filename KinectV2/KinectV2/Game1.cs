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
    //KinectManager manager;
    KeyboardState prevState;
    float aspectRatio;

    // Empirically gathered offset
    float offsetX = 0.01f;
    float offsetY = 0;
    float offsetZ = 0.35f;

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
      model = new GlobeModel(Content, GraphicsDevice, Matrix.CreateTranslation(offsetX, offsetY, offsetZ));
      // Get a list of data points from the file
      List<DataPoint> dataPoints = new List<DataPoint>();
      string line;
      StreamReader file = new StreamReader("earthquake.tsv");
      while ((line = file.ReadLine()) != null) {
        string[] parts = line.Split(' ');
        dataPoints.Add(new DataPoint(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]) / 9.5f));
      }
      /*
      dataPoints.Clear();
      for (float lng = (float)-180; lng < 180; lng += 5) {
        float lat = lng / 2;
        float value = (float) ((lng + 180) / (360f));
        dataPoints.Add(new DataPoint(lat, lng, value));
      }*/
      model.setData(dataPoints.ToArray());

      aspectRatio = graphics.GraphicsDevice.Viewport.AspectRatio;

      // Set cullmode
      RasterizerState rs = new RasterizerState();
      rs.CullMode = CullMode.None;
      GraphicsDevice.RasterizerState = rs;

      //manager = new KinectManager("out.txt");
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
        //manager.retry();
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
          offsetZ += .1f;
        } else if (key == Keys.D6) {
          offsetZ -= .1f;
        } else if (key == Keys.Space) {
          Console.WriteLine(offsetX + " " + offsetY + " " + offsetZ);
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
      GraphicsDevice.Clear(Color.CornflowerBlue);
      /*
      // Draw the camera feed
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
      */
      // Render with the new transform
      //model.render(GraphicsDevice, transform);
      model.render(Matrix.Identity);
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
