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

    private Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(43), 57f / 43f, .4f, 8f);
    private Matrix offset = Matrix.Identity;

    private float yRotation = 0;

    private Matrix[] globeTransforms;
    private Matrix[] chairTransforms;

    private bool drawChair = false;
    private bool drawOverlay = true;

    public GlobeModel(ContentManager content, GraphicsDevice GraphicsDevice, Matrix? offset = null) {
      // Grab transforms from models right now, not every frame
      //globe = content.Load<Model>("cube2overlay");
      globe = content.Load<Model>("globe");
      globeTransforms = new Matrix[globe.Bones.Count];
      globe.CopyAbsoluteBoneTransformsTo(globeTransforms);
      Console.WriteLine(globeTransforms[1]);
      //chair = content.Load<Model>("model2");
      chair = content.Load<Model>("chair");
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

    public void newDepth(float[] depths) {
      occlusion.updateInstanceInformation(depths);
    }

    public void update(int millis) {
      // Update the rotation around the y axis (earth spinning)
      // .2 radians / second
      yRotation += millis * 0.0002f;
      if (yRotation >= 2 * Math.PI) {
        yRotation %= (float)(2 * Math.PI);
      }
      data.setRotation(yRotation);
    }

    public void setOffset(Matrix offset) {
      this.offset = offset;
      data.setOffset(globeTransforms[1] * this.offset);
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
      occlusion.updateInstanceInformation(data);
    }

    public void setData(DataPoint[] dataPoints) {
      data.setData(dataPoints);
    }

    public void render(Matrix cameraPosition, GraphicsDevice GraphicsDevice) {
      // First draw the chair, if we want to see it, set the transparency to half, if not, set to 0
      // drawModel(chair, chairTransforms, Matrix.Identity, offset, cameraPosition, drawChair ? .5f : 0);

      // If we want to draw the model
      if (drawOverlay) {
        // Get the rotation matrix
        Matrix r = Matrix.CreateRotationY(yRotation);
        occlusion.draw();

        GraphicsDevice.BlendState = BlendState.Opaque;
        data.draw();

        // Finally, draw the globe,
        //  First rotate the globe when it is centered at the origin
        //  Then, apply the internal transformations, moving it away from the origin
        //  Last, if theres any offset, offset it
        drawModel(globe, globeTransforms, r, offset, cameraPosition);
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
