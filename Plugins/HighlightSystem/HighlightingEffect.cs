using UnityEngine;

// ί��ͻ����ʾ�¼�
public delegate void HighlightingEventHandler(bool state, bool zWrite);

[RequireComponent(typeof(Camera))]
public class HighlightingEffect : MonoBehaviour
{
    // �¼�����������ͻ����ʾ����������֮ǰ֪ͨhighlightable������������
    public static event HighlightingEventHandler highlightingEvent;

    #region Inspector Fields
    // ģ��(������ʾ)�������   0  Ĭ��  1  ǿ��  2  �ٶ�  3  ����
    [Header("������ʾ�������")]
    public int stencilZBufferDepth = 0;

    // ģ��(������ʾ)�����С���Ͳ�������
    [Header("������ʾ�����С���Ͳ�������")]
    public int _downsampleFactor = 4;

    // ģ������
    [Header("ģ������")]
    public int iterations = 2;

    // ģ����С����
    [Header("ģ����С����")]
    [Range(0.0f,3.0f)]
    public float blurMinSpread = 0.65f;

    // ÿ�ε�����ģ����չ
    [Header("ÿ�ε�����ģ����չ")]
    [Range(0.0f, 3.0f)]
    public float blurSpread = 0.25f;

    // ģ�����ʵ�ģ��ǿ��
    [Header("ģ�����ʵ�ģ��ǿ��")]
    [Range(0.0f, 1.0f)]
    public float _blurIntensity = 0.3f;

    // ��Щ����ֻ�ڱ༭���п��á������ǲ���Ҫ�ڶ���������ʹ������
#if UNITY_EDITOR
    // z����д״̬getter/setter
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

    // ����������getter / setter
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

    // ģ��alphaǿ�ȵĻ�ȡ/����
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
    // ͻ����ʾ���ͼ�㣬�޳��ɰ�
    private int layerMask = (1 << HighlightableObject.highlightingLayer);

    // ���GameObject����
    private GameObject go = null;

    // ��Ⱦģ�建����Ϸ���������ͷ
    private GameObject shaderCameraGO = null;

    // ��Ⱦģ�建�������ͷ
    private Camera shaderCamera = null;

    // ��Ⱦ������ģ�߻���
    private RenderTexture stencilBuffer = null;

    // ����ο�
    private Camera refCam = null;

    // ģ������
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

    // �ϳɲ���
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

    // ģ������
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

    // �ϳɲ���
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
  //      // �����֧��ͼ��Ч���������
  //      if (!SystemInfo.supportsImageEffects)
		//{
		//	Debug.LogWarning("HighlightingSystem : Image effects is not supported on this platform! Disabling.");
		//	this.enabled = false;
		//	return;
		//}

  //      // ���������Ⱦ������֧��
  //      if (!SystemInfo.supportsRenderTextures)
		//{
		//	Debug.LogWarning("HighlightingSystem : RenderTextures is not supported on this platform! Disabling.");
		//	this.enabled = false;
		//	return;
		//}

        // �����֧����Ⱦ�����ʽ�������
        if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
		{
			Debug.LogWarning("HighlightingSystem : RenderTextureFormat.ARGB32 is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        // �����֧����Ⱦ����HighlightingStencilOpaque��ʽ�������
        if (!Shader.Find("Hidden/Highlighted/StencilOpaque").isSupported)
		{
			Debug.LogWarning("HighlightingSystem : HighlightingStencilOpaque shader is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        // ���HighlightingStencilTransparent��ɫ����֧�֣������
        if (!Shader.Find("Hidden/Highlighted/StencilTransparent").isSupported)
		{
			Debug.LogWarning("HighlightingSystem : HighlightingStencilTransparent shader is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        //��� HighlightingStencilOpaqueZ ��ɫ����֧�֣������
        if (!Shader.Find("Hidden/Highlighted/StencilOpaqueZ").isSupported)
		{
			Debug.LogWarning("HighlightingSystem : HighlightingStencilOpaqueZ shader is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        //��� HighlightingStencilTransparentZ ��ɫ����֧�֣������
        if (!Shader.Find("Hidden/Highlighted/StencilTransparentZ").isSupported)
		{
			Debug.LogWarning("HighlightingSystem : HighlightingStencilTransparentZ shader is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        //��� HighlightingBlur ��ɫ����֧�֣������
        if (!blurShader.isSupported)
		{
			Debug.LogWarning("HighlightingSystem : HighlightingBlur shader is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        //��� HighlightingComposite ��ɫ����֧�֣������
        if (!compShader.isSupported)
		{
			Debug.LogWarning("HighlightingSystem : HighlightingComposite shader is not supported on this platform! Disabling.");
			this.enabled = false;
			return;
		}

        // ��ģ����ɫ�������ó�ʼǿ��
        blurMaterial.SetFloat("_Intensity", _blurIntensity);
	}

    // ִ��һ��ģ������
    public void FourTapCone(RenderTexture source, RenderTexture dest, int iteration)
	{
		float off = blurMinSpread + iteration * blurSpread;
		blurMaterial.SetFloat("_OffsetScale", off);
		Graphics.Blit(source, dest, blurMaterial);
	}

    // DownsamplesԴ����
    private void DownSample4x(RenderTexture source, RenderTexture dest)
	{
		float off = 1.0f;
		blurMaterial.SetFloat("_OffsetScale", off);
		Graphics.Blit(source, dest, blurMaterial);
	}

    // ������ͻ����ʾ�Ķ�����ֵ�ģ�建����
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

        // �򿪸�����ɫ��
        if (highlightingEvent != null)
		{
			highlightingEvent(true, (stencilZBufferDepth > 0));
		}
        // ���û�и߹����壬���ǲ���Ҫ��Ⱦ����
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
        //shaderCamera.projectionMatrix = refCam.projectionMatrix;		// ȡ��ע����һ�У������������ʹ�ø���ϵͳ���Զ���ͶӰ������������
        shaderCamera.cullingMask = layerMask;
		shaderCamera.rect = new Rect(0f, 0f, 1f, 1f);
		shaderCamera.renderingPath = RenderingPath.VertexLit;
		shaderCamera.allowHDR = false;
		shaderCamera.useOcclusionCulling = false;
		shaderCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
		shaderCamera.clearFlags = CameraClearFlags.SolidColor;
		shaderCamera.targetTexture = stencilBuffer;
		shaderCamera.Render();

        // �رո�����ɫ��
        if (highlightingEvent != null)
		{
			highlightingEvent(false, false);
		}
	}

    // �ø߹�ϳ����һ֡
    void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
        // �������ĳ��ԭ��û�д���stencilBuffer
        if (stencilBuffer == null)
		{
            // ֻ�轫framebuffer���䵽Ŀ�ĵ�
            Graphics.Blit(source, destination);
			return;
		}

        // ����������������ģ��ͼ��
        int width = source.width / _downsampleFactor;
		int height = source.height / _downsampleFactor;
		RenderTexture buffer = RenderTexture.GetTemporary(width, height, stencilZBufferDepth, RenderTextureFormat.ARGB32);
		RenderTexture buffer2 = RenderTexture.GetTemporary(width, height, stencilZBufferDepth, RenderTextureFormat.ARGB32);

        // ����ģ�߻��嵽4x4��С����
        DownSample4x(stencilBuffer, buffer);

        // ģ��С����
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

        // ����
        compMaterial.SetTexture("_StencilTex", stencilBuffer);
		compMaterial.SetTexture("_BlurTex", oddEven ? buffer : buffer2);
		Graphics.Blit(source, destination, compMaterial);

        // ���
        RenderTexture.ReleaseTemporary(buffer);
		RenderTexture.ReleaseTemporary(buffer2);
		if (stencilBuffer != null)
		{
			RenderTexture.ReleaseTemporary(stencilBuffer);
			stencilBuffer = null;
		}
	}
}