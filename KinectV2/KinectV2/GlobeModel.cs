using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectV2 {
  class GlobeModel {
    private Model globe;
    private Model chair;
    private RenderData data;
    private RenderOcclusion occlusion;

    private Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(48.6f), 62f / 48.6f, .01f, 8f);
    private Matrix offset = Matrix.Identity;

    private float yRotation = 0;
    private float time = 0;

    private Matrix[] globeTransforms;
    private Matrix[] chairTransforms;

    private bool drawChair = false;
    private bool drawOverlay = true;

    private bool isRotating = false;
    private bool totalScene = true;
    private bool renderOverlay = false;

    private Vector2 currentPosition = new Vector2();
    private Vector2 destPosition = new Vector2();

    private Queue<Vector2> pendingLocations = new Queue<Vector2>();


    public GlobeModel(ContentManager content, GraphicsDevice GraphicsDevice, Matrix? offset = null) {
      // Grab transforms from models right now, not every frame
      //globe = content.Load<Model>("cube2overlay");
      globe = content.Load<Model>("globe2");
      globeTransforms = new Matrix[globe.Bones.Count];
      globe.CopyAbsoluteBoneTransformsTo(globeTransforms);
      chair = content.Load<Model>("model2");
      //chair = content.Load<Model>("chair");
      chairTransforms = new Matrix[chair.Bones.Count];
      chair.CopyAbsoluteBoneTransformsTo(chairTransforms);

      data = new RenderData(content, GraphicsDevice);
      occlusion = new RenderOcclusion(content, GraphicsDevice);

      // If there is an offset, set the offset
      if (offset.HasValue) {
        this.offset = offset.Value;
      }

      data.setOffset(globeTransforms[1] * this.offset);
    }

    public void update(int millis) {
      // Update the rotation around the y axis (earth spinning)
      // .2 radians / second
      yRotation += millis * 0.0002f;
      if (yRotation >= 2 * Math.PI) {
        yRotation %= (float)(2 * Math.PI);
      }

      if (isRotating) {
        time += millis / 5000f;
        if (time > 1) {
          time = 1;
          isRotating = false;
          currentPosition.X = destPosition.X;
          currentPosition.Y = destPosition.Y;
          if (pendingLocations.Count > 0) {
            Vector2 point = pendingLocations.Dequeue();
            rotateTo(point.X, point.Y);
          }
        }
      }
    }

    public void rotateTo(float lat, float lng) {
      if (!isRotating) {
        destPosition.X = lat;
        destPosition.Y = lng;
        time = 0;
        isRotating = true;
      } else {
        pendingLocations.Enqueue(new Vector2(lat, lng));
      }
    }

    public void setOffset(Matrix offset) {
      this.offset = offset;
      data.setOffset(globeTransforms[1] * this.offset);
    }

    public void toggleSceneOcclusion() {
      totalScene = !totalScene;
    }

    public void toggleRenderOverlay() {
      renderOverlay = !renderOverlay;
    }

    public void toggleChairDraw() {
      drawChair = !drawChair;
    }

    public void toggleOverlayDraw() {
      drawOverlay = !drawOverlay;
    }

    public void toggleTile() {
      data.toggleTile();
    }

    public void updateOcclusion(float[] data) {
      if (totalScene) {
        occlusion.updateInstanceInformation(data);
      }
    }

    public void setData(DataPoint[] dataPoints) {
      data.setData(dataPoints);
    }

    public void render(Matrix cameraPosition, GraphicsDevice GraphicsDevice) {
      // First draw the chair, if we want to see it, set the transparency to half, if not, set to 0
      if (renderOverlay) {
        drawModel(chair, chairTransforms, Matrix.Identity, offset, cameraPosition, drawChair ? .5f : 0);
      }

      // If we want to draw the model
      if (drawOverlay) {
        // Get the rotation matrix
        //Matrix r = Matrix.CreateRotationY(yRotation);
        Matrix r = VisualHelper.LatLongRotateTo(currentPosition, destPosition, time);
        if (totalScene) {
          occlusion.draw();
        }

        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.BlendState = BlendState.AlphaBlend;
        data.setView(cameraPosition);
        drawModel(globe, globeTransforms, r, offset, cameraPosition);
        data.draw(r);

        // Finally, draw the globe,
        //  First rotate the globe when it is centered at the origin
        //  Then, apply the internal transformations, moving it away from the origin
        //  Last, if theres any offset, offset it
      }
    }

    private void drawModel(Model m, Matrix[] transforms, Matrix preWorld, Matrix postWorld, Matrix view, float? alpha = null, Color? color = null) {
      for (int i = 0; i < m.Meshes.Count; i++) {
        ModelMesh mesh = m.Meshes[i];
        for (int j = 0; j < mesh.Effects.Count; j++) {
          BasicEffect effect = (BasicEffect) mesh.Effects[j];

          // If we want to change alpha, change alpha
          if (alpha.HasValue) {
            effect.Alpha = alpha.Value;
          }
          effect.EnableDefaultLighting();
          effect.World = preWorld * transforms[mesh.ParentBone.Index] * postWorld;
          effect.View = view;
          effect.Projection = projection;
          effect.PreferPerPixelLighting = true;

          // If we want to change the color, change it
          if (color.HasValue) {
            effect.DiffuseColor = color.Value.ToVector3();
            effect.AmbientLightColor = Color.Gray.ToVector3();
          }
        }
        mesh.Draw();
      }
    }

    
  }
}
