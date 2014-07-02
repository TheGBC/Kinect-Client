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
using Henge3D.Physics;
using Henge3D;

namespace PhysicsTest {
  /// <summary>
  /// This is the main type for your game
  /// </summary>
  public class Game1 : Microsoft.Xna.Framework.Game {
    GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;
    PhysicsManager physicsManager;
    List<Body> bodies = new List<Body>();

    RigidBody wall;

    SpriteFont font;

    bool toggle = false;

    float x;
    float y;
    float z = -10;

    float magnitude = 1;
    float angle = (float) Math.PI / 4;

    KeyboardState prevState;

    public Game1() {
      graphics = new GraphicsDeviceManager(this);
      Content.RootDirectory = "Content";
      physicsManager = new PhysicsManager(this);
    }


    private bool OnCollide(RigidBody b1, RigidBody b2) {
      Body body1 = b1 as Body;
      Body body2 = b2 as Body;

      if (body1.Tag.Equals("cup_bottom") && body2.Tag.Equals("sphere")) {
        Console.WriteLine("CONTACT");
        physicsManager.Remove(body2);
        bodies.Remove(body2);
      } else if (body1.Tag.Equals("sphere") && body2.Tag.Equals("cup_bottom")) {
        Console.WriteLine("CONTACT");
        physicsManager.Remove(body1);
        bodies.Remove(body1);
      }

      return false;
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

      physicsManager.LinearErrorTolerance = .2f;

      Model cupBottomModel = Content.Load<Model>("cup_bottom");
      Model cupTopModel = Content.Load<Model>("cup_top");

      Body cupBottom = new Body(this, cupBottomModel, "cup_bottom");
      Body cupTop = new Body(this, cupTopModel, "cup_top");

      cupBottom.OnCollision += OnCollide;
      for (int i = 0; i < cupBottom.Skin.Count; i++) {
        cupBottom.Skin.SetMaterial(cupBottom.Skin[i], new Material(1, .5f));
      }
       
      cupBottom.MassProperties = MassProperties.Immovable;
      cupBottom.SetWorld(10, Vector3.Zero, Quaternion.Identity);
      bodies.Add(cupBottom);

      for (int i = 0; i < cupTop.Skin.Count; i++) {
        cupTop.Skin.SetMaterial(cupTop.Skin[i], new Material(1, .5f));
      }
      cupTop.MassProperties = MassProperties.Immovable;
      cupTop.SetWorld(10, Vector3.Zero, Quaternion.Identity);

      
      bodies.Add(cupTop);


      BodySkin skin = new BodySkin();
      skin.DefaultMaterial = new Material(.4f, .5f);
      skin.Add(
        new PlanePart(new Vector3(0, 0, -1f), Vector3.UnitZ),
        new PlanePart(new Vector3(0, 1, 0), -Vector3.UnitY),
        new PlanePart(new Vector3(0, 0, 0), Vector3.UnitY),
        new PlanePart(new Vector3(1, 0, 0), -Vector3.UnitX),
        new PlanePart(new Vector3(-1, 0, 0), Vector3.UnitX)
      );
      wall = new RigidBody(skin);
      wall.SetWorld(10, Vector3.Zero, Quaternion.Identity);

      physicsManager.Gravity = new Vector3(0,-9.8f, 0);
      physicsManager.Add(cupBottom);
      physicsManager.Add(cupTop);
      physicsManager.Add(wall);

      prevState = Keyboard.GetState();
    }

    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// </summary>
    protected override void LoadContent() {
      // Create a new SpriteBatch, which can be used to draw textures.
      spriteBatch = new SpriteBatch(GraphicsDevice);
      font = Content.Load<SpriteFont>("font");
      // TODO: use this.Content to load your game content here
    }

    /// <summary>
    /// UnloadContent will be called once per game and is the place to unload
    /// all content.
    /// </summary>
    protected override void UnloadContent() {
      // TODO: Unload any non ContentManager content here
    }

    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Update(GameTime gameTime) {
      // Allows the game to exit
      if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        this.Exit();

      KeyboardState state = Keyboard.GetState();
      if (state.IsKeyDown(Keys.D1)) {
        x -= .1f;
      } else if (state.IsKeyDown(Keys.D2)) {
        x += .1f;
      } else if (state.IsKeyDown(Keys.D3)) {
        y -= .1f;
      } else if (state.IsKeyDown(Keys.D4)) {
        y += .1f;
      } else if (state.IsKeyDown(Keys.D5)) {
        z -= .1f;
      } else if (state.IsKeyDown(Keys.D6)) {
        z += .1f;
      } else if (state.IsKeyDown(Keys.Up)) {
        magnitude += .05f;
        if (magnitude > 30) {
          magnitude = 30;
        }
      } else if (state.IsKeyDown(Keys.Down)) {
        magnitude -= .05f;
        if (magnitude < 5) {
          magnitude = 5;
        }
      } else if (state.IsKeyDown(Keys.Left)) {
        angle += .02f;
        if (angle > Math.PI / 2) {
          angle = (float)Math.PI / 2;
        }
      } else if (state.IsKeyDown(Keys.Right)) {
        angle -= .02f;
        if (angle < 0) {
          angle = 0;
        }
      }

      if (prevState.IsKeyUp(Keys.Space) && state.IsKeyDown(Keys.Space)) {
        Model cubeMode = Content.Load<Model>("sphere");
        Body body = new Body(this, cubeMode, toggle ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1), "sphere");
        body.SetWorld(new Vector3(0, 0, -9));
        toggle = !toggle;

        float yMag = (float) Math.Sin(angle) * magnitude;
        float zMag = (float) Math.Cos(angle) * magnitude;

        body.SetVelocity(new Vector3(0, yMag, zMag), Vector3.Zero);
        for (int i = 0; i < body.Skin.Count; i++) {
          body.Skin.SetMaterial(body.Skin[i], new Material(.5f, 1f));
        }
        physicsManager.Add(body);
        bodies.Add(body);
      }

      prevState = state;
      base.Update(gameTime);
    }

    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime) {
      GraphicsDevice.Clear(Color.CornflowerBlue);
      GraphicsDevice.BlendState = BlendState.Opaque;

      GraphicsDevice.RasterizerState = RasterizerState.CullNone;
      GraphicsDevice.DepthStencilState = DepthStencilState.Default;

      foreach (Body body in bodies) {
        body.Draw(Matrix.CreateLookAt(new Vector3(x, y, z), Vector3.Zero, Vector3.Up),
          Matrix.CreatePerspectiveFieldOfView(
          MathHelper.ToRadians(45f),
          GraphicsDevice.Viewport.AspectRatio,
          .01f,
          100f));
      }

      spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
      spriteBatch.DrawString(font, "Magnitude: " + magnitude, new Vector2(0, 0), Color.White);
      spriteBatch.DrawString(font, "Angle: " + angle, new Vector2(0, 20), Color.White);
      spriteBatch.End();

      base.Draw(gameTime);
    }
  }
}
