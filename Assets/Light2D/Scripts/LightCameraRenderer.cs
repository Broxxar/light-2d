using UnityEngine;
using System.Collections;

[ExecuteAlways]
public class LightCameraRenderer : MonoBehaviour
{
	public enum CameraBlendMode
	{
		Multiply,
		Multiply2x,
		AlphaBlend,
		Additive
	}

	[Range(0, 10)]
	public int BlurIterations = 3;
	[Range(0, 4)]
	public int DownResFactor = 2;
	public CameraBlendMode BlendMode;

	Material _blendMat;
	Material _blurMat;
	Camera _camera;
	RenderTexture _rt;

	void OnEnable()
	{
		if (_blendMat == null)
		{
			_blendMat = new Material(Shader.Find("Hidden/LightCameraBlend"));
		}

		if (_blurMat == null)
		{
			_blurMat = new Material(Shader.Find("Hidden/KawaseBlur"));
		}

		_camera = GetComponent<Camera>();
		GenerateRenderTexture();
	}

	void OnDisable()
	{
		_camera.targetTexture = null;
		DestroyImmediate(_rt);
	}

	void GenerateRenderTexture()
	{
		if (_rt != null)
		{
			_camera.targetTexture = null;
			DestroyImmediate(_rt);
		}

		_rt = new RenderTexture(Screen.width, Screen.height, 0)
		{
			antiAliasing = 1,
			filterMode = FilterMode.Bilinear,
			format = RenderTextureFormat.ARGBHalf,
			name = "Light2DCapture"
		};
	}

	#region Editor Update
#if UNITY_EDITOR
	void Update()
	{
		// Constantly refreshes the render texture in editor, in case you're resizing or something, keeps things looking nice.
		if (!UnityEditor.EditorApplication.isPlaying && (Screen.width != _rt.width || Screen.height != _rt.height))
		{
			GenerateRenderTexture();
		}
	}
#endif
	#endregion

	void OnPreRender()
	{
		_camera.allowMSAA = false;
		_camera.allowHDR = true;
		_camera.targetTexture = _rt;
	}

	void OnPostRender()
	{
		_camera.targetTexture = null;
		if (BlurIterations != 0 || DownResFactor != 0)
		{
			ApplyBlur();
		}	
		Graphics.Blit(_rt, null as RenderTexture, _blendMat, (int)BlendMode);
	}

	void ApplyBlur()
	{
		RenderTexture temp0 = RenderTexture.GetTemporary(_rt.width >> DownResFactor, _rt.height >> DownResFactor);
		Graphics.Blit(_rt, temp0);

		for (int i = 0; i < BlurIterations; i++)
		{
			RenderTexture temp1 = RenderTexture.GetTemporary(temp0.width, temp0.height);
			_blurMat.SetFloat("_Steps", i);
			Graphics.Blit(temp0, temp1, _blurMat);
			RenderTexture.ReleaseTemporary(temp0);
			temp0 = temp1;
		}

		Graphics.Blit(temp0, _rt);
		RenderTexture.ReleaseTemporary(temp0);
	}
}
