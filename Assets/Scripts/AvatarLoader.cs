using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

class AvatarLoader
{
    private readonly Material _blitMaterial;
    private readonly RenderTexture _output;
    private readonly RenderTexture _outputHolder;
    public Texture AtlasTexture { get { return _output; } }

    private readonly Dictionary<string, Vector2> _avatarsCoords;
    public Vector2 GetCoordsFor(Node node)
    {
        if(!_avatarsCoords.ContainsKey(node.SubUrl))
        {
            return Vector2.zero;
        }
        return _avatarsCoords[node.SubUrl];
    }

    public AvatarLoader(DataProcessor processor, Material blitMaterial)
    {
        _blitMaterial = blitMaterial;

        string[] avatars = Directory.GetFiles(processor.AvatarFolder);

        int neededResolution = Mathf.CeilToInt(Mathf.Sqrt(avatars.Length) * 16);
        int imageResolution = Mathf.NextPowerOfTwo(neededResolution);

        _output = CreateRenderTexture(imageResolution);
        _outputHolder = CreateRenderTexture(imageResolution);

        int avatarResolution = neededResolution / 16;
        Texture2D avatarTexture = new Texture2D(16, 16);

        _avatarsCoords = new Dictionary<string, Vector2>();

        byte[] pngData;
        for (int i = 0; i < avatarResolution; i++)
        {
            for (int j = 0; j < avatarResolution; j++)
            {
                int avatarIndex = i * avatarResolution + j;
                if (avatarIndex > avatars.Length - 1)
                {
                    break;
                }
                pngData = File.ReadAllBytes(avatars[avatarIndex]);
                avatarTexture.LoadImage(pngData);

                float xOffsetForShader = (float)i / avatarResolution;
                float yOffsetForShader = (float)j / avatarResolution;
                _blitMaterial.SetFloat("_XOffset", xOffsetForShader);
                _blitMaterial.SetFloat("_YOffset", yOffsetForShader);
                _blitMaterial.SetTexture("_OutputTex", _output);
                Graphics.Blit(avatarTexture, _outputHolder, _blitMaterial, 0);
                Graphics.Blit(_outputHolder, _output);

                string key = Path.GetFileNameWithoutExtension(avatars[avatarIndex]);
                Vector2 uvs = new Vector2(xOffsetForShader, yOffsetForShader);
                _avatarsCoords.Add(key, uvs);
            }
        }
    }

    private RenderTexture CreateRenderTexture(int imageResolution)
    {
        RenderTexture ret = new RenderTexture(imageResolution, imageResolution, 0)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            enableRandomWrite = true
        };
        ret.Create();
        return ret;
    }
}
