using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class D2M
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Header
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] fourCC;

        public byte version;
        public byte fps;
        public byte type;   // 1 - dxt1, 5 - dxt5
        public byte reserved;

        public int width;
        public int height;

        public int frameCount;
    }

    Header header;

    [HideInInspector]
    public Texture2D texture;

    byte[] data;

    byte[] currentImage;
    int frameCount;
    int imageBufferSize;
    int dataOffset;
    int blockCount;
    int blockSize;

    float frameUpdateTimer;

    public int width {  get { if (header == null) return 0; return header.width; } }
    public int height { get { if (header == null) return 0; return header.height; } }

    T BytesToClass<T>(byte[] data, int offset = 0)
    {
        if (data == null)
            return default(T);

        if ((Marshal.SizeOf(typeof(T)) > (data.Length - offset)) || (data.Length <= offset))
        {
            return default(T);
        }

        System.IntPtr buff = Marshal.AllocHGlobal(data.Length - offset);
        Marshal.Copy(data, offset, buff, data.Length - offset);
        T obj = (T)Marshal.PtrToStructure(buff, typeof(T));
        Marshal.FreeHGlobal(buff);

        return obj;
    }

    public void Load(string filename)
    {
        data = System.IO.File.ReadAllBytes(filename);
        if (data == null || data.Length == 0)
        {
            data = null;
            return;
        }

        header = BytesToClass<Header>(data, 0);

        dataOffset = 20; // start offset, header size 20byte
        frameCount = 0;

        if (header.type == 1)
        {
            blockSize = 8;
            imageBufferSize = header.width * header.height / 2; // 1/8, dxt1
            texture = new Texture2D(header.width, header.height, TextureFormat.DXT1, false);
        }
        else
        {
            blockSize = 16;
            imageBufferSize = header.width * header.height; // 1/4, dxt5
            texture = new Texture2D(header.width, header.height, TextureFormat.DXT5, false);
        }

        blockCount = imageBufferSize / blockSize;
        currentImage = new byte[imageBufferSize];

        DecodeImage();
        frameUpdateTimer = 1.0f / (float)header.fps;

        Debug.Log(header.version + ",  " + header.fps + ",  " + header.width + "x" + header.height + ",    " + header.frameCount + ",   " + imageBufferSize + ",   " + blockCount);
    }
        
    public void Update()
    {
        frameUpdateTimer -= Time.deltaTime;
        if (frameUpdateTimer > 0)
            return;

        frameUpdateTimer += 1.0f / (float)header.fps;
        DecodeImage();
    }

    void DecodeImage()
    {
        if (data == null || currentImage == null)
            return;

        int imageOffset = 0;
        int i;
        byte blockStatus;            
        for (i = 0; i < blockCount; i++)
        {
            blockStatus = data[dataOffset++];
            switch (blockStatus)
            {
                case 0: // same
                    imageOffset += blockSize;                        
                    break;

                case 0x01:  // upper same
                    System.Buffer.BlockCopy(data, dataOffset, currentImage, imageOffset + (blockSize / 2), blockSize/2);
                    imageOffset += blockSize;
                    dataOffset += blockSize/2;
                    break;

                case 0x02:  // lower same
                    System.Buffer.BlockCopy(data, dataOffset, currentImage, imageOffset, blockSize/2);
                    imageOffset += blockSize;
                    dataOffset += blockSize / 2;
                    break;

                case 0xff:  // all diff
                    System.Buffer.BlockCopy(data, dataOffset, currentImage, imageOffset, blockSize);
                    imageOffset += blockSize;
                    dataOffset += blockSize;
                    break;
            }
        }

        texture.LoadRawTextureData(currentImage);
        texture.Apply();

        frameCount ++;
        if(frameCount>=header.frameCount)
        {
            frameCount = 0;
            dataOffset = 20;
        }

        //Debug.Log("framecount = " + frameCount);
    }
}
