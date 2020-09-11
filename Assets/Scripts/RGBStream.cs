using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_WSA && !UNITY_EDITOR
using System;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
#endif
public class RGBStream : MonoBehaviour
{
    private Texture2D tex = null;
    private byte[] bytes = null;
    private float time = 0f;
    private int count = 300;

    [SerializeField]
    private RawImage image;
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_WSA && !UNITY_EDITOR
        Task.Run(() => { InitSensor(); });
#endif
    }
    private void Update()
    {
#if UNITY_WSA && !UNITY_EDITOR
        return;
        if(time > 1 / 20)
        {
            time = 0f;
            image.texture = null;
            image.texture = tex;
        }
        else
        {
            time += Time.deltaTime;
        }
#endif
    }
#if UNITY_WSA && !UNITY_EDITOR
    private async void InitSensor()
    {
        var mediaFrameSourceGroupList = await MediaFrameSourceGroup.FindAllAsync();
        var mediaFrameSourceGroup = mediaFrameSourceGroupList[0];
        var mediaFrameSourceInfo = mediaFrameSourceGroup.SourceInfos[0];
        MediaFrameSourceKind kind = mediaFrameSourceInfo.SourceKind;
        var mediaCapture = new MediaCapture();
        var settings = new MediaCaptureInitializationSettings()
        {
            SourceGroup = mediaFrameSourceGroup,
            SharingMode = MediaCaptureSharingMode.SharedReadOnly,
            StreamingCaptureMode = StreamingCaptureMode.Video,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
        };
        try
        {
            await mediaCapture.InitializeAsync(settings);
            var mediaFrameSource = mediaCapture.FrameSources[mediaFrameSourceInfo.Id];
            MediaFrameReader mediaframereader;
            if (kind == MediaFrameSourceKind.Color)
            {
                mediaframereader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSource, MediaEncodingSubtypes.Argb32);
            }
            else
            {
                mediaframereader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSource, mediaFrameSource.CurrentFormat.Subtype);
            }
            //var mediaframereader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSource, mediaFrameSource.CurrentFormat.Subtype);
            mediaframereader.FrameArrived += FrameArrived;
            await mediaframereader.StartAsync();
        }
        catch (Exception e)
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() => { Debug.Log(e); }, true);
        }
    }

    private void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        var mediaframereference = sender.TryAcquireLatestFrame();
        if (mediaframereference != null)
        {
            var videomediaframe = mediaframereference?.VideoMediaFrame;
            var softwarebitmap = videomediaframe?.SoftwareBitmap;
            if (softwarebitmap != null)
            {
                softwarebitmap = SoftwareBitmap.Convert(softwarebitmap, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
                int w = softwarebitmap.PixelWidth;
                //Debug.Log("w: " + w);
                int h = softwarebitmap.PixelHeight;
                //Debug.Log("h: " + h);
                if (bytes==null)
                {
                    bytes = new byte[w * h * 4];
                }
                softwarebitmap.CopyToBuffer(bytes.AsBuffer());
                softwarebitmap.Dispose();
                UnityEngine.WSA.Application.InvokeOnAppThread(() => {
                    if (tex == null)
                    {
                        tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                        GetComponent<Renderer>().material.mainTexture = tex;
                    }
                    tex.LoadRawTextureData(bytes);
                    tex.Apply();
                    image.texture = null;
                    image.texture = tex;
                }, true);
            }
            mediaframereference.Dispose();
        }

    }
#endif
}
