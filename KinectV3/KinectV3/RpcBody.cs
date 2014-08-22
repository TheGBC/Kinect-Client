using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectV3 {
  class RpcBody {
    private Model _model;
    private Matrix[] _meshTransforms;
    private string tag;

    public RpcBody(Model model, string tag) {
      _model = model;
      _meshTransforms = new Matrix[_model.Bones.Count];
      _model.CopyAbsoluteBoneTransformsTo(_meshTransforms);

      this.tag = tag;
      foreach (var mesh in _model.Meshes) {
        foreach (BasicEffect effect in mesh.Effects) {
          effect.EnableDefaultLighting();
          effect.AmbientLightColor = Vector3.One * 0.75f;
          effect.SpecularColor = Vector3.One;
          effect.PreferPerPixelLighting = true;
        }
      }
    }

    public string Tag {
      get {
        return tag;
      }
    }

    public void Draw(Matrix view, Matrix projection, Matrix offset, List<Matrix> transforms) {
      foreach (Matrix transform in transforms) {
        foreach (var mesh in _model.Meshes) {
          foreach (BasicEffect effect in mesh.Effects) {
            effect.World = _meshTransforms[mesh.ParentBone.Index] * transform * offset;
            effect.View = view;
            effect.Projection = projection;
            effect.DiffuseColor = new Vector3(0, 0, 1);
          }
          mesh.Draw();
        }
      }
    }
  }
}
