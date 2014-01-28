using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectV2 {
  class RenderOcclusion {
    private readonly int WIDTH = 640;
    private readonly int HEIGHT = 480;
    private readonly Matrix VP = Matrix.CreatePerspective(640, 480, .4f, 8f);

    private VertexBuffer geometryBuffer;
    private VertexBuffer instanceBuffer;
    private VertexDeclaration instanceVertexDeclaration;
    private VertexBufferBinding[] bindings;
    private IndexBuffer indexBuffer;
    private GraphicsDevice GraphicsDevice;
    private InstanceInfo[] instances;
    private Effect effect;
    private Color Invisible = new Color(1, 1, 1, 0);

    public RenderOcclusion(ContentManager Content, GraphicsDevice GraphicsDevice) {
      effect = Content.Load<Effect>("occlusionshader");
      this.GraphicsDevice = GraphicsDevice;
      GenerateGeometry();
      GenerateInstanceVertexDeclaration();
      GenerateInstanceInformation(GraphicsDevice);
      bindings = new VertexBufferBinding[2];
      bindings[0] = new VertexBufferBinding(geometryBuffer);
      bindings[1] = new VertexBufferBinding(instanceBuffer, 0, 1);
    }

    public void draw() {
        effect.CurrentTechnique = effect.Techniques["Instancing"];
        effect.Parameters["VP"].SetValue(VP);
        GraphicsDevice.Indices = indexBuffer;
        effect.CurrentTechnique.Passes[0].Apply();
        GraphicsDevice.SetVertexBuffers(bindings);
        GraphicsDevice.DrawInstancedPrimitives(
            PrimitiveType.TriangleList,
            0, 0, geometryBuffer.VertexCount,
            0, indexBuffer.IndexCount / 3, WIDTH * HEIGHT);
    }

    private void GenerateGeometry() {
      VertexPositionColor[] vertices = new VertexPositionColor[4];
      vertices[0].Position = new Vector3(-.5f, .5f, 0);
      vertices[1].Position = new Vector3(-.5f, -.5f, 0);
      vertices[2].Position = new Vector3(.5f, .5f, 0);
      vertices[3].Position = new Vector3(.5f, -.5f, 0);

      for (int i = 0; i < vertices.Length; i++) {
        vertices[i].Color = Color.White;
      }
      geometryBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration,
          vertices.Length, BufferUsage.WriteOnly);
      geometryBuffer.SetData(vertices);

      short[] indices = new short[6] { 2, 1, 0, 1, 2, 3 };
      indexBuffer = new IndexBuffer(GraphicsDevice, typeof(short), indices.Length, BufferUsage.WriteOnly);
      indexBuffer.SetData(indices);
    }

    private void GenerateInstanceVertexDeclaration() {
      VertexElement[] instanceStreamElements = new VertexElement[4];
      instanceStreamElements[0] = new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0);
      instanceStreamElements[1] = new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1);
      instanceStreamElements[2] = new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2);
      instanceStreamElements[3] = new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3);
      instanceVertexDeclaration = new VertexDeclaration(instanceStreamElements);
    }

    private void GenerateInstanceInformation(GraphicsDevice device) {
      instances = new InstanceInfo[WIDTH * HEIGHT];
      for (int y = 0; y < HEIGHT; y++ ) {
        for (int x = 0; x < WIDTH; x++) {
          float scale = 4f / .4f;
          instances[y * WIDTH + x].World = Matrix.CreateTranslation(x - (WIDTH / 2), y - (HEIGHT / 2), 0)
              * Matrix.CreateScale(scale) * Matrix.CreateTranslation(0, 0, -8f);
        }
      }
      instanceBuffer = new VertexBuffer(device, instanceVertexDeclaration, instances.Length, BufferUsage.WriteOnly);
      instanceBuffer.SetData(instances);
    }

    public void updateInstanceInformation(float[] values) {
      for (int y = 0; y < HEIGHT; y++) {
        for (int x = 0; x < WIDTH; x++) {
          int i = y * WIDTH + x;
          float scale = values[i]/ .4f;
          // We don't need to multiply new matrices and slow it down
          // scale   0       0     0
          // 0       scale   0     0
          // 0       0       scale 0
          // x*scale y*scale -z    1
          instances[i].World.M11 = scale;
          instances[i].World.M22 = scale;
          instances[i].World.M33 = scale;
          instances[i].World.M41 = scale * (x - (WIDTH / 2));
          instances[i].World.M42 = scale * (y - (HEIGHT / 2));
          instances[i].World.M43 = -scale;
        }
      }
      instanceBuffer.SetData(instances);
    }

    struct InstanceInfo {
      public Matrix World;
    };
  }
}