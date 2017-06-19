using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BlitTestScript : MonoBehaviour 
{
    public Material OutputMaterial;
    public RenderTexture Output;
    public RenderTexture OutputHolder;
    public Texture2D ContentTexture;
    public Material BlittingMaterial;

	// Use this for initialization
	void Start ()
    {
        Output = new RenderTexture(512, 512, 0);
        OutputHolder = new RenderTexture(512, 512, 0);
        ContentTexture = new Texture2D(16, 16);
        ContentTexture.wrapMode = TextureWrapMode.Clamp;
        ContentTexture.filterMode = FilterMode.Point;
        
        byte[] someContent = File.ReadAllBytes(@"D:\DataTree\SamplePostData\Avatars\0hlee.png");
        ContentTexture.LoadImage(someContent);
        
        Graphics.Blit(ContentTexture, OutputHolder, BlittingMaterial);
        Graphics.Blit(OutputHolder, Output);

        someContent = File.ReadAllBytes(@"D:\DataTree\SamplePostData\Avatars\0mniblade.png");
        ContentTexture.LoadImage(someContent);
        BlittingMaterial.SetFloat("_XOffset", .5f);
        BlittingMaterial.SetFloat("_YOffset", .5f);
        BlittingMaterial.SetTexture("_OutputTex", Output);
        Graphics.Blit(ContentTexture, OutputHolder, BlittingMaterial);

        OutputMaterial.SetTexture("_MainTex", OutputHolder);
	}
}
