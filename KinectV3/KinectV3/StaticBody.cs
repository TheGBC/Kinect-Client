using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectV3 {
  class StaticBody {
    private Model _model;
    private Matrix world;
    private Matrix[] _meshTransforms;
    private string tag;
    private bool transparent;

    public StaticBody(Model model, Matrix world, string tag, bool transparent) {
      this.world = world;
      _model = model;

      _meshTransforms = new Matrix[_model.Bones.Count];
      Console.WriteLine(_meshTransforms.Length);
      _model.CopyAbsoluteBoneTransformsTo(_meshTransforms);

      this.tag = tag;
      this.transparent = transparent;
      foreach (var mesh in _model.Meshes) {
        foreach (BasicEffect effect in mesh.Effects) {
          effect.EnableDefaultLighting();
          effect.AmbientLightColor = Vector3.One * 0.75f;
          effect.SpecularColor = Vector3.One;
          effect.PreferPerPixelLighting = true;
        }
      }
    }

    public Matrix MeshTransform {
      get {
        return _meshTransforms[0] * world;
      }
    }

    public void Draw(Matrix view, Matrix projection, Matrix offset, bool disp = true) {
      foreach (var mesh in _model.Meshes) {
        foreach (BasicEffect effect in mesh.Effects) {
          effect.World = _meshTransforms[mesh.ParentBone.Index] * world * offset;
          effect.View = view;
          effect.Projection = projection;
          if (this.transparent) {
            effect.Alpha = .5f;
          }
          if (!disp) {
            effect.Alpha = 0;
          }
        }
        mesh.Draw();
      }
    }
  }
}
