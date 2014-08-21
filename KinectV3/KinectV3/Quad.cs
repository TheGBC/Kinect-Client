using KinectV2;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectV3 {
  class Quad {
    private VertexPositionNormalTexture[] Vertices = new VertexPositionNormalTexture[4];
    private short[] Indexes = new short[6];
    private Vector3 Origin = new Vector3(0, 0, -10f);
    private Vector3 Normal = Vector3.UnitZ;
    private Vector3 Up = Vector3.Up;
    private Vector3 Left = Vector3.Left;
    private Vector3 UpperLeft;
    private Vector3 UpperRight;
    private Vector3 LowerLeft;
    private Vector3 LowerRight;

    private BasicEffect effect;
    private VertexDeclaration vertexDeclaration = new VertexDeclaration(
      new VertexElement[] {
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
      });

    private GraphicsDevice graphicsDevice;

    public Quad(GraphicsDevice graphics, Texture2D texture) {
      graphicsDevice = graphics;
      effect = new BasicEffect(graphics);
      effect.World = Matrix.Identity;
      effect.View = Matrix.Identity;
      effect.TextureEnabled = true;
      effect.Texture = texture;

      // Calculate the quad corners
      float width = 12.02f;
      float height = 12.02f * KinectManager.IMG_HEIGHT / (float) KinectManager.IMG_WIDTH;
      Vector3 uppercenter = (Up * height / 2) + Origin;
      UpperLeft = uppercenter + (Left * width / 2);
      UpperRight = uppercenter - (Left * width / 2);
      LowerLeft = UpperLeft - (Up * height);
      LowerRight = UpperRight - (Up * height);

      // Fill in texture coordinates to display full texture
      // on quad
      Vector2 textureUpperLeft = new Vector2(0.0f, 0.0f);
      Vector2 textureUpperRight = new Vector2(1.0f, 0.0f);
      Vector2 textureLowerLeft = new Vector2(0.0f, 1.0f);
      Vector2 textureLowerRight = new Vector2(1.0f, 1.0f);

      // Provide a normal for each vertex
      for (int i = 0; i < Vertices.Length; i++) {
        Vertices[i].Normal = Normal;
      }
      Vertices[0].Position = LowerLeft;
      Vertices[0].TextureCoordinate = textureLowerLeft;
      Vertices[1].Position = UpperLeft;
      Vertices[1].TextureCoordinate = textureUpperLeft;
      Vertices[2].Position = LowerRight;
      Vertices[2].TextureCoordinate = textureLowerRight;
      Vertices[3].Position = UpperRight;
      Vertices[3].TextureCoordinate = textureUpperRight;

      // Set the index buffer for each vertex, using
      // clockwise winding
      Indexes[0] = 0;
      Indexes[1] = 1;
      Indexes[2] = 2;
      Indexes[3] = 2;
      Indexes[4] = 1;
      Indexes[5] = 3;
    }

    public void Draw(Matrix projection) {
      effect.Projection = projection;
      foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
        pass.Apply();
        graphicsDevice.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(
            PrimitiveType.TriangleList,
            Vertices, 0, 4,
            Indexes, 0, 2);
      }
    }
  }
}
