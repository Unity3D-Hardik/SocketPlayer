using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

namespace com.rambla.vr {

public class VideoController : MonoBehaviour
{
        VideoPlayer videoPlayer;

        string cunnretPath = "none";


        [SerializeField]
        RenderTexture movieTexture;

        MeshRenderer renderer;

        private void Awake()
        {
            videoPlayer = GetComponent<VideoPlayer>();
            renderer = GetComponent<MeshRenderer>();

        }

        private void OnEnable()
        {
            NetworkStreamHelper.OnDataUpdate += OnDataUpdated;
            videoPlayer.loopPointReached += OnVideoEnd;
            renderer.enabled = false;
        }


   

        private void OnDisable()
        {
            NetworkStreamHelper.OnDataUpdate -= OnDataUpdated;
            videoPlayer.loopPointReached -= OnVideoEnd;
        }

        private void OnVideoEnd(VideoPlayer source)
        {
            videoPlayer.targetTexture.Release();
        }

        private void OnDataUpdated(JsonResponse response)
        {

            UnityMainThreadDispatcher.Dispatcher.Enqueue(() =>
            {
                StartCoroutine("PlayPauseVideo", response);
            });

        }

        IEnumerator PlayPauseVideo(JsonResponse response)
        {

            if (!cunnretPath.Equals(response.Path))
            {
                if (string.IsNullOrEmpty(response.Path) || string.IsNullOrWhiteSpace(response.Path))
                {
                    
                }
                else {
                    Debug.Log($"<color=green>Video path ------> {response.Path}</color>");

                    if (File.Exists(response.Path))
                    {
                        Debug.Log($"<color=green>File Found At ------> {response.Path}</color>");

                        cunnretPath = response.Path;
                        videoPlayer.source = VideoSource.Url;
                        videoPlayer.url = response.Path;

                        videoPlayer.Prepare();
                        videoPlayer.prepareCompleted += PlayVideo;
                        videoPlayer.errorReceived += OnErrorReceived;
                    }
                    else
                    {
                        // File does not exist
                        Debug.LogError("===============> File not found.");
                    }
                }
            }

            if (videoPlayer.isPlaying)
            {
                

                if (response.PlayerState.Equals("1"))
                {
                    Debug.Log($"<color=green> Stop current video</color>");

                    videoPlayer.Stop();
                    videoPlayer.targetTexture.Release();
                    renderer.enabled = false;
                    cunnretPath = "";

                    videoPlayer.prepareCompleted -= PlayVideo;

                }

                double doubleValue = 0;
                bool parsingSuccessful = double.TryParse(response.CurrentTime, out doubleValue);

                if (parsingSuccessful)
                {
                    Debug.Log($"<color=green> Video Seek To {response.CurrentTime}</color>");
                    SeekVideoPlayer(doubleValue);
                }
            }
           

            yield return null;
        }

        private void OnErrorReceived(VideoPlayer source, string message)
        {
           Debug.LogError("File path Issue :- "+message);
        }

        private void PlayVideo(VideoPlayer source)
        {
            videoPlayer.Play();
            videoPlayer.targetTexture = movieTexture;
            renderer.enabled = true;
        }




        /// <summary>
        /// fornt
        /// </summary>
        public void SeekVideoPlayer(double videoTime)
        {
            videoPlayer.time = videoTime;
        }

        IEnumerator LoadVideoPlayer(string path)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get("file:///"+path))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(": HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                       
                        break;
                }
            }
        }

    }
}