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

#if UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace Extensions.Unity.ImageLoader.Testing
{
    public class TestingImageLoader : MonoBehaviour
    {
        [SerializeField] List<Texture2D> textures = new List<Texture2D>();
        [SerializeField] private Image image;

        [SerializeField] private Button clearLogsButton;
        [SerializeField] private Button quitButton;

        static ProfilerMarker s_SimulatePerfMarker = new ProfilerMarker("Profile.LoadingFromMemoryCache");

        static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        private readonly static string base_url = "https://github.com/tarunkrishnat0/Unity-ImageLoader/raw/2021.3.23f1/feature/memory-optimization/Test%20Images/";
        //private readonly static string base_url = "http://localhost/data/";

        readonly string[] ImageURLs =
        {
            $"{base_url}ImageA.jpg",
            $"{base_url}ImageB.png",
            $"{base_url}ImageC.png",
            $"{base_url}batsman_512.png",
            $"{base_url}batsman_1000.jpg",
            $"{base_url}batsman_2048.png",
        };

        private Queue myLogQueue = new Queue();

        // Start is called before the first frame update
        void Start()
        {
            clearLogsButton.onClick.RemoveAllListeners();
            clearLogsButton.onClick.AddListener(() => {
                myLog = "";
                myLogQueue.Clear();
            });
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(Application.Quit);

            spriteCache.Clear();

            foreach (var tex in textures)
            {
                Debug.Log($"LocalLoader: name={tex.name}, size={Utils.ToSize(tex.GetRawTextureData().Length)}, mipmap={tex.mipmapCount}, format={tex.format}, graphicsFormat={tex.graphicsFormat}");
            }

            StartCoroutine(StartTests());
        }

        IEnumerator StartTests()
        {
            yield return new WaitForSeconds(1);
            yield return new WaitForEndOfFrame();

            //var memoryStatsAtStart = SaveCurrentMemoryStats();

            //yield return CleanUpEverything();
            //yield return LoadingFromImageLoader();

            //yield return CleanUpEverything();
            //ImageLoader.settings.generateMipMaps = false;
            //yield return LoadingFromImageLoaderMemoryOptimized();
            
            //yield return CleanUpEverything();
            //ImageLoader.settings.generateMipMaps = true;
            //yield return LoadingFromImageLoaderMemoryOptimized();

            yield return CleanUpEverything();
            yield return LoadSpriteTestingCoroutine(Utils.GetImageUsingUWRTexture, nameof(Utils.GetImageUsingUWRTexture));

            yield return CleanUpEverything();
            yield return LoadSpriteTestingCoroutine(Utils.GetImageUsingUWRBufferAndEnableMipmapsAndCompression, nameof(Utils.GetImageUsingUWRBufferAndEnableMipmapsAndCompression));

            //PrintDiffOfMemoryStatsFromLastSave(memoryStatsAtStart);

            //yield return CleanUpEverything();

            //PrintDiffOfMemoryStatsFromLastSave(memoryStatsAtStart, "After cleanup");
        }

        private IEnumerator CleanUpEverything()
        {
            image.overrideSprite = null;
            GC.Collect(0, GCCollectionMode.Forced, blocking: true);
            AsyncOperation asyncOperation = Resources.UnloadUnusedAssets();
            yield return new WaitUntil(() => asyncOperation.isDone);

            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(2f);
        }

        private void OnApplicationQuit()
        {
            ImageLoader.ClearCache().AsUniTask().ToCoroutine();
        }

        public async UniTask LoadSprite(string url, Image outputImage = null)
        {
            var sprite = await ImageLoader.LoadSprite(url);
            if (outputImage != null)
                outputImage.overrideSprite = sprite;
            Assert.IsNotNull(sprite);
        }

        public async UniTask LoadSpriteMemoryOptimized(string url, Image outputImage = null)
        {
            var sprite = await ImageLoader.LoadSpriteMemoryOptimized(url);
            if (outputImage != null)
                outputImage.overrideSprite = sprite;
            Assert.IsNotNull(sprite);
        }

        public IEnumerator LoadingFromImageLoader()
        {
            yield return ImageLoader.ClearCache().AsUniTask().ToCoroutine();
            yield return new WaitForSeconds(1);

            var memoryStatsAtStart = SaveCurrentMemoryStats();

            int memSize = 0;
            
            ImageLoader.settings.useMemoryCache = true;
            ImageLoader.settings.useDiskCache = false;

            foreach (var imageURL in ImageURLs)
            {
                yield return LoadSprite(imageURL, image).ToCoroutine();
                memSize += image.overrideSprite.texture.GetRawTextureData().Length;
            }

            yield return new WaitForSeconds(0.2f);

            PrintDiffOfMemoryStatsFromLastSave(memoryStatsAtStart, "LoadingFromImageLoader");

            //yield return LoadSprite(ImageURLs[ImageURLs.Length - 2], image);

            Debug.Log($"<color=lime>LoadingFromImageLoader: MipMaps={ImageLoader.settings.generateMipMaps}, All Images Raw Data Size={Utils.ToSize(memSize)}</color>");
        }

        public IEnumerator LoadingFromImageLoaderMemoryOptimized()
        {
            //ProfilerDriver.enabled = true;
            //Profiler.enabled = true;

            yield return ImageLoader.ClearCache().AsUniTask().ToCoroutine();
            yield return new WaitForSeconds(1);

            var memoryStatsAtStart = SaveCurrentMemoryStats();

            int memSize = 0;

            ImageLoader.settings.useMemoryCache = true;
            ImageLoader.settings.useDiskCache = false;

            foreach (var imageURL in ImageURLs)
            {
                yield return LoadSpriteMemoryOptimized(imageURL, image).ToCoroutine();
                memSize += image.overrideSprite.texture.GetRawTextureData().Length;
            }

            yield return new WaitForSeconds(0.2f);

            PrintDiffOfMemoryStatsFromLastSave(memoryStatsAtStart, "LoadingFromImageLoaderMemoryOptimized");

            //Profiler.enabled = false;
            //ProfilerDriver.enabled = false;

            //yield return LoadSprite(ImageURLs[ImageURLs.Length - 2], image);
            Debug.Log($"<color=lime>LoadingFromImageLoaderMemoryOptimized: MipMaps={ImageLoader.settings.generateMipMaps}, All Images Raw Data Size={Utils.ToSize(memSize)}</color>");
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

        public IEnumerator LoadSpriteTestingCoroutine(Func<string, Image, IEnumerator> downloadHandler, string handlerName)
        {
            //ProfilerDriver.enabled = true;
            //Profiler.enabled = true;

            yield return ImageLoader.ClearCache().AsUniTask().ToCoroutine();
            yield return new WaitForSeconds(1);

            var memoryStatsAtStart = SaveCurrentMemoryStats(handlerName);

            int memSize = 0;

            yield return new WaitForSeconds(1);

            foreach (var imageURL in ImageURLs)
            {
                yield return downloadHandler.Invoke(imageURL, image);
                memSize += image.overrideSprite.texture.GetRawTextureData().Length;
            }

            // image.overrideSprite = spriteCache[ImageURLs[3]];

            PrintDiffOfMemoryStatsFromLastSave(memoryStatsAtStart, handlerName);

            //Profiler.enabled = false;
            //ProfilerDriver.enabled = false;

            //yield return LoadSprite(ImageURLs[ImageURLs.Length - 2], image);
            Debug.Log($"<color=lime>{handlerName}: All Images Raw Data Size={Utils.ToSize(memSize)}</color>");

            //Profiler.enabled = false;
            //ProfilerDriver.enabled = false;
        }

        private IEnumerator LoadSpriteTesting(string URL)
        {
            UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(URL);
            //UnityWebRequest uwr = new UnityWebRequest(URL);
            //uwr.downloadHandler = new DownloadHandlerBuffer();
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
                    Texture2D tex = ((DownloadHandlerTexture)uwr.downloadHandler).texture;
                    bool hasAlpha = GraphicsFormatUtility.HasAlphaChannel(tex.graphicsFormat);
                    Debug.Log($"LoadSpriteTesting: before name={name}, size={Utils.ToSize(tex.GetRawTextureData().Length)}, mipmap={tex.mipmapCount}, format={tex.format}, graphicsFormat={tex.graphicsFormat}, dimensions={tex.width}x{tex.height}, hasAlpha={hasAlpha}");

                    //Debug.Log($"LoadSpriteTesting: before name={name}, data size={Utils.ToSize(uwr.downloadHandler.data.Length)}");

                    //var loadedTexture = Utils.CreateTexWithMipmaps(uwr.downloadHandler.data, GraphicsFormat.R8G8B8A8_SRGB);
                    var loadedTexture = tex;
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

        #region Profiling & Logs
        ProfilerRecorder _totalReservedMemoryRecorder;
        ProfilerRecorder _gcReservedMemoryRecorder;
        ProfilerRecorder _textureMemoryRecorder;
        ProfilerRecorder _meshMemoryRecorder;

        struct MemoryStats
        {
            public long totalMemory;
            public long gcMemory;
            public long textureMemory;
            public long meshMemory;
        }

        void OnEnable()
        {
            _totalReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            _gcReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
            _textureMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Memory");
            _meshMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Mesh Memory");
#if !UNITY_EDITOR
        Application.logMessageReceived += HandleLog;
#endif
        }
        void OnDisable()
        {
            _totalReservedMemoryRecorder.Dispose();
            _gcReservedMemoryRecorder.Dispose();
            _textureMemoryRecorder.Dispose();
            _meshMemoryRecorder.Dispose();
#if !UNITY_EDITOR
        Application.logMessageReceived -= HandleLog;
#endif
        }

        private MemoryStats SaveCurrentMemoryStats(string msg = "")
        {
            Debug.Log($"<color=yellow>{msg}: MemoryStats - Tracking Start</color>");

            MemoryStats memoryStats = new MemoryStats();

            memoryStats.totalMemory = _totalReservedMemoryRecorder.LastValue;
            memoryStats.gcMemory = _gcReservedMemoryRecorder.LastValue;
            memoryStats.textureMemory = _textureMemoryRecorder.LastValue;
            memoryStats.meshMemory = _meshMemoryRecorder.LastValue;

            return memoryStats;
        }

        private void PrintDiffOfMemoryStatsFromLastSave(MemoryStats prevMemoryStats, string msg = "")
        {
            Debug.Log($"<color=yellow>{msg}: MemoryStats - Tracking End - Diff from Start: Total={Utils.ToSize(_totalReservedMemoryRecorder.LastValue - prevMemoryStats.totalMemory)}, GC={Utils.ToSize(_gcReservedMemoryRecorder.LastValue - prevMemoryStats.gcMemory)}, Texture={Utils.ToSize(_textureMemoryRecorder.LastValue - prevMemoryStats.textureMemory)}, Mesh={Utils.ToSize(_meshMemoryRecorder.LastValue - prevMemoryStats.meshMemory)}</color>");
        }

        private string myLog;
        private Vector2 scrollPosition;
        private int LogHeight = 150;
        private int LogWidth = 0;

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            myLog = logString;
            string newString = "\n [" + DateTime.Now.ToString("T") + "] [" + type + "] : " + myLog;
            myLogQueue.Enqueue(newString);
            if (type == LogType.Exception)
            {
                newString = "\n" + stackTrace;
                myLogQueue.Enqueue(newString);
            }
            myLog = string.Empty;
            List<string> list = new List<string>();
            foreach (string mylog in myLogQueue)
            {
                list.Add(mylog);
                //myLog += mylog;
            }
            list.Reverse();
            foreach (string str in list)
            {
                myLog += str;
            }

            scrollPosition = new Vector2(scrollPosition.x, 0);

            //myLog += "\n" + scrollPosition.ToString() + "\n";
        }

        private void OnGUI()
        {

            //GUILayout.BeginArea(new Rect(Screen.width - 400, 0, 400, Screen.height));
            GUILayout.BeginArea(new Rect(10, Screen.height / 2 + 50, Screen.width - LogWidth, LogHeight * 2));
            // GUILayout.BeginArea(new Rect(10, LogHeight, Screen.width - LogWidth, Screen.height - 10));
            //GUILayout.Label(myLog);
            //GUILayout.EndArea();

            //// we want to place the TextArea in a particular location - use BeginArea and provide Rect
            //GUILayout.BeginArea(new Rect(10, Screen.height - LogHeight, Screen.width - 100, Screen.height - 10));
            // scrollPosition = new Vector2(0, LogHeight * 5);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(Screen.width - 40), GUILayout.Height(LogHeight * 3));
            //// We just add a single label to go inside the scroll view. Note how the
            //// scrollbars will work correctly with wordwrap.

            //GUIStyle textStyle = new GUIStyle(GUI.skin.textArea);
            //textStyle.fontSize = FontLogSize;
            // GUILayout.TextArea(myLog, textStyle);
            GUILayout.Label(myLog);

            // End the scrollview we began above.
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            //if (GUI.Button(new Rect(10, 10, 150, 100), "Destroy Sprite"))
            //{
            //    Debug.Log("Destroy Sprite");
            //    Destroy(image.sprite.texture);
            //    Destroy(image.sprite);
            //    // image.sprite = null;
            //}
        }
        #endregion
    }
}