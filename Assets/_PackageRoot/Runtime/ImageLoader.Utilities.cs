using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Extensions.Unity.ImageLoader
{
    public static class Utils
    {
        public enum SizeUnits
        {
            Byte, KB, MB, GB, TB, PB, EB, ZB, YB
        }

        public static string ToSize(Int64 value, SizeUnits unit = SizeUnits.MB)
        {
            return (value / (double)Math.Pow(1024, (Int64)unit)).ToString("0.00") + unit.ToString();
        }

        public static bool IsPowerOfTwo(int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }

        public static Texture2D CreateTexWithMipmaps(byte[] data, GraphicsFormat origGraphicsFormat, int height = 4, int width = 4, TextureFormat textureFormat = default, string name = "")
        {
            GraphicsFormat finalGraphicsFormat;
#if UNITY_2021_3_OR_NEWER
            bool hasAlpha = GraphicsFormatUtility.HasAlphaChannel(origGraphicsFormat);
            if (Utils.IsPowerOfTwo(width) && Utils.IsPowerOfTwo(height) && width == height)
            {
                // Standalone - RGBA_DXT5_SRGB/RGBA_DXT1_SRGB
                // Android - RGBA_ETC2_SRGB/RGB_ETC2_SRGB
#if UNITY_ANDROID
                finalGraphicsFormat = hasAlpha ? GraphicsFormat.RGBA_ETC2_SRGB : GraphicsFormat.RGB_ETC2_SRGB;
#else
                finalGraphicsFormat = hasAlpha ? GraphicsFormat.RGBA_DXT5_SRGB : GraphicsFormat.RGBA_DXT1_SRGB;
#endif
            }
            else
            {
                // graphicsFormat = hasAlpha ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8_SRGB;
                finalGraphicsFormat = GraphicsFormat.R8G8B8A8_SRGB;
            }
            // Debug.Log($"Format {finalGraphicsFormat} supported = " + SystemInfo.IsFormatSupported(finalGraphicsFormat, FormatUsage.Linear));
#else
            finalGraphicsFormat = GraphicsFormat.RGBA_DXT5_SRGB;
#endif
            
            TextureCreationFlags flags = TextureCreationFlags.MipChain;
            var loadedTexture = new Texture2D(width, height, finalGraphicsFormat, flags);
            // var loadedTexture = new Texture2D(width, height, textureFormat, true); // Generates mipmaps without compression
            loadedTexture.wrapMode = TextureWrapMode.Clamp;

            //try
            //{
            //    loadedTexture.LoadImage(data);
            //} catch (Exception e)
            //{
            //    Debug.Log($"CreateTexWithMipmaps: LoadImage failed error={e.Message}, using format={finalGraphicsFormat}, trying again with {GraphicsFormat.R8G8B8A8_SRGB}");
            //    finalGraphicsFormat = GraphicsFormat.R8G8B8A8_SRGB;
            //    loadedTexture = new Texture2D(width, height, finalGraphicsFormat, flags);
            //    if (loadedTexture.LoadImage(data) == false)
            //    {
            //        Debug.LogError($"CreateTexWithMipmaps: LoadImage failed, using format={finalGraphicsFormat}, returning null texture");
            //        loadedTexture = null;
            //    }
            //}

            if (loadedTexture.LoadImage(data) == false)
            {
                Debug.Log($"CreateTexWithMipmaps: LoadImage failed, using format={finalGraphicsFormat}, trying again with {GraphicsFormat.R8G8B8A8_SRGB}");
                finalGraphicsFormat = GraphicsFormat.R8G8B8A8_SRGB;
                loadedTexture = new Texture2D(width, height, finalGraphicsFormat, flags);
                if (loadedTexture.LoadImage(data) == false)
                {
                    Debug.LogError($"CreateTexWithMipmaps: LoadImage failed, using format={finalGraphicsFormat}, returning null texture");
                    loadedTexture = null;
                }
            }

            return loadedTexture;
        }

        public static Texture2D CreateTexWithMipmaps(byte[] data, bool shouldGenerateMipMaps = true, string name = "DynamicTex")
        {
            GraphicsFormat finalGraphicsFormat = GraphicsFormat.R8G8B8A8_SRGB;
//#if UNITY_ANDROID
//            finalGraphicsFormat = GraphicsFormat.R8G8B8A8_SRGB;
//            //TextureFormat textureFormat = TextureFormat.ETC2_RGB;
//#else
//            finalGraphicsFormat = GraphicsFormat.RGBA_DXT5_SRGB;
//#endif

            TextureCreationFlags flags = TextureCreationFlags.None;
            if (shouldGenerateMipMaps)
                flags = TextureCreationFlags.MipChain;

            // Debug.Log($"Format {finalGraphicsFormat} supported = " + SystemInfo.IsFormatSupported(finalGraphicsFormat, FormatUsage.Linear));
            // Debug.Log($"Format {textureFormat} supported = " + SystemInfo.SupportsTextureFormat(textureFormat));

            var loadedTexture = new Texture2D(4, 4, finalGraphicsFormat, flags) { 
                name = name+"-"+shouldGenerateMipMaps,
                wrapMode = TextureWrapMode.Clamp,
            };
            //var loadedTexture = new Texture2D(4, 4, textureFormat, true); // Generates mipmaps without compression
            // loadedTexture.wrapMode = TextureWrapMode.Clamp;

            if (loadedTexture.LoadImage(data) == false)
            {
                Debug.Log($"CreateTexWithMipmaps: LoadImage failed using format={finalGraphicsFormat}, size={loadedTexture.width}x{loadedTexture.height} trying again with {GraphicsFormat.R8G8B8A8_SRGB}");
                //finalGraphicsFormat = GraphicsFormat.R8G8B8A8_SRGB;
                //loadedTexture = new Texture2D(4, 4, finalGraphicsFormat, flags);
                //if (loadedTexture.LoadImage(data) == false)
                //{
                //    Debug.LogError($"CreateTexWithMipmaps: LoadImage failed, using format={finalGraphicsFormat}, returning null texture");
                //    loadedTexture = null;
                //}
            }

            if(loadedTexture.width % 4 == 0 && loadedTexture.height % 4 == 0)
                loadedTexture.Compress(false);

            return loadedTexture;
        }
    }
}
