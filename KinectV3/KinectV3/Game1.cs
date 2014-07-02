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

namespace KinectV3 {
  /// <summary>
  /// This is the main type for your game
  /// </summary>
  public class Game1 : Microsoft.Xna.Framework.Game {
    GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;
    Texture2D cameraFeed;
    KinectManager manager;
    KeyboardState prevState;
    List<Body> bodies = new List<Body>();
    Matrix offset = Matrix.Identity;
    Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(48.6f), 62f / 48.6f, .01f, 100f);

    Matrix Tpw = Matrix.CreateRotationX(MathHelper.ToRadians(12.7f));// * Matrix.CreateTranslation(0, -.2948f, -.6f);
    //Matrix Twp = Matrix.CreateRotationX(MathHelper.ToRadians(-12.7f));// * Matrix.CreateTranslation(0, .2948f, .6f);
    Matrix Twp = Matrix.CreateRotationX(MathHelper.ToRadians(12.7f));
    PhysicsManager physics;

    float aspectRatio;

    // Empirically gathered offset
    //float offsetX = 0.01f;
    //float offsetY = 0;
    //float offsetZ = 0.35f;
    Vector3 offsetPos = new Vector3(0, -.01f, .35f);

    bool ENABLE_KINECT = true;
    bool CONTINUE_TRACK = false;
    bool toggle = false;

    float mag = 1;
    float left = 0;
    float down = 0;

    Model model;

    public Game1() {
      graphics = new GraphicsDeviceManager(this);
      graphics.PreferredBackBufferWidth = 640;
      graphics.PreferredBackBufferHeight = 480;
      Content.RootDirectory = "Content";
      prevState = Keyboard.GetState();
      physics = new PhysicsManager(this);
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
      physics.Gravity = new Vector3(0, -9.8f, 0);
    }

    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// </summary>
    protected override void LoadContent() {
      // Create a new SpriteBatch, which can be used to draw textures.
      spriteBatch = new SpriteBatch(GraphicsDevice);
      aspectRatio = graphics.GraphicsDevice.Viewport.AspectRatio;

      if (ENABLE_KINECT) {
        manager = new KinectManager("out.txt", CONTINUE_TRACK);
      }

      //Model cup = Content.Load<Model>("cup_top");
      //Body bodyCup = new Body(this, cup, "");
      //bodies.Add(bodyCup);
      //physics.Add(bodyCup);

      model = Content.Load<Model>("modelascii");
      //Body bodyWorld = new Body(this, model, "");
      //bodyWorld.Transform = new Henge3D.Transform(1, Vector3.Zero, Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationX(MathHelper.ToRadians(-12.7f))));
      //bodies.Add(bodyWorld);
      //physics.Add(bodyWorld);

      /*
      BodySkin skin = new BodySkin();
      skin.DefaultMaterial = new Material(1, 0);
      skin.Add(
        new PlanePart(new Vector3(0, 0, 0), Vector3.UnitY)
      );
      RigidBody wall = new RigidBody(skin);
      wall.SetWorld(10, Vector3.Zero, Quaternion.Identity);
      physics.Add(wall);*/


      //model = Content.Load<Model>("model1");
      BodySkin skin = new BodySkin();
      skin.DefaultMaterial = new Material(0, 0);
      skin.Add(
        new PlanePart(new Vector3(0, -.5f, 0), Vector3.UnitY),
        new PlanePart(new Vector3(0, 0, -2), Vector3.UnitZ)
      );
      RigidBody wall = new RigidBody(skin);
      physics.Add(wall);

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
          offsetPos.X += .01f;
        } else if (key == Keys.D2) {
          offsetPos.X -= .01f;
        } else if (key == Keys.D3) {
          offsetPos.Y += .01f;
        } else if (key == Keys.D4) {
          offsetPos.Y -= .01f;
        } else if (key == Keys.D5) {
          offsetPos.Z += .01f;
        } else if (key == Keys.D6) {
          offsetPos.Z -= .01f;
        } else if (key == Keys.Space) {
          Console.WriteLine(offsetPos);
        }
      }

      Matrix.CreateTranslation(ref offsetPos, out offset);

      if (ENABLE_KINECT) {
        if (prevState.IsKeyUp(Keys.OemPlus) && Keyboard.GetState().IsKeyDown(Keys.OemPlus)) {
          Model sphere = Content.Load<Model>("sphere");
          Body body = new Body(this, sphere, toggle ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1), "sphere");

          // Get camera pose from kinect manager
          Matrix m = manager.Camera;

          // Decompose into its scale, rotation, and translation components
          Vector3 s = new Vector3();
          Quaternion r = new Quaternion();
          Vector3 t = new Vector3();
          m.Decompose(out s, out r, out t);
          r.X *= -1;

          float eulerZ = (float)Math.Atan2(2f * r.X * r.Y + 2f * r.Z * r.W, 1 - 2f * (r.Y * r.Y + r.Z * r.Z));
          float eulerX = (float)Math.Atan2(2f * r.X * r.W + 2f * r.Y * r.Z, 1 - 2f * (r.Z * r.Z + r.W * r.W));
          float eulerY = (float)Math.Asin(2f * (r.X * r.Z - r.W * r.Y));

          //toggle = !toggle;

          /*body.SetVelocity(new Vector3(
              (float)(Math.Cos(eulerX) * Math.Sin(eulerY) * 10),
              (float)(Math.Sin(eulerX) * 10),
              (float)(Math.Cos(eulerX) * Math.Cos(eulerY) * 10)
          ), Vector3.Zero);*/

          body.SetVelocity(new Vector3(0, .5f, -.5f), Vector3.Zero);

          body.SetWorld(.1f, Vector3.Zero, Quaternion.Identity);
          for (int i = 0; i < body.Skin.Count; i++) {
            body.Skin.SetMaterial(body.Skin[i], new Material(0, 0));
          }
          physics.Add(body);
          bodies.Add(body);
        }
      } else {
        if (prevState.IsKeyUp(Keys.OemPlus) && Keyboard.GetState().IsKeyDown(Keys.OemPlus)) {
          Model sphere = Content.Load<Model>("sphere");
          Body body = new Body(this, sphere, toggle ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1), "sphere");
          body.SetWorld(.1f, new Vector3(left, down, 0), Quaternion.Identity);
          body.SetVelocity(new Vector3(0, 2, -mag), Vector3.Zero);
          for (int i = 0; i < body.Skin.Count; i++) {
            body.Skin.SetMaterial(body.Skin[i], new Material(1f, 1f));
          }
          physics.Add(body);
          bodies.Add(body);

        }
      }

      if (Keyboard.GetState().IsKeyDown(Keys.Up)) {
        mag += .01f;
        Console.WriteLine(mag);
      } else if (Keyboard.GetState().IsKeyDown(Keys.Down)) {
        mag-= .01f;
        Console.WriteLine(mag);
      } else if (Keyboard.GetState().IsKeyDown(Keys.Left)) {
        left -= .01f;
        Console.WriteLine(left);
      } else if (Keyboard.GetState().IsKeyDown(Keys.Right)) {
        left += .01f;
        Console.WriteLine(left);
      } else if (Keyboard.GetState().IsKeyDown(Keys.W)) {
        down += .01f;
      } else if (Keyboard.GetState().IsKeyDown(Keys.S)) {
        down -= .01f;
      } else if (Keyboard.GetState().IsKeyDown(Keys.X)) {
        toggle = !toggle;
      } else if (Keyboard.GetState().IsKeyDown(Keys.Y)) {
        manager.toggleRun();
      }
      // Update the model and previous state
      prevState = Keyboard.GetState();
      base.Update(gameTime);
    }

    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime) {
      GraphicsDevice.Clear(Color.Black);
      
      // Draw the camera feed
      if (ENABLE_KINECT) {
        spriteBatch.Begin();
        byte[] imageData = manager.ImageData;
        if (imageData != null) {
          texture(640, 480, imageData);
        }
        spriteBatch.Draw(cameraFeed, new Rectangle(0, 0, 640, 480), Color.White);
        spriteBatch.End();

        // Change GraphicsDevice settings to allow for occlusion
        GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
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

        Matrix tpc = Twp * offset;


        // Render with the new transform
        foreach (Body body in bodies) {
          body.Draw(transform, projection, tpc);
        }

        if (toggle) {
          Matrix[] meshTransforms = new Matrix[model.Bones.Count];
          model.CopyAbsoluteBoneTransformsTo(meshTransforms);
          foreach (var mesh in model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
              effect.World = meshTransforms[mesh.ParentBone.Index] * offset;
              effect.View = transform;
              effect.Projection = projection;
            }
            mesh.Draw();
          }
        }


      } else {
        Matrix tpc = offset * Tpw;
        Matrix identity = Matrix.Identity;
        Vector3 translation;
        Quaternion q;
        Vector3 s;
        offset.Decompose(out s, out q, out translation);
        Matrix view = Matrix.CreateLookAt(translation, Vector3.Zero, Vector3.Up);
        foreach (Body body in bodies) {
          body.Draw(view,
            projection, identity);
        }
        /*
        foreach (Body body in bodies) {
          body.Draw(identity, projection, offset);
        }*/
      }
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
