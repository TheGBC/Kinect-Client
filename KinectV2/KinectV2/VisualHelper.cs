using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectV2 {
  class VisualHelper {
    // Offset of map I used
    private static readonly float lngOffset = (float)Math.PI * 2 * 59 / 2000f;
    
    private VisualHelper() { }


    // x^2 + x/5 / (x^2 + 1) => horizontal asymptote y=1 and passes through origin
    // DON'T PLUG IN -1
    private static float stepFunction(float x) {
      //return (x * x + x / 5) / (x * x  + 1);
      return -1f + 2f / (1 + (float)Math.Exp(-x));
    }

    private static Matrix rotateTo(Vector2 src, Vector2 dest, float t) {
      float rescaledTime = t * 10;
      if (rescaledTime == 10) {
        return Matrix.CreateRotationY(dest.Y) * Matrix.CreateRotationX(dest.X);
      } else {
        float factor = stepFunction(rescaledTime);
        float dX = dest.X - src.X;
        float dY = dest.Y - src.Y;
        return Matrix.CreateRotationY(src.Y + dY * factor) * Matrix.CreateRotationX(src.X + dX * factor);
      }
    }

    public static Vector2 latlng(float lat, float lng) {
      return new Vector2(-MathHelper.ToRadians(lat), MathHelper.ToRadians(lng) - lngOffset);
    }

    public static Matrix LatLngRotation(float lat, float lng) {
      Vector2 rot = latlng(lat, lng);
      return Matrix.CreateRotationX(rot.X) * Matrix.CreateRotationY(rot.Y);
    }

    public static Matrix LatLongRotateTo(Vector2 current, Vector2 dest, float t) {
      return rotateTo(-latlng(current.X, current.Y), -latlng(dest.X, dest.Y), t);
    }

    // Magic hue to rgb calculating code.
    // Source: wikipedia
    public static Color hueToColor(float hue) {
      int h = (int)(hue * 6);
      float f = hue * 6 - h;
      float q = (1 - f);
      float t = (1 - (1 - f));

      switch (h) {
        case 0: return new Color(1, t, 0);
        case 1: return new Color(q, 1, 0);
        case 2: return new Color(0, 1, t);
        case 3: return new Color(0, q, 1);
        case 4: return new Color(t, 0, 1);
        case 5: return new Color(1, 0, q);
      }
      return new Color();
    }
  }
}
