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

        int avatarResolution = neededResolution / 16;
        Texture2D avatarTexture = new Texture2D(16, 16);
        byte[] pngData;
        for (int i = 0; i < avatarResolution; i++)
        {
            for (int j = 0; j < avatarResolution; j++)
            {
                int avatarIndex = i * avatarResolution + j;
                if(avatarIndex > avatars.Length - 1)
                {
                    Debug.Log("Skipping " + avatarIndex);
                    break;
                }
                pngData = File.ReadAllBytes(avatars[avatarIndex]);
                avatarTexture.LoadImage(pngData);

                float xOffsetForShader = (float)i / avatarResolution;
                float yOffsetForShader = (float)j / avatarResolution;
                BlitMaterial.SetFloat("_XOffset", xOffsetForShader);
                BlitMaterial.SetFloat("_YOffset", yOffsetForShader);
                BlitMaterial.SetTexture("_OutputTex", Output);
                Graphics.Blit(avatarTexture, OutputHolder, BlitMaterial, 0);
                Graphics.Blit(OutputHolder, Output);
            }
        }

        OutputMaterial.SetTexture("_MainTex", OutputHolder);
    }
}
