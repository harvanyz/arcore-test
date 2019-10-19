using System;
using UnityEngine;
using GoogleARCore;
using GoogleARCore.Examples.ComputerVision;


public class TextureReaderWrapper : MonoBehaviour {
    /// <summary>
    /// 取得するTextureのサイズの、カメラ画像に対する割合
    /// </summary>
    public float TextureSizeRatio = 1.0f;

    /// <summary>
    /// カメラ画像のデータ群
    /// </summary>
    private TextureReaderApi.ImageFormatType format;
    private int width;
    private int height;
    private IntPtr pixelBuffer;
    private int bufferSize = 0;

    /// <summary>
    /// カメラ画像取得用API
    /// </summary>
    private TextureReader TextureReader = null;


    /// <summary>
    /// カメラ画像のサイズに合わせてTextureReaderをセットしたかどうかのフラグ
    /// </summary>
    private bool setFrameSizeToTextureReader = false;
    public void Awake()
    {
        // // カメラ画像取得時に呼ばれるコールバック関数を定義
        // TextureReader = GetComponent<TextureReader>();
        // TextureReader.OnImageAvailableCallback += OnImageAvailableCallbackFunc;
        // _ShowAndroidToastMessage("KO KO DA!");
    }

    private void OnImageAvailableCallbackFunc(TextureReaderApi.ImageFormatType format, int width, int height, IntPtr pixelBuffer, int bufferSize)
    {
        this.format = format;
        this.width = width;
        this.height = height;
        this.pixelBuffer = pixelBuffer;
        this.bufferSize = bufferSize;
        //_ShowAndroidToastMessage(bufferSize.ToString());
        //_ShowAndroidToastMessage(pixelBuffer.ToString());
    }


    // Use this for initialization
    void Start()
    {
        // カメラ画像取得時に呼ばれるコールバック関数を定義
        TextureReader = GetComponent<TextureReader>();
        TextureReader.OnImageAvailableCallback += OnImageAvailableCallbackFunc;
        // _ShowAndroidToastMessage("KO KO DA!");
    }

    // Update is called once per frame
    void Update()
    {
        // TextureReaderにカメラ画像のサイズをセットする。実行は一回だけ
        if (!setFrameSizeToTextureReader)
        {
            using (var image = Frame.CameraImage.AcquireCameraImageBytes())
            {
                if (!image.IsAvailable)
                {
                    return;
                }

                TextureReader.ImageWidth = (int)(image.Width * TextureSizeRatio);
                TextureReader.ImageHeight = (int)(image.Height * TextureSizeRatio);
                TextureReader.Apply();

                setFrameSizeToTextureReader = true;

                //_ShowAndroidToastMessage(TextureReader.ImageWidth.ToString());
            }
        }
    }

    public bool isRed(float x, float y)
    {
        // TextureReaderが取得した画像データのポインタからデータを取得
        byte[] data = new byte[bufferSize];

        //_ShowAndroidToastMessage(bufferSize.ToString());
        _ShowAndroidToastMessage(pixelBuffer.ToString());
        
        // _ShowAndroidToastMessage(width.ToString());


        System.Runtime.InteropServices.Marshal.Copy(pixelBuffer, data, 0, bufferSize);
        //_ShowAndroidToastMessage(pixelBuffer.ToString());
        // 向きが270回転と反転しているので補正する
        byte[] correctedData = Rotate90AndFlip(data, width, height, format == TextureReaderApi.ImageFormatType.ImageFormatGrayscale);
        
        int idx = ((int)y * width + (int)x) * 4;

        int v = correctedData[idx];
        _ShowAndroidToastMessage(v.ToString());
        if (v > 150){
            return true;
        }

        return false;

        //_ShowAndroidToastMessage("Test dayo");


        // if (bufferSize == 0){
        //     return false;
        // }
        // else{
        //     return true;
        // }


        //return false;
    }


    public Texture2D FrameTexture
    {
        get
        {
            if (bufferSize != 0)
            {
                // TextureReaderが取得した画像データのポインタからデータを取得
                byte[] data = new byte[bufferSize];
                System.Runtime.InteropServices.Marshal.Copy(pixelBuffer, data, 0, bufferSize);
                // 向きが270回転と反転しているので補正する
                byte[] correctedData = Rotate90AndFlip(data, width, height, format == TextureReaderApi.ImageFormatType.ImageFormatGrayscale);
                // Texture2Dを作成 90度回転させているのでwidth/heightを入れ替える
                Texture2D _tex = new Texture2D(height, width, TextureFormat.RGBA32, false, false);
                _tex.LoadRawTextureData(correctedData);
                _tex.Apply();

                return _tex;
            }
            else
            {
                return null;
            }
        }
    }


    private byte[] Rotate90AndFlip(byte[] img, int width, int height, bool isGrayscale)
    {
        int srcChannels = isGrayscale ? 1 : 4;
        int dstChannels = 4; //出力は常にRGBA32にする
        byte[] newImg = new byte[width * height * dstChannels];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                //imgのindex
                int p = (i * width + j) * srcChannels;

                //newImgに対するindex. 90度回転と反転を入れている
                int np = ((width - j - 1) * height + (height - i - 1)) * dstChannels;

                // グレースケールでもRGBで扱えるようにしておく
                if (isGrayscale)
                {
                    newImg[np] = img[p]; // R
                    newImg[np + 1] = img[p]; // G
                    newImg[np + 2] = img[p]; // B
                    newImg[np + 3] = 255; // A
                }
                else
                {
                    for (int c = 0; c < dstChannels; c++)
                    {
                        newImg[np + c] = img[p + c];
                    }
                }
            }
        }

        return newImg;
    }

    private void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject =
                        toastClass.CallStatic<AndroidJavaObject>(
                            "makeText", unityActivity, message, 0);
                    toastObject.Call("show");
                }));
            }
        }
}