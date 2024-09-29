using UnityEngine;

// 委托突出显示事件
public delegate void HighlightingEventHandler(bool state, bool zWrite);

[RequireComponent(typeof(Camera))]
public class HighlightingEffect : MonoBehaviour
{
    // 事件，用于在向突出显示缓冲区呈现之前通知highlightable对象更改其材质
    public static event HighlightingEventHandler highlightingEvent;

    #region Inspector Fields
    // 模板(高亮显示)缓冲深度   0  默认  1  强大  2  速度  3  质量
    [Header("高亮显示缓冲深度")]
    public int stencilZBufferDepth = 0;

    // 模板(高亮显示)缓冲大小降低采样因子
    [Header("高亮显示缓冲大小降低采样因子")]
    public int _downsampleFactor = 4;

    // 模糊迭代
    [Header("模糊迭代")]
    public int iterations = 2;

    // 模糊最小传播
    [Header("模糊最小传播")]
    [Range(0.0f,3.0f)]
    public float blurMinSpread = 0.65f;

    // 每次迭代的模糊扩展
    [Header("每次迭代的模糊扩展")]
    [Range(0.0f, 3.0f)]
    public float blurSpread = 0.25f;

    // 模糊材质的模糊强度
    [Header("模糊材质的模糊强度")]
    [Range(0.0f, 1.0f)]
    public float _blurIntensity = 0.3f;

    // 这些属性只在编辑器中可用——我们不需要在独立构建中使用它们
#if UNITY_EDITOR
    // z缓冲写状态getter/setter
    public bool stencilZBufferEnabled
	{
		get
		{
			return (stencilZBufferDepth > 0);
		}
		set
		{
			if (stencilZBufferEnabled != value)
			{
				stencilZBufferDepth = value ? 16 : 0;
			}
		}
	}

    // 将采样因子getter / setter
    public int downsampleFactor
	{
		get
		{
			if (_downsampleFactor == 1)
			{
				return 0;
			}
			if (_downsampleFactor == 2)
			{
				return 1;
			}
			return 2;
		}
		set
		{
			if (value == 0)
			{
				_downsampleFactor = 1;
			}
			if (value == 1)
			{
				_downsampleFactor = 2;
			}
			if (value == 2)
			{
				_downsampleFactor = 4;
			}
		}
	}

    // 模糊alpha强度的获取/设置
    public float blurIntensity
	{
		get
		{
			return _blurIntensity;
		}
		set
		{
			if (_blurIntensity != value)
			{
				_blurIntensity = value;
				if (Application.isPlaying)
				{
					blurMaterial.SetFloat("_Intensity", _blurIntensity);
				}
			}
		}
	}
#endif
    #endregion

    #region Private Fields
    // 突出显示相机图层，剔除蒙版
    private int layerMask = (1 << HighlightableObject.highlightingLayer);

    // 这个GameObject引用
    private GameObject go = null;

    // 渲染模板缓冲游戏对象的摄像头
    private GameObject shaderCameraGO = null;

    // 渲染模板缓冲的摄像头
    private Camera shaderCamera = null;

    // 渲染纹理与模具缓冲
    private RenderTexture stencilBuffer = null;

    // 相机参考
    private Camera refCam = null;

    // 模糊材质
    private static Shader _blurShader;
	private static Shader blurShader
	{
		get
		{
			if (_blurShader == null)
			{
				_blurShader = Shader.Find("Hidden/Highlighted/Blur");
			}
			return _blurShader;
		}
	}

    // 合成材质
    private static Shader _compShader;
	private static Shader compShader 
	{
		get
		{
			if (_compShader == null)
			{
				_compShader = Shader.Find("Hidden/Highlighted/Composite");
			}
			return _compShader;
		}
	}

    // 模糊材质
    private static Material _blurMaterial = null;
	private static Material blurMaterial
	{
		get
		{
			if (_blurMaterial == null)
			{
				_blurMaterial = new Material(blurShader);
				_blurMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return _blurMaterial;
		}
	}

    // 合成材质
    private static Material _compMaterial = null;
	private static Material compMaterial
	{
		get
		{
			if (_compMaterial == null)
			{
				_compMaterial = new Material(compShader);
				_compMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return _compMaterial;
		}
	}
	#endregion
	
	
	void Awake()
	{
		go = gameObject;
		refCam = GetComponent<Camera>();
	}
	
	void OnDisable()
	{
		if (shaderCameraGO != null)
		{
			DestroyImmediate(shaderCameraGO);
		}
		
		if (_blurShader)
		{
			_blurShader = null;
		}
		
		if (_compShader)
		{
			_compShader = null;
		}
		
		if (_blurMaterial)
		{
			DestroyImmediate(_blurMaterial);
		}
		
		if (_compMaterial)
		{
			DestroyImmediate(_compMaterial);
		}
		
		if (stencilBuffer != null)
		{
			RenderTexture.ReleaseTemporary(stencilBuffer);
			stencilBuffer = null;
		}
	}
	
	void Start()
	{
  //      // 如果不支持图像效果，请禁用
  //      if (!SystemInfo.supportsImageEffects)
		//{
		//	Debug.LogWarning("HighlightingSystem : Image effects is not supported on this platform! Disabling.");
		//	this.enabled = false;
		//	return;
		//}

  //      // 禁用如果渲染纹理不受支持
  //      if (!SystemInfo.supportsRenderTextures)
		//{
		//	Debug.LogWarning("HighlightingSystem : RenderTextures is not supported on this platform! Disabling.");
		//	this.enabled = false;
		//	return;
		//}

        // 如果不支持渲染纹理格式，请禁用
        if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
		{
			Debug.LogWarning("HighlightingSystem : RenderTextureFormat.ARGB32 is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        // 如果不支持渲染纹理HighlightingStencilOpaque格式，请禁用
        if (!Shader.Find("Hidden/Highlighted/StencilOpaque").isSupported)
		{
			Debug.LogWarning("HighlightingSystem : HighlightingStencilOpaque shader is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        // 如果HighlightingStencilTransparent着色器不支持，请禁用
        if (!Shader.Find("Hidden/Highlighted/StencilTransparent").isSupported)
		{
			Debug.LogWarning("HighlightingSystem : HighlightingStencilTransparent shader is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        //如果 HighlightingStencilOpaqueZ 着色器不支持，请禁用
        if (!Shader.Find("Hidden/Highlighted/StencilOpaqueZ").isSupported)
		{
			Debug.LogWarning("HighlightingSystem : HighlightingStencilOpaqueZ shader is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        //如果 HighlightingStencilTransparentZ 着色器不支持，请禁用
        if (!Shader.Find("Hidden/Highlighted/StencilTransparentZ").isSupported)
		{
			Debug.LogWarning("HighlightingSystem : HighlightingStencilTransparentZ shader is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        //如果 HighlightingBlur 着色器不支持，请禁用
        if (!blurShader.isSupported)
		{
			Debug.LogWarning("HighlightingSystem : HighlightingBlur shader is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        //如果 HighlightingComposite 着色器不支持，请禁用
        if (!compShader.isSupported)
		{
			Debug.LogWarning("HighlightingSystem : HighlightingComposite shader is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        // 在模糊着色器中设置初始强度
        blurMaterial.SetFloat("_Intensity", _blurIntensity);
	}

    // 执行一次模糊迭代
    public void FourTapCone(RenderTexture source, RenderTexture dest, int iteration)
	{
		float off = blurMinSpread + iteration * blurSpread;
		blurMaterial.SetFloat("_OffsetScale", off);
		Graphics.Blit(source, dest, blurMaterial);
	}

    // Downsamples源纹理
    private void DownSample4x(RenderTexture source, RenderTexture dest)
	{
		float off = 1.0f;
		blurMaterial.SetFloat("_OffsetScale", off);
		Graphics.Blit(source, dest, blurMaterial);
	}

    // 将所有突出显示的对象呈现到模板缓冲区
    void OnPreRender()
	{
		#if UNITY_4_0
		if (this.enabled == false || go.activeInHierarchy == false)
		#else
		if (this.enabled == false || go.activeSelf == false)
		#endif
			return;
		
		if (stencilBuffer != null)
		{
			RenderTexture.ReleaseTemporary(stencilBuffer);
			stencilBuffer = null;
		}

        // 打开高亮着色器
        if (highlightingEvent != null)
		{
			highlightingEvent(true, (stencilZBufferDepth > 0));
		}
        // 如果没有高光物体，我们不需要渲染场景
        else
        {
			return;
		}

		stencilBuffer = RenderTexture.GetTemporary((int)GetComponent<Camera>().pixelWidth, (int)GetComponent<Camera>().pixelHeight, stencilZBufferDepth, RenderTextureFormat.ARGB32);

		if (!shaderCameraGO)
		{
			shaderCameraGO = new GameObject("HighlightingCamera", typeof(Camera));
			shaderCameraGO.GetComponent<Camera>().enabled = false;
			shaderCameraGO.hideFlags = HideFlags.HideAndDontSave;
		}
		
		if (!shaderCamera)
		{
			shaderCamera = shaderCameraGO.GetComponent<Camera>();
		}
		
		shaderCamera.CopyFrom(refCam);
        //shaderCamera.projectionMatrix = refCam.projectionMatrix;		// 取消注释这一行，如果你有问题使用高亮系统与自定义投影矩阵对您的相机
        shaderCamera.cullingMask = layerMask;
		shaderCamera.rect = new Rect(0f, 0f, 1f, 1f);
		shaderCamera.renderingPath = RenderingPath.VertexLit;
		shaderCamera.allowHDR = false;
		shaderCamera.useOcclusionCulling = false;
		shaderCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
		shaderCamera.clearFlags = CameraClearFlags.SolidColor;
		shaderCamera.targetTexture = stencilBuffer;
		shaderCamera.Render();

        // 关闭高亮着色器
        if (highlightingEvent != null)
		{
			highlightingEvent(false, false);
		}
	}

    // 用高光合成最后一帧
    void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
        // 如果由于某种原因没有创建stencilBuffer
        if (stencilBuffer == null)
		{
            // 只需将framebuffer传输到目的地
            Graphics.Blit(source, destination);
			return;
		}

        // 创建两个缓冲区来模糊图像
        int width = source.width / _downsampleFactor;
		int height = source.height / _downsampleFactor;
		RenderTexture buffer = RenderTexture.GetTemporary(width, height, stencilZBufferDepth, RenderTextureFormat.ARGB32);
		RenderTexture buffer2 = RenderTexture.GetTemporary(width, height, stencilZBufferDepth, RenderTextureFormat.ARGB32);

        // 复制模具缓冲到4x4的小纹理
        DownSample4x(stencilBuffer, buffer);

        // 模糊小纹理
        bool oddEven = true;
		for (int i = 0; i < iterations; i++)
		{
			if (oddEven)
			{
				FourTapCone(buffer, buffer2, i);
			}
			else
			{
				FourTapCone(buffer2, buffer, i);
			}
			
			oddEven = !oddEven;
		}

        // 构成
        compMaterial.SetTexture("_StencilTex", stencilBuffer);
		compMaterial.SetTexture("_BlurTex", oddEven ? buffer : buffer2);
		Graphics.Blit(source, destination, compMaterial);

        // 清除
        RenderTexture.ReleaseTemporary(buffer);
		RenderTexture.ReleaseTemporary(buffer2);
		if (stencilBuffer != null)
		{
			RenderTexture.ReleaseTemporary(stencilBuffer);
			stencilBuffer = null;
		}
	}
}