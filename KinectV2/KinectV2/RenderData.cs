using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectV2 {
  class RenderData {
    private Matrix Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(48.6f), 62f / 48.6f, .01f, 8f);
    private Matrix PreOffset = Matrix.CreateTranslation(0, 0, 1);
    
    private readonly Color Invisible = new Color(0, 0, 0, 0);

    private Matrix View = Matrix.Identity;
    private Effect effect;
    private VertexBuffer geometryBuffer;
    private VertexBuffer tileGeometryBuffer;
    private VertexBuffer instanceBuffer;
    private VertexBufferBinding[] bindings;
    private IndexBuffer indexBuffer;
    private IndexBuffer tileIndexBuffer;
    private InstanceInfo[] instances;
    private VertexDeclaration instanceVertexDeclaration;
    private GraphicsDevice GraphicsDevice;

    private bool useTile = false;
    private Matrix[] transforms;
    private Matrix Offset = Matrix.Identity;

    public RenderData(ContentManager Content, GraphicsDevice GraphicsDevice) {
      // Load the shader
      effect = Content.Load<Effect>("shader");
      this.GraphicsDevice = GraphicsDevice;
      GenerateInstanceVertexDeclaration();
      generateVertexBuffers();
    }

    public void setData(DataPoint[] dataPoints) {
      transforms = new Matrix[dataPoints.Length];
      Color[] colors = new Color[dataPoints.Length];
      for (int i = 0; i < dataPoints.Length; i++) {
        DataPoint point = dataPoints[i];
        transforms[i] = Matrix.CreateScale(1, 1, point.value) * PreOffset
              * VisualHelper.LatLngRotation(point.lat, point.lng);
        colors[i] = point.value == 0 ? Invisible : VisualHelper.hueToColor((1 - point.value) / 2);
      }
      GenerateInstanceInformation(GraphicsDevice, transforms, colors);
      bindings = new VertexBufferBinding[2];
      bindings[0] = new VertexBufferBinding(useTile ? tileGeometryBuffer : geometryBuffer);
      bindings[1] = new VertexBufferBinding(instanceBuffer, 0, 1);
    }

    public void setOffset(Matrix offset) {
      Offset = offset;
    }

    public void setView(Matrix view) {
      View = view;
    }

    public void toggleTile() {
      useTile = !useTile;
      bindings = new VertexBufferBinding[2];
      bindings[0] = new VertexBufferBinding(useTile ? tileGeometryBuffer : geometryBuffer);
      bindings[1] = new VertexBufferBinding(instanceBuffer, 0, 1);
    }

    public void draw(Matrix rotation) {
      if (transforms != null) {
        effect.CurrentTechnique = effect.Techniques["Instancing"];
        effect.Parameters["View"].SetValue(View);
        effect.Parameters["Projection"].SetValue(Projection);
        effect.Parameters["Rotation"].SetValue(rotation);
        effect.Parameters["PostWorld"].SetValue(Offset);
        effect.Parameters["PostWorldInverse"].SetValue(Matrix.Invert(Offset));
        GraphicsDevice.Indices = useTile ? tileIndexBuffer : indexBuffer;
        effect.CurrentTechnique.Passes[0].Apply();
        GraphicsDevice.SetVertexBuffers(bindings);

        GraphicsDevice.DrawInstancedPrimitives(
            PrimitiveType.TriangleList,
            0, 0, useTile ? tileGeometryBuffer.VertexCount : geometryBuffer.VertexCount,
            0, useTile ? tileIndexBuffer.IndexCount / 3 : indexBuffer.IndexCount / 3, transforms.Length);
      }
    }

    private void GenerateInstanceVertexDeclaration() {
      VertexElement[] instanceStreamElements = new VertexElement[9];
      instanceStreamElements[0] = new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0);
      instanceStreamElements[1] = new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1);
      instanceStreamElements[2] = new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2);
      instanceStreamElements[3] = new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3);
      instanceStreamElements[4] = new VertexElement(64, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 4);
      instanceStreamElements[5] = new VertexElement(80, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 5);
      instanceStreamElements[6] = new VertexElement(96, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 6);
      instanceStreamElements[7] = new VertexElement(112, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 7);
      instanceStreamElements[8] = new VertexElement(128, VertexElementFormat.Vector4, VertexElementUsage.Color, 1);

      instanceVertexDeclaration = new VertexDeclaration(instanceStreamElements);
    }

    private void generateVertexBuffers() {
      VertexPositionColorNormal[] vertices = new VertexPositionColorNormal[24];
      vertices[0].Position = new Vector3(-.0025f, .0025f, .5f);
      vertices[1].Position = new Vector3(-.0025f, -.0025f, .5f);
      vertices[2].Position = new Vector3(.0025f, .0025f, .5f);
      vertices[3].Position = new Vector3(.0025f, -.0025f, .5f);

      vertices[4].Position = new Vector3(.0025f, .0025f, .5f);
      vertices[5].Position = new Vector3(.0025f, -.0025f, .5f);
      vertices[6].Position = new Vector3(.0025f, .0025f, 0);
      vertices[7].Position = new Vector3(.0025f, -.0025f, 0);

      vertices[8].Position = new Vector3(.0025f, .0025f, 0);
      vertices[9].Position = new Vector3(.0025f, -.0025f, 0);
      vertices[10].Position = new Vector3(-.0025f, .0025f, 0);
      vertices[11].Position = new Vector3(-.0025f, -.0025f, 0);

      vertices[12].Position = new Vector3(-.0025f, .0025f, .5f);
      vertices[13].Position = new Vector3(-.0025f, -.0025f, .5f);
      vertices[14].Position = new Vector3(-.0025f, .0025f, 0);
      vertices[15].Position = new Vector3(-.0025f, -.0025f, 0);

      vertices[16].Position = new Vector3(-.0025f, .0025f, .5f);
      vertices[17].Position = new Vector3(.0025f, .0025f, .5f);
      vertices[18].Position = new Vector3(.0025f, .0025f, 0);
      vertices[19].Position = new Vector3(-.0025f, .0025f, 0);

      vertices[20].Position = new Vector3(-.0025f, -.0025f, .5f);
      vertices[21].Position = new Vector3(.0025f, -.0025f, .5f);
      vertices[22].Position = new Vector3(.0025f, -.0025f, 0);
      vertices[23].Position = new Vector3(-.0025f, -.0025f, 0);

      for (int i = 0; i < vertices.Length; i++) {
        vertices[i].Color = Color.White;

        switch (i / 4) {
          case 0:
            vertices[i].Normal = Vector3.UnitZ;
            break;
          case 1:
            vertices[i].Normal = Vector3.UnitX;
            break;
          case 2:
            vertices[i].Normal = -Vector3.UnitZ;
            break;
          case 3:
            vertices[i].Normal = -Vector3.UnitX;
            break;
          case 4:
            vertices[i].Normal = Vector3.UnitY;
            break;
          case 5:
            vertices[i].Normal = -Vector3.UnitY;
            break;
        }
        vertices[i].Normal.Normalize();
      }

      geometryBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColorNormal.VertexDeclaration,
          vertices.Length, BufferUsage.WriteOnly);
      geometryBuffer.SetData(vertices);
      tileGeometryBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColorNormal.VertexDeclaration, 4, BufferUsage.WriteOnly);
      tileGeometryBuffer.SetData(vertices, 0, 4);

      short[] indices = new short[36] {
          2, 1, 0, 1, 2, 3,
          6, 5, 4, 7, 5, 6,
          10, 9, 8, 11, 9, 10,
          12, 15, 14, 13, 15, 12,
          17, 16, 19, 17, 19, 18,
          23, 20, 21, 22, 23, 21
      };
      indexBuffer = new IndexBuffer(GraphicsDevice, typeof(short), indices.Length, BufferUsage.WriteOnly);
      indexBuffer.SetData(indices);
      tileIndexBuffer = new IndexBuffer(GraphicsDevice, typeof(short), 6, BufferUsage.WriteOnly);
      tileIndexBuffer.SetData(indices, 0, 6);
    }

    private void GenerateInstanceInformation(GraphicsDevice device, Matrix[] worlds, Color[] colors) {
      instances = new InstanceInfo[worlds.Length];
      for (int i = 0; i < instances.Length; i++) {
        instances[i].PreWorld = worlds[i];
        instances[i].PreWorldInverse = Matrix.Invert(worlds[i]);
        instances[i].Color = colors[i].ToVector4();
      }
      instanceBuffer = new VertexBuffer(device, instanceVertexDeclaration, worlds.Length, BufferUsage.WriteOnly);
      instanceBuffer.SetData(instances);
    }

    struct InstanceInfo {
      public Matrix PreWorld;
      public Matrix PreWorldInverse;
      public Vector4 Color;
    };

    struct VertexPositionColorNormal {
      public Vector3 Position;
      public Color Color;
      public Vector3 Normal;

      public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
      (
          new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
          new VertexElement(sizeof(float) * 3, VertexElementFormat.Color, VertexElementUsage.Color, 0),
          new VertexElement(sizeof(float) * 3 + 4, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
      );
    }
  }
}
