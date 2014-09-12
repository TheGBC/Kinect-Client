using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace KinectV3 {
  class JpgWriter { 
    byte[] textureData; 
    Bitmap bmp; 
    BitmapData bitmapData; 
    IntPtr safePtr; 
    Rectangle rect; 
    public ImageFormat imageFormat; 
    public JpgWriter(int width, int height)  { 
      textureData = new byte[4 * width * height]; 
      bmp = new System.Drawing.Bitmap( 
          width, height, 
          System.Drawing.Imaging.PixelFormat.Format32bppArgb); 
      rect = new System.Drawing.Rectangle(0, 0, width, height); 
      imageFormat = System.Drawing.Imaging.ImageFormat.Jpeg; 
    } 
    
    public void TextureToJpg(RenderTarget2D texture, Stream stream)  {
      texture.GetData<byte>(textureData); 
      byte blue; 
      for (int i = 0; i < textureData.Length; i += 4) { 
        blue = textureData[i]; 
        textureData[i] = textureData[i+2]; 
        textureData[i + 2] = blue; 
      }
      bitmapData = bmp.LockBits( 
          rect, 
          System.Drawing.Imaging.ImageLockMode.WriteOnly, 
          System.Drawing.Imaging.PixelFormat.Format32bppArgb); 
      safePtr = bitmapData.Scan0; 
      System.Runtime.InteropServices.Marshal.Copy(textureData, 0, safePtr, textureData.Length); 
      bmp.UnlockBits(bitmapData);
      bmp.Save(stream, imageFormat); 
    } 
  }
}
