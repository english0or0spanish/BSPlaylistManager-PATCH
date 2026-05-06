using System.IO;
using System.Reflection;
using UnityEngine;

namespace PlaylistManager.Utilities
{
    internal static class SpriteUtils
    {
        public static Sprite LoadSpriteFromAssembly(Assembly assembly, string resourceName, float pixelsPerUnit = 100f)
        {
            if (assembly == null || string.IsNullOrEmpty(resourceName))
            {
                return null;
            }

            using Stream resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                return null;
            }

            using MemoryStream memoryStream = new MemoryStream();
            resourceStream.CopyTo(memoryStream);
            return CreateSprite(memoryStream.ToArray(), pixelsPerUnit);
        }

        public static Sprite CreateSprite(byte[] imageData, float pixelsPerUnit = 100f)
        {
            if (imageData == null || imageData.Length == 0)
            {
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, mipChain: false, linear: false);
            if (!ImageConversion.LoadImage(texture, imageData))
            {
                return null;
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0u, SpriteMeshType.FullRect, Vector4.zero, generateFallbackPhysicsShape: false);
        }
    }
}
