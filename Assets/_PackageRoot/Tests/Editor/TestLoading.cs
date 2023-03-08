using System;
using UnityEngine;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using UnityEngine.TestTools;
using System.Collections;

namespace Extensions.Unity.ImageLoader.Tests
{
    public class TestLoading
    {
        static readonly string[] ImageURLs =
        {
            "https://github.com/IvanMurzak/Unity-ImageLoader/raw/master/Test%20Images/ImageA.jpg",
            "https://github.com/IvanMurzak/Unity-ImageLoader/raw/master/Test%20Images/ImageB.png",
            "https://github.com/IvanMurzak/Unity-ImageLoader/raw/master/Test%20Images/ImageC.png"
        };

        public async UniTask LoadSprite(string url)
        {
            var sprite = await ImageLoader.LoadSprite(url);
            Assert.AreNotEqual(sprite, null);
        }

        [UnityTest] public IEnumerator LoadSpritesCacheMemoryDisk()
        {
            ImageLoader.ClearCache();

            foreach (var imageURL in ImageURLs) 
                yield return LoadSprite(imageURL).ToCoroutine();
        }
        [UnityTest] public IEnumerator LoadSpritesCacheMemory()
        {
            ImageLoader.ClearCache();

            foreach (var imageURL in ImageURLs) 
                yield return LoadSprite(imageURL).ToCoroutine();
        }
        [UnityTest] public IEnumerator LoadSpritesCacheDisk()
        {
            ImageLoader.ClearCache();

            foreach (var imageURL in ImageURLs) 
                yield return LoadSprite(imageURL).ToCoroutine();
        }
        [UnityTest] public IEnumerator LoadSpritesNoCache()
        {
            ImageLoader.ClearCache();

            foreach (var imageURL in ImageURLs) 
                yield return LoadSprite(imageURL).ToCoroutine();
        }
    }
}