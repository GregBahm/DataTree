using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AvatarTestLoader : MonoBehaviour
{
    public Material OutputMaterial;
    public Material BlitMaterial;
    public RenderTexture Output;
    public RenderTexture OutputHolder;

    void Start()
    {
        DataProcessor processor = DataProcessor.GetTestProcessor();
        string[] avatars = Directory.GetFiles(processor.AvatarFolder);

        int neededResolution = Mathf.CeilToInt(Mathf.Sqrt(avatars.Length) * 16);
        int imageResolution = Mathf.NextPowerOfTwo(neededResolution);
        Output = new RenderTexture(imageResolution, imageResolution, 0)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            enableRandomWrite = true
        };
        Output.Create();

        OutputHolder = new RenderTexture(imageResolution, imageResolution, 0)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            enableRandomWrite = true
        };
        OutputHolder.Create();

        Texture2D avatarTexture = new Texture2D(16, 16);
        byte[] pngData;
        for (int i = 0; i < neededResolution; i++)
        {
            for (int j = 0; j < neededResolution; j++)
            {
                int xOffset = i * 16;
                int yOffset = j * 16;
                int avatarIndex = i * neededResolution + j;
                if(avatarIndex > avatars.Length - 1)
                {
                    break;
                }
                pngData = File.ReadAllBytes(avatars[avatarIndex]);
                avatarTexture.LoadImage(pngData);

                float xOffsetForShader = (float)xOffset / neededResolution;
                float yOffsetForShader = (float)yOffset / neededResolution;
                BlitMaterial.SetFloat("_XOffset", xOffsetForShader);
                BlitMaterial.SetFloat("_YOffset", yOffsetForShader);
                BlitMaterial.SetTexture("_OutputTex", OutputHolder);
                Graphics.Blit(avatarTexture, Output, BlitMaterial, 0);
                Graphics.Blit(Output, OutputHolder);
            }
        }

        OutputMaterial.SetTexture("_MainTex", Output);
    }
}
