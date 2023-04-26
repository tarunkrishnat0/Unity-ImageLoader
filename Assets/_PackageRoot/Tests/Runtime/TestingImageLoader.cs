using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.Profiling;
using Unity.Profiling;
using System.Threading;
using System.Threading.Tasks;
using System;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.Experimental.Rendering;
using UnityEditorInternal;

namespace Extensions.Unity.ImageLoader.Testing
{
    public class TestingImageLoader : MonoBehaviour
    {
        [SerializeField] List<Texture2D> textures = new List<Texture2D>();
        [SerializeField] private Image image;

        static ProfilerMarker s_SimulatePerfMarker = new ProfilerMarker("Profile.LoadingFromMemoryCache");

        static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        readonly string[] ImageURLs =
            {
            "https://github.com/tarunkrishnat0/Unity-ImageLoader/raw/master/Test%20Images/ImageA.jpg",
            "https://github.com/tarunkrishnat0/Unity-ImageLoader/raw/master/Test%20Images/ImageB.png",
            "https://github.com/tarunkrishnat0/Unity-ImageLoader/raw/master/Test%20Images/ImageC.png",
            "https://github.com/tarunkrishnat0/Unity-ImageLoader/raw/master/Test%20Images/batsman_512.png",
            "https://github.com/tarunkrishnat0/Unity-ImageLoader/raw/master/Test%20Images/batsman_1000.jpg",
        };

        // Start is called before the first frame update
        async void Start()
        {
            spriteCache.Clear();

            foreach (var tex in textures)
            {
                Debug.Log($"LocalLoader: name={tex.name}, size={Utils.ToSize(tex.GetRawTextureData().Length)}, mipmap={tex.mipmapCount}, format={tex.format}, graphicsFormat={tex.graphicsFormat}");
            }

            StartCoroutine(StartTests());
        }

        IEnumerator StartTests()
        {
            yield return LoadingFromImageLoader();
            yield return LoadingFromImageLoaderMemoryOptimized();
        }

        private void OnApplicationQuit()
        {
            ImageLoader.ClearCache().AsUniTask().ToCoroutine();
        }

        void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 150, 100), "Destroy Sprite"))
            {
                Debug.Log("Destroy Sprite");
                Destroy(image.sprite.texture);
                Destroy(image.sprite);
                // image.sprite = null;
            }
        }

        public async UniTask LoadSprite(string url, Image outputImage = null)
        {
            var sprite = await ImageLoader.LoadSprite(url);
            if (outputImage != null)
                outputImage.sprite = sprite;
            Assert.IsNotNull(sprite);
        }

        public async UniTask LoadSpriteMemoryOptimized(string url, Image outputImage = null)
        {
            var sprite = await ImageLoader.LoadSpriteMemoryOptimized(url);
            if (outputImage != null)
                outputImage.sprite = sprite;
            Assert.IsNotNull(sprite);
        }

        public IEnumerator LoadingFromImageLoader()
        {
            yield return ImageLoader.ClearCache().AsUniTask().ToCoroutine();

            int memSize = 0;
            
            ImageLoader.settings.useMemoryCache = true;
            ImageLoader.settings.useDiskCache = false;

            foreach (var imageURL in ImageURLs)
            {
                yield return LoadSprite(imageURL, image).ToCoroutine();
                memSize += image.overrideSprite.texture.GetRawTextureData().Length;
            }

            //yield return LoadSprite(ImageURLs[ImageURLs.Length - 2], image);

            Debug.Log($"<color=lime>LoadingFromImageLoader: Memory Size = {Utils.ToSize(memSize)}</color>");
        }

        public IEnumerator LoadingFromImageLoaderMemoryOptimized()
        {
            yield return ImageLoader.ClearCache().AsUniTask().ToCoroutine();

            int memSize = 0;

            ImageLoader.settings.useMemoryCache = true;
            ImageLoader.settings.useDiskCache = false;

            foreach (var imageURL in ImageURLs)
            {
                yield return LoadSpriteMemoryOptimized(imageURL, image).ToCoroutine();
                memSize += image.overrideSprite.texture.GetRawTextureData().Length;
            }

            //yield return LoadSprite(ImageURLs[ImageURLs.Length - 2], image);
            Debug.Log($"<color=lime>LoadingFromImageLoaderMemoryOptimized: Memory Size = {Utils.ToSize(memSize)}</color>");
        }

        public async void LoadingFromMemoryCacheAsync()
        {
            var context = SynchronizationContext.Current;
            var samplerThread = CustomSampler.Create("thread");
            var samplerMain = CustomSampler.Create("main");

            //yield return ImageLoader.ClearCache().AsUniTask().ToCoroutine();
            ImageLoader.settings.useMemoryCache = true;
            ImageLoader.settings.useDiskCache = false;

            await Task.Run(async () =>
            {
                Profiler.BeginThreadProfiling("group", "name");
                foreach (var imageURL in ImageURLs)
                {
                    samplerThread.Begin();

                    await LoadSprite(imageURL);
                    //Assert.IsTrue(ImageLoader.MemoryCacheContains(imageURL));
                    //yield return LoadSprite(imageURL).ToCoroutine();
                    //Assert.IsTrue(ImageLoader.MemoryCacheContains(imageURL));

                    samplerThread.End();
                }
                Profiler.EndThreadProfiling();
            });

            await LoadSprite(ImageURLs[ImageURLs.Length - 2], image);
        }

        public IEnumerator LoadSpriteTestingCoroutine()
        {
            ProfilerDriver.enabled = true;
            Profiler.enabled = true;

            yield return new WaitForSeconds(1);

            foreach (var imageURL in ImageURLs)
            {
                yield return LoadSpriteTesting(imageURL);
            }

            image.sprite = spriteCache[ImageURLs[3]];

            GC.Collect();
            AsyncOperation asyncOperation = Resources.UnloadUnusedAssets();
            yield return new WaitUntil(() => asyncOperation.isDone);

            yield return new WaitForSeconds(0.2f);

            Profiler.enabled = false;
            ProfilerDriver.enabled = false;
        }

        private IEnumerator LoadSpriteTesting(string URL)
        {
            // UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(URL);
            UnityWebRequest uwr = new UnityWebRequest(URL);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.timeout = 30;

            // Fire the request
            yield return uwr.SendWebRequest();

            string name = Path.GetFileNameWithoutExtension(URL);
            Profiler.BeginSample("LoadSpriteTesting " + name);
            if (true)
            {
                if (isError(uwr) || !uwr.isDone)
                {
                    Debug.Log($"Download failed : url={URL}, error=" + uwr.error);
                }
                else
                {
                    //Texture2D tex = ((DownloadHandlerTexture)uwr.downloadHandler).texture;
                    //bool hasAlpha = GraphicsFormatUtility.HasAlphaChannel(tex.graphicsFormat);
                    //Debug.Log($"LoadSpriteTesting: before name={name}, size={Utils.ToSize(tex.GetRawTextureData().Length)}, mipmap={tex.mipmapCount}, format={tex.format}, graphicsFormat={tex.graphicsFormat}, dimensions={tex.width}x{tex.height}, hasAlpha={hasAlpha}");

                    Debug.Log($"LoadSpriteTesting: before name={name}, data size={Utils.ToSize(uwr.downloadHandler.data.Length)}");

                    var loadedTexture = Utils.CreateTexWithMipmaps(uwr.downloadHandler.data, GraphicsFormat.R8G8B8A8_SRGB);

                    Debug.Log($"LoadSpriteTesting: after name={name}, size={Utils.ToSize(loadedTexture.GetRawTextureData().Length)}, mipmap={loadedTexture.mipmapCount}, format={loadedTexture.format}, graphicsFormat={loadedTexture.graphicsFormat}, dimensions={loadedTexture.width}x{loadedTexture.height}");

                    Sprite downloadedSprite = Sprite.Create(
                        loadedTexture,
                        new Rect(0, 0, loadedTexture.width, loadedTexture.height),
                        Vector2.zero,
                        100f,
                        0,
                        SpriteMeshType.FullRect);

                    // img.sprite = downloadedSprite;
                    spriteCache.Add(URL, downloadedSprite);

                    /*
                     * https://github.com/mapbox/mapbox-sdk-cs/issues/31
                     * http://answers.unity.com/answers/474657/view.html
                     * https://forum.unity.com/threads/unitywebrequesttexture-memory-leak.742490/
                     * 
                     * Getter for the texture in the www class seems to be doing something that prevents the destruction, what ultimatelly worked for me was:
                     *      Texture2D texture = www.texture;
                     *      //use texture
                     *      GameObject.Destroy(texture); www.Dispose();
                     * 
                     * We are destroying the texture only if we are able to generate compressed texture with mip map.
                     */
                    uwr.Dispose();
                }
            }
            Profiler.EndSample();
        }

        private bool isError(UnityWebRequest m_unityWebRequest)
        {
            if (m_unityWebRequest.isNetworkError || m_unityWebRequest.isHttpError)
                return true;
            return false;
        }
    }
}