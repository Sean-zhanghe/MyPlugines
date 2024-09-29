using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HighlightableObject : MonoBehaviour
{
    #region Editable Fields
    // ������Ԥ����ͻ����ʾ
    public static int highlightingLayer = 7;

    // �߹⿪���ٶ�
    private static float constantOnSpeed = 4.5f;

    // �����ر��ٶ�
    private static float constantOffSpeed = 4f;

    // Ĭ�ϵ�͸����ֵֹ����û��_Cutoff���Ե���ɫ��
    private static float transparentCutoff = 0.5f;
    #endregion

    #region Private Fields
    // 2 * PI����Ҫ����˸
    private const float doublePI = 2f * Mathf.PI;

    // ����Ĳ���
    private List<HighlightingRendererCache> highlightableRenderers;

    // ����ĸ��������
    private int[] layersCache;

    // ��Ҫ���°�װ���ʲ���
    private bool materialsIsDirty = true;

    // ��ǰ����״̬
    private bool currentState = false;

    // ��ǰ����ͻ����ɫ
    private Color currentColor;

    // ת�������
    private bool transitionActive = false;

    // ��ǰ����ֵ
    private float transitionValue = 0f;

    // ��˸��Ƶ��
    private float flashingFreq = 2f;
	
	// One-frame ��������
	private bool once = false;
	
	// One-frame ������ɫ
	private Color onceColor = Color.red;

    // ��˸����
    private bool flashing = false;
	
	// ��˸��ɫ��Сֵ
	private Color flashingColorMin = new Color(0.0f, 1.0f, 1.0f, 0.0f);

    // ��˸��ɫ�����ֵ
    private Color flashingColorMax = new Color(0.0f, 1.0f, 1.0f, 1.0f);

    // ��������״̬����
    private bool constantly = false;

    // ������ɫ
    private Color constantColor = Color.yellow;

    // �ڹ��
    private bool occluder = false;

    // ��ǰʹ�õ���ɫ��ZWriting״̬
    private bool zWrite = false;

    // �ڵ���ɫ(��Ҫ�����!)
    private readonly Color occluderColor = new Color(0.0f, 0.0f, 0.0f, 0.005f);
	
	// 
	private Material highlightingMaterial
	{
		get
		{
			return zWrite ? opaqueZMaterial : opaqueMaterial;
		}
	}

    // ����(���ڴ����)�滻������ͻ����͸���ļ���ͼ��
    private Material _opaqueMaterial;
	private Material opaqueMaterial
	{
		get
		{
			if (_opaqueMaterial == null)
			{
				_opaqueMaterial = new Material(opaqueShader);
				_opaqueMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return _opaqueMaterial;
		}
	}

    // ͨ�õ�(����������)�滻���ϲ�͸���ļ���ͼ��ͻ����ʾ��z����д����
    private Material _opaqueZMaterial;
	private Material opaqueZMaterial
	{
		get
		{
			if (_opaqueZMaterial == null)
			{
				_opaqueZMaterial = new Material(opaqueZShader);
				_opaqueZMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return _opaqueZMaterial;
		}
	}
	
	// 
	private static Shader _opaqueShader;
	private static Shader opaqueShader
	{
		get
		{
			if (_opaqueShader == null)
			{
				_opaqueShader = Shader.Find("Hidden/Highlighted/StencilOpaque");
			}
			return _opaqueShader;
		}
	}
	
	// 
	private static Shader _transparentShader;
	public static Shader transparentShader
	{
		get
		{
			if (_transparentShader == null)
			{
				_transparentShader = Shader.Find("Hidden/Highlighted/StencilTransparent");
			}
			return _transparentShader;
		}
	}
	
	// 
	private static Shader _opaqueZShader;
	private static Shader opaqueZShader
	{
		get
		{
			if (_opaqueZShader == null)
			{
				_opaqueZShader = Shader.Find("Hidden/Highlighted/StencilOpaqueZ");
			}
			return _opaqueZShader;
		}
	}
	
	// 
	private static Shader _transparentZShader;
	private static Shader transparentZShader
	{
		get
		{
			if (_transparentZShader == null)
			{
				_transparentZShader = Shader.Find("Hidden/Highlighted/StencilTransparentZ");
			}
			return _transparentZShader;
		}
	}
    #endregion

    #region Common
    // ��Ⱦ��������ڲ���
    private class HighlightingRendererCache
	{
		public Renderer rendererCached;
		public GameObject goCached;
		private Material[] sourceMaterials;
		private Material[] replacementMaterials;
		private List<int> transparentMaterialIndexes;

        // ����
        public HighlightingRendererCache(Renderer rend, Material[] mats, Material sharedOpaqueMaterial, bool writeDepth)
		{
			rendererCached = rend;
			goCached = rend.gameObject;
			sourceMaterials = mats;
			replacementMaterials = new Material[mats.Length];
			transparentMaterialIndexes = new List<int>();
			
			for (int i = 0; i < mats.Length; i++)
			{
				Material sourceMat = mats[i];
				if (sourceMat == null)
				{
					continue;
				}
				string tag = sourceMat.GetTag("RenderType", true);
				if (tag == "Transparent" || tag == "TransparentCutout")
				{
					Material replacementMat = new Material(writeDepth ? transparentZShader : transparentShader);
					if (sourceMat.HasProperty("_MainTex"))
					{
						replacementMat.SetTexture("_MainTex", sourceMat.mainTexture);
						replacementMat.SetTextureOffset("_MainTex", sourceMat.mainTextureOffset);
						replacementMat.SetTextureScale("_MainTex", sourceMat.mainTextureScale);
					}
					
					replacementMat.SetFloat("_Cutoff", sourceMat.HasProperty("_Cutoff") ? sourceMat.GetFloat("_Cutoff") : transparentCutoff);
					
					replacementMaterials[i] = replacementMat;
					transparentMaterialIndexes.Add(i);
				}
				else
				{
					replacementMaterials[i] = sharedOpaqueMaterial;
				}
			}
		}

        // ���ڸ�����״̬���������������Ⱦ���Ĳ����滻Ϊ������ʾ�Ĳ��ʲ�����
        public void SetState(bool state)
		{
			rendererCached.sharedMaterials = state ? replacementMaterials : sourceMaterials;
		}

        // ����ָ������ɫ��Ϊ����͸�����ʵ�ͻ����ʾ��ɫ
        public void SetColorForTransparent(Color clr)
		{
			for (int i = 0; i < transparentMaterialIndexes.Count; i++)
			{
				replacementMaterials[transparentMaterialIndexes[i]].SetColor("_Outline", clr);
			}
		}
	}
	
	// 
	private void OnEnable()
	{
		StartCoroutine(EndOfFrame());
        // ���ĸ����¼�
        HighlightingEffect.highlightingEvent += UpdateEventHandler;
	}
	
	// 
	private void OnDisable()
	{
		StopAllCoroutines();
        // ȡ������ͻ����ʾ�¼�
        HighlightingEffect.highlightingEvent -= UpdateEventHandler;

        // ���������Ⱦ��
        if (highlightableRenderers != null)
		{
			highlightableRenderers.Clear();
		}

        // ��ͻ����ʾ��������ΪĬ��ֵ
        layersCache = null;
		materialsIsDirty = true;
		currentState = false;
		currentColor = Color.clear;
		transitionActive = false;
		transitionValue = 0f;
		once = false;
		flashing = false;
		constantly = false;
		occluder = false;
		zWrite = false;

        /* 
		// ���ø�����ʾ���Զ������
		onceColor = Color.red;
		flashingColorMin = new Color(0f, 1f, 1f, 0f);
		flashingColorMax = new Color(0f, 1f, 1f, 1f);
		flashingFreq = 2f;
		constantColor = Color.yellow;
		*/

        if (_opaqueMaterial)
		{
			DestroyImmediate(_opaqueMaterial);
		}
		
		if (_opaqueZMaterial)
		{
			DestroyImmediate(_opaqueZMaterial);
		}
	}
    #endregion

    #region Public Methods
    /// <summary>
    /// ���ʳ�ʼ�� 
    /// ���ͻ����ʾ�Ķ�����������Ĳ��ʻ��Ӷ�������ô˷���
    /// ���Զ�ε���ÿ����-��Ⱦ�����³�ʼ��ֻ�ᷢ��һ��
    /// </summary>
    public void ReinitMaterials()
	{
		materialsIsDirty = true;
	}

    /// <summary>
    /// �����ָ�ԭʼ���ϡ���ʱ�ˡ�ʹ��ReinitMaterials ()
    /// </summary>
    public void RestoreMaterials()
	{
		Debug.LogWarning("HighlightingSystem : RestoreMaterials() is obsolete. Please use ReinitMaterials() instead.");
		ReinitMaterials();
	}

    /// <summary>
    /// ����һ֡����ģʽ����ɫ
    /// </summary>
    /// <param name='color'>
    /// ������ɫ
    /// </param>
    public void OnParams(Color color)
	{
		onceColor = color;
	}

    /// <summary>
    /// �򿪵�֡������ʾ
    /// </summary>
    public void On()
	{
        // ���ڴ˿����ͻ����ʾ����
        once = true;
	}

    /// <summary>
    /// ��ָ������ɫ�򿪵�֡������ʾ
    /// ���Զ�ε���ÿ�θ��£���ɫֻ�����µĵ��ý���ʹ��
    /// </summary>
    /// <param name='color'>
    /// Highlighting color.
    /// </param>
    public void On(Color color)
	{
        // Ϊһ֡������������ɫ
        onceColor = color;
		On();
	}

    /// <summary>
    /// ��˸�Ĳ�������
    /// </summary>
    /// <param name='color1'>
    /// Starting color.
    /// </param>
    /// <param name='color2'>
    /// Ending color.
    /// </param>
    /// <param name='freq'>
    /// Flashing frequency.
    /// </param>
    public void FlashingParams(Color color1, Color color2, float freq)
	{
		flashingColorMin = color1;
		flashingColorMax = color2;
		flashingFreq = freq;
	}

    /// <summary>
    /// ����˸
    /// </summary>
    public void FlashingOn()
	{
		flashing = true;
	}

    /// <summary>
    /// ����ɫ1�л�����ɫ2
    /// </summary>
    /// <param name='color1'>
    /// Starting color.
    /// </param>
    /// <param name='color2'>
    /// Ending color.
    /// </param>
    public void FlashingOn(Color color1, Color color2)
	{
		flashingColorMin = color1;
		flashingColorMax = color2;
		FlashingOn();
	}

    /// <summary>
    /// ��color1��color2��ָ��Ƶ�ʴ���˸
    /// </summary>
    /// <param name='color1'>
    /// Starting color.
    /// </param>
    /// <param name='color2'>
    /// Ending color.
    /// </param>
    /// <param name='freq'>
    /// Flashing frequency.
    /// </param>
    public void FlashingOn(Color color1, Color color2, float freq)
	{
		flashingFreq = freq;
		FlashingOn(color1, color2);
	}

    /// <summary>
    /// ���涨��Ƶ�ʴ������
    /// </summary>
    /// <param name='f'>
    /// Flashing frequency.
    /// </param>
    public void FlashingOn(float freq)
	{
		flashingFreq = freq;
		FlashingOn();
	}

    /// <summary>
    /// �ص�����
    /// </summary>
    public void FlashingOff()
	{
		flashing = false;
	}

    /// <summary>
    /// �л�����ģʽ
    /// </summary>
    public void FlashingSwitch()
	{
		flashing = !flashing;
	}

    /// <summary>
    /// ���ó���������ɫ
    /// </summary>
    /// <param name='color'>
    /// ����ǿ����ɫ
    /// </param>
    public void ConstantParams(Color color)
	{
		constantColor = color;
	}

    /// <summary>
    /// ��������ĸ߹�
    /// </summary>
    public void ConstantOn()
	{
        // ʹ����͹��
        constantly = true;
		// ��ʼ����
		transitionActive = true;
	}

    /// <summary>
    /// ��ɫ�ڲ���ͻ�����������ɫ
    /// </summary>
    /// <param name='color'>
    /// Constant highlighting color.
    /// </param>
    public void ConstantOn(Color color)
	{
        // ���ó���������ɫ
        constantColor = color;
		ConstantOn();
	}

    /// <summary>
    /// ���������ĸ߹�
    /// </summary>
    public void ConstantOff()
	{
        // ���ò���͹��
        constantly = false;
		// Start transition
		transitionActive = true;
	}

    /// <summary>
    /// �л�����͹��
    /// </summary>
    public void ConstantSwitch()
	{
		// Switch constant highlighting
		constantly = !constantly;
		// Start transition
		transitionActive = true;
	}

    /// <summary>
    /// �����򿪳�������(����ɫ)
    /// </summary>
    public void ConstantOnImmediate()
	{
		constantly = true;
		// Set transition value to 1
		transitionValue = 1f;
		// Stop transition
		transitionActive = false;
	}

    /// <summary>
    /// ������ָ������ɫ�򿪳�������(����ɫ)
    /// </summary>
    /// <param name='color'>
    /// Constant highlighting color.
    /// </param>
    public void ConstantOnImmediate(Color color)
	{
		// Set constant highlighting color
		constantColor = color;
		ConstantOnImmediate();
	}

    /// <summary>
    /// �����رճ�������(������)
    /// </summary>
    public void ConstantOffImmediate()
	{
		constantly = false;
		// Set transition value to 0
		transitionValue = 0f;
		// Stop transition
		transitionActive = false;
	}

    /// <summary>
    /// �����л���������(������/����)
    /// </summary>
    public void ConstantSwitchImmediate()
	{
		constantly = !constantly;
		// Set transition value to the final value
		transitionValue = constantly ? 1f : 0f;
		// Stop transition
		transitionActive = false;
	}

    /// <summary>
    /// �����ڹ��ģʽ
    /// </summary>
    public void OccluderOn()
	{
		occluder = true;
	}

    /// <summary>
    /// �ر��ڹ��ģʽ
    /// </summary>
    public void OccluderOff()
	{
		occluder = false;
	}

    /// <summary>
    /// �л��ڹ��ģʽ
    /// </summary>
    public void OccluderSwitch()
	{
		occluder = !occluder;
	}

    /// <summary>
    /// �ص��������͵ĸ�����ʾ
    /// </summary>
    public void Off()
	{
		// Turn off all types of highlighting
		once = false;
		flashing = false;
		constantly = false;
		// Set transition value to 0
		transitionValue = 0f;
		// Stop transition
		transitionActive = false;
	}

    /// <summary>
    /// �������HighlightableObject���
    /// </summary>
    public void Die()
	{
		Destroy(this);
	}
    #endregion


    #region Private Methods
    // ���ʵĳ�ʼ��
    private void InitMaterials(bool writeDepth)
	{
		currentState = false;
		
		zWrite = writeDepth;
		
		highlightableRenderers = new List<HighlightingRendererCache>();
		
		MeshRenderer[] mr = GetComponentsInChildren<MeshRenderer>();
		CacheRenderers(mr);
		
		SkinnedMeshRenderer[] smr = GetComponentsInChildren<SkinnedMeshRenderer>();
		CacheRenderers(smr);
		
		#if !UNITY_FLASH
		//ClothRenderer[] cr = GetComponentsInChildren<ClothRenderer>();
		//CacheRenderers(cr);
		#endif
		
		currentState = false;
		materialsIsDirty = false;
		currentColor = Color.clear;
	}

    // �����������Ⱦ������
    private void CacheRenderers(Renderer[] renderers)
	{
		for (int i = 0; i < renderers.Length; i++)
		{
			Material[] materials = renderers[i].sharedMaterials;
			
			if (materials != null)
			{
				highlightableRenderers.Add(new HighlightingRendererCache(renderers[i], materials, highlightingMaterial, zWrite));
			}
		}
	}

    // ����ͻ����ʾ��ɫ��һ��������ֵ
    private void SetColor(Color c)
	{
		if (currentColor == c)
		{
			return;
		}
		
		if (zWrite)
		{
			opaqueZMaterial.SetColor("_Outline", c);
		}
		else
		{
			opaqueMaterial.SetColor("_Outline", c);
		}
		
		for (int i = 0; i < highlightableRenderers.Count; i++)
		{
			highlightableRenderers[i].SetColorForTransparent(c);
		}
		
		currentColor = c;
	}

    // �����Ҫ�������µ���ɫ
    private void UpdateColors()
	{
        // ���������ʾ�����ã���Ҫ������ɫ
        if (currentState == false)
		{
			return;
		}
		
		if (occluder)
		{
			SetColor(occluderColor);
			return;
		}
		
		if (once)
		{
			SetColor(onceColor);
			return;
		}
		
		if (flashing)
		{
            // ����Ƶ�ʲ���ʱ��߶ȵ�Ӱ��
            Color c = Color.Lerp(flashingColorMin, flashingColorMax, 0.5f * Mathf.Sin(Time.realtimeSinceStartup * flashingFreq * doublePI) + 0.5f);
			SetColor(c);
			return;
		}
		
		if (transitionActive)
		{
			Color c = new Color(constantColor.r, constantColor.g, constantColor.b, constantColor.a * transitionValue);
			SetColor(c);
			return;
		}
		else if (constantly)
		{
			SetColor(constantColor);
			return;
		}
	}

    // �����Ҫ�������µ�ת��ֵ
    private void PerformTransition()
	{
		if (transitionActive == false)
		{
			return;
		}
		
		float targetValue = constantly ? 1f : 0f;

        // �Ƿ���ɹ���
        if (transitionValue == targetValue)
		{
			transitionActive = false;
			return;
		}
		
		if (Time.timeScale != 0f)
		{
            // ���㲻��ʱ��Ӱ���ʱ������
            float unscaledDeltaTime = Time.deltaTime / Time.timeScale;

            // �����µĹ���ֵ
            transitionValue += (constantly ? constantOnSpeed : -constantOffSpeed) * unscaledDeltaTime;
			transitionValue = Mathf.Clamp01(transitionValue);
		}
		else
		{
			return;
		}
	}

    // ͻ����ʾ�¼��������(��ͻ����ʾ����)
    private void UpdateEventHandler(bool trigger, bool writeDepth)
	{
        // ���²����ø�����ʾ
        if (trigger)
		{
            // ZWriting״̬�Ƿ�ı�
            if (zWrite != writeDepth)
			{
				materialsIsDirty = true;
			}

            // �����Ҫ����ʼ���²���
            if (materialsIsDirty)
			{
				InitMaterials(writeDepth);
			}
			
			currentState = (once || flashing || constantly || transitionActive || occluder);
			
			if (currentState)
			{
				UpdateColors();
				PerformTransition();
				
				if (highlightableRenderers != null)
				{
					layersCache = new int[highlightableRenderers.Count];
					for (int i = 0; i < highlightableRenderers.Count; i++)
					{
						GameObject go = highlightableRenderers[i].goCached;
                        // �����
                        layersCache[i] = go.layer;
                        // ��ʱ���ò���Ⱦ�ɸ���Ч�����
                        go.layer = highlightingLayer;
						highlightableRenderers[i].SetState(true);
					}
				}
			}
		}
        // ���ø�����ʾ
        else
        {
			if (currentState && highlightableRenderers != null)
			{
				for (int i = 0; i < highlightableRenderers.Count; i++)
				{
					highlightableRenderers[i].goCached.layer = layersCache[i];
					highlightableRenderers[i].SetState(false);
				}
			}
		}
	}
	
	IEnumerator EndOfFrame()
	{
		while (enabled)
		{
			yield return new WaitForEndOfFrame();
            // �ڳ����е�ÿ���߹�Ч�������Ⱦ������һ֡�߹�״̬
            once = false;
		}
	}
	#endregion
}