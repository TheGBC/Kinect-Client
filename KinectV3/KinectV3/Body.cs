using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Henge3D.Physics;
using Henge3D.Pipeline;

namespace KinectV3 {
  public class Body : RigidBody {
    private string tag = "";

    private static Random _colorRand = new Random();

    private Model _model;
    private Matrix[] _meshTransforms;
    private Vector3 _diffuseColor;

    public Body(Game game, Model model, string tag)
      : this(game, model, new Vector3((float)_colorRand.NextDouble(), (float)_colorRand.NextDouble(), (float)_colorRand.NextDouble()), tag) {
    }

    public Body(Game game, Model model, Vector3 color, string tag)
      : base((RigidBodyModel)model.Tag) {
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
      _diffuseColor = color;
      _diffuseColor *= 0.6f;
    }

    public string Tag {
      get {
        return tag;
      }
    }

    public void Draw(Matrix view, Matrix projection, Matrix offset) {
      foreach (var mesh in _model.Meshes) {
        foreach (BasicEffect effect in mesh.Effects) {
          effect.World = _meshTransforms[mesh.ParentBone.Index] * Transform.Combined * offset;
          effect.View = view;
          effect.Projection = projection;
          effect.DiffuseColor = _diffuseColor;
          if (!this.IsActive) {
            effect.DiffuseColor *= 0.5f;
          }
        }
        mesh.Draw();
      }
    }
  }
}
