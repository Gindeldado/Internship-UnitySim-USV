using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
//using Unity.Robotics.ROSTCPConnector;
//using RosMessageTypes.Sensor;
//using RosMessageTypes.Std;
//using RosMessageTypes.BuiltinInterfaces;
//using Unity.Robotics.Core;

public sealed class RosCamera : MonoBehaviour
{
    enum CameraMode { RGB, depth}
    [SerializeField] private CameraMode cameraMode = CameraMode.RGB;
    [SerializeField] private int cameraFarPlane;
    [SerializeField] private bool sendCompressed = false;
    [SerializeField] private float imagesPerSecond = 2;

    [Space(10)]
    public int resWidth = 1280;
    public int resHeight = 720;

    [Space(10)]
    //[SerializeField] private string topic = "Imagestream";
    //[SerializeField] private string FrameId = "Stand";

    [Space(10)]
    public Material flipMaterial;
    
    private Camera cam;

    private RenderTexture grab;
    private RenderTexture flip;

    private NativeArray<byte> _buffer;
    private bool renderIsDone = true;
    private byte[] imagePixelBytes;

    //private ROSConnection i_Ros;

    //by calling it as an IEnumerator we can return to this function using yield return
    System.Collections.IEnumerator Start()
    {
        cam = GetComponent<Camera>();
        cam.farClipPlane = cameraFarPlane;
        //i_Ros = ROSConnection.GetOrCreateInstance();
        //make the right type of publisher depending on if we send compressed
        if (sendCompressed)
        {
            //i_Ros.RegisterPublisher<CompressedImageMsg>(topic);
        }
        else
        {
            //i_Ros.RegisterPublisher<ImageMsg>(topic);
        }

        //init all rendertextures 
        switch (cameraMode)
        {
            case CameraMode.RGB:
                grab = new RenderTexture(resWidth, resHeight, 16, RenderTextureFormat.ARGB32);
                flip = new RenderTexture(resWidth, resHeight, 16, RenderTextureFormat.ARGB32);

                _buffer = new NativeArray<byte>(resWidth * resHeight * 4, Allocator.Persistent,
                                                NativeArrayOptions.UninitializedMemory);
                imagePixelBytes = new byte[resWidth * resHeight * 4];
                cam.targetTexture = grab;
                break;
            case CameraMode.depth:
                grab = new RenderTexture(resWidth, resHeight, 0, RenderTextureFormat.R16);
                flip = new RenderTexture(resWidth, resHeight, 0, RenderTextureFormat.R16);

                _buffer = new NativeArray<byte>(resWidth * resHeight*2, Allocator.Persistent,
                                                NativeArrayOptions.UninitializedMemory);
                imagePixelBytes = new byte[resWidth * resHeight*2];
                cam.targetTexture = grab;
                break;
            default:
                Debug.LogError("Non valid camera mode");
                break;
        }




        //initial wait to make sure everything is initialized
        yield return new WaitForSeconds(0.5f);
        while (true)
        {

            //stop starting a new render if the previous isn't done, can cause overflow errors in _buffer otherwise
            if (renderIsDone)
            {
                renderIsDone = false;
                float renderstartTime = Time.realtimeSinceStartup;
                cam.Render();
                //flip the rendered image, unity sees the bottom left corner of the image as 0,0 while rviz sees the top left corner as 0,0 
                Graphics.Blit(grab, flip, flipMaterial);
 
                AsyncGPUReadback.RequestIntoNativeArray(ref _buffer, flip, 0, OnCompleteReadback);
                //try to match or desired images per second, catch it if time waited is 0 seconds or lower
                if ((1 / imagesPerSecond) - (Time.realtimeSinceStartup - renderstartTime) > 0)
                {
                    yield return new WaitForSeconds((1 / imagesPerSecond) - (Time.realtimeSinceStartup - renderstartTime));
                }
            }
            else
            {
                yield return new WaitForSeconds(0.001f);//arbitrary low number, prevents this from being continuesly called if the render is not done yet.
            }
            

        }
    }

    //when working with native arrays you need to dispose them to prevent memory loss
    void OnDestroy()
    {
        AsyncGPUReadback.WaitAllRequests();
        _buffer.Dispose();
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        renderIsDone = true;
        if (request.hasError)
        {
            Debug.Log("GPU readback error detected.");
            return;
        }
        if (request.done) {
            NativeArray<byte> encoded = request.GetData<byte>(0);
            //if compressed encode the data into a jpg and then send a compressed image msg
            if (sendCompressed)
            {
                using var encodedJPG = ImageConversion.EncodeNativeArrayToJPG(encoded, grab.graphicsFormat, (uint)grab.width, (uint)grab.height);
                //compressed data size is not static so we need to make a new one everytime
                byte[] CompressedPixelBytes = new byte[encodedJPG.Length];
                encodedJPG.CopyTo(CompressedPixelBytes);
                //SendCompressedImageMessage(CompressedPixelBytes);
            }
            else
            {
                //copy our raw data to our initiliazed byte[]
                encoded.CopyTo(imagePixelBytes);
                switch (cameraMode)
                {
                    case CameraMode.RGB:
                        //SendImageMessage(imagePixelBytes);
                        break;

                    case CameraMode.depth:
                        //SendDepthImageMessage(imagePixelBytes);
                        break;

                    default:
                        Debug.LogError("Non valid camera mode");
                        break;
                }
            }
            //Prevent memory leak by disposing native arrays when we are done with them
            encoded.Dispose();
        }

    }

    //all ros sensor message formats can be found here http://docs.ros.org/en/noetic/api/sensor_msgs/html/index-msg.html
    /*
    void SendImageMessage(byte[] imageData)
    {
        var timestamp = new TimeStamp(Clock.time);
        var msg = new ImageMsg
        {
            header = new HeaderMsg
            {
                frame_id = FrameId,
                stamp = new TimeMsg
                {
                    sec = timestamp.Seconds,
                    nanosec = timestamp.NanoSeconds,
                }

            },
            height = (uint)resHeight,
            width = (uint)resWidth,
            is_bigendian = 0,
            //encoding has to be one of http://docs.ros.org/en/jade/api/sensor_msgs/html/image__encodings_8h_source.html
            encoding = "rgba8",
            step = 4 * 8 * (uint)resWidth,
            data = imageData


        };
        i_Ros.Publish(topic, msg);

    }

    //send a depth image
    void SendDepthImageMessage(byte[] imageData)
    {
        var timestamp = new TimeStamp(Clock.time);
        var msg = new ImageMsg
        {
            header = new HeaderMsg
            {
                frame_id = FrameId,
                stamp = new TimeMsg
                {
                    sec = timestamp.Seconds,
                    nanosec = timestamp.NanoSeconds,
                }

            },
            height = (uint)resHeight,
            width = (uint)resWidth,
            is_bigendian = 0,
            //encoding has to be one of http://docs.ros.org/en/jade/api/sensor_msgs/html/image__encodings_8h_source.html
            encoding = "mono16",
            step =2 * 8 * (uint)resWidth,
            data = imageData


        };
        i_Ros.Publish(topic, msg);

    }
    //send a compressed image message
    void SendCompressedImageMessage(byte[] imageData)
    {
        var timestamp = new TimeStamp(Clock.time);
        var msg = new CompressedImageMsg
        {
            header = new HeaderMsg
            {
                frame_id = FrameId,
                stamp = new TimeMsg
                {
                    sec = timestamp.Seconds,
                    nanosec = timestamp.NanoSeconds,
                }

            },
            format = "jpeg",
            data = imageData


        };
        i_Ros.Publish(topic, msg);
    }
    */
}