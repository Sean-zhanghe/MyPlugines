using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HighlightableObject : MonoBehaviour
{
    #region Editable Fields
    // 建立层预留给突出显示
    public static int highlightingLayer = 7;

    // 高光开启速度
    private static float constantOnSpeed = 4.5f;

    // 高亮关闭速度
    private static float constantOffSpeed = 4f;

    // 默认的透明截止值用于没有_Cutoff属性的着色器
    private static float transparentCutoff = 0.5f;
    #endregion

    #region Private Fields
    // 2 * PI常数要求闪烁
    private const float doublePI = 2f * Mathf.PI;

    // 缓存的材质
    private List<HighlightingRendererCache> highlightableRenderers;

    // 缓存的高亮对象层
    private int[] layersCache;

    // 需要重新安装材质布尔
    private bool materialsIsDirty = true;

    // 当前高亮状态
    private bool currentState = false;

    // 当前材质突出颜色
    private Color currentColor;

    // 转换活动布尔
    private bool transitionActive = false;

    // 当前过渡值
    private float transitionValue = 0f;

    // 闪烁的频率
    private float flashingFreq = 2f;
	
	// One-frame 高亮布尔
	private bool once = false;
	
	// One-frame 高亮颜色
	private Color onceColor = Color.red;

    // 闪烁布尔
    private bool flashing = false;
	
	// 闪烁颜色最小值
	private Color flashingColorMin = new Color(0.0f, 1.0f, 1.0f, 0.0f);

    // 闪烁的色彩最大值
    private Color flashingColorMax = new Color(0.0f, 1.0f, 1.0f, 1.0f);

    // 常量高亮状态布尔
    private bool constantly = false;

    // 持续颜色
    private Color constantColor = Color.yellow;

    // 遮光板
    private bool occluder = false;

    // 当前使用的着色器ZWriting状态
    private bool zWrite = false;

    // 遮挡颜色(不要碰这个!)
    private readonly Color occluderColor = new Color(0.0f, 0.0f, 0.0f, 0.005f);
	
	// 
	private Material highlightingMaterial
	{
		get
		{
			return zWrite ? opaqueZMaterial : opaqueMaterial;
		}
	}

    // 常用(用于此组件)替换材质以突出不透明的几何图形
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

    // 通用的(对于这个组件)替换材料不透明的几何图形突出显示与z缓冲写启用
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
    // 渲染器缓存的内部类
    private class HighlightingRendererCache
	{
		public Renderer rendererCached;
		public GameObject goCached;
		private Material[] sourceMaterials;
		private Material[] replacementMaterials;
		private List<int> transparentMaterialIndexes;

        // 构造
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

        // 基于给定的状态变量，将缓存的渲染器的材质替换为高亮显示的材质并返回
        public void SetState(bool state)
		{
			rendererCached.sharedMaterials = state ? replacementMaterials : sourceMaterials;
		}

        // 设置指定的颜色作为所有透明材质的突出显示颜色
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
        // 订阅高亮事件
        HighlightingEffect.highlightingEvent += UpdateEventHandler;
	}
	
	// 
	private void OnDisable()
	{
		StopAllCoroutines();
        // 取消订阅突出显示事件
        HighlightingEffect.highlightingEvent -= UpdateEventHandler;

        // 清楚缓存渲染器
        if (highlightableRenderers != null)
		{
			highlightableRenderers.Clear();
		}

        // 将突出显示参数重置为默认值
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
		// 重置高亮显示的自定义参数
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
    /// 材质初始化 
    /// 如果突出显示的对象更改了它的材质或子对象，请调用此方法
    /// 可以多次调用每更新-渲染器重新初始化只会发生一次
    /// </summary>
    public void ReinitMaterials()
	{
		materialsIsDirty = true;
	}

    /// <summary>
    /// 立即恢复原始材料。过时了。使用ReinitMaterials ()
    /// </summary>
    public void RestoreMaterials()
	{
		Debug.LogWarning("HighlightingSystem : RestoreMaterials() is obsolete. Please use ReinitMaterials() instead.");
		ReinitMaterials();
	}

    /// <summary>
    /// 设置一帧高亮模式的颜色
    /// </summary>
    /// <param name='color'>
    /// 高亮颜色
    /// </param>
    public void OnParams(Color color)
	{
		onceColor = color;
	}

    /// <summary>
    /// 打开单帧高亮显示
    /// </summary>
    public void On()
	{
        // 仅在此框架中突出显示对象
        once = true;
	}

    /// <summary>
    /// 用指定的颜色打开单帧高亮显示
    /// 可以多次调用每次更新，颜色只从最新的调用将被使用
    /// </summary>
    /// <param name='color'>
    /// Highlighting color.
    /// </param>
    public void On(Color color)
	{
        // 为一帧高亮设置新颜色
        onceColor = color;
		On();
	}

    /// <summary>
    /// 闪烁的参数设置
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
    /// 打开闪烁
    /// </summary>
    public void FlashingOn()
	{
		flashing = true;
	}

    /// <summary>
    /// 从颜色1切换到颜色2
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
    /// 从color1到color2按指定频率打开闪烁
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
    /// 按规定的频率打开闪光灯
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
    /// 关掉闪光
    /// </summary>
    public void FlashingOff()
	{
		flashing = false;
	}

    /// <summary>
    /// 切换闪光模式
    /// </summary>
    public void FlashingSwitch()
	{
		flashing = !flashing;
	}

    /// <summary>
    /// 设置常量高亮颜色
    /// </summary>
    /// <param name='color'>
    /// 不断强调颜色
    /// </param>
    public void ConstantParams(Color color)
	{
		constantColor = color;
	}

    /// <summary>
    /// 淡入持续的高光
    /// </summary>
    public void ConstantOn()
	{
        // 使不断凸显
        constantly = true;
		// 开始过渡
		transitionActive = true;
	}

    /// <summary>
    /// 褪色在不断突出与给定的颜色
    /// </summary>
    /// <param name='color'>
    /// Constant highlighting color.
    /// </param>
    public void ConstantOn(Color color)
	{
        // 设置常量高亮颜色
        constantColor = color;
		ConstantOn();
	}

    /// <summary>
    /// 淡出持续的高光
    /// </summary>
    public void ConstantOff()
	{
        // 禁用不断凸显
        constantly = false;
		// Start transition
		transitionActive = true;
	}

    /// <summary>
    /// 切换不断凸显
    /// </summary>
    public void ConstantSwitch()
	{
		// Switch constant highlighting
		constantly = !constantly;
		// Start transition
		transitionActive = true;
	}

    /// <summary>
    /// 立即打开持续高亮(不褪色)
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
    /// 立即用指定的颜色打开持续高亮(不褪色)
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
    /// 立即关闭持续高亮(不淡出)
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
    /// 立即切换常量高亮(不淡入/淡出)
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
    /// 启用遮光板模式
    /// </summary>
    public void OccluderOn()
	{
		occluder = true;
	}

    /// <summary>
    /// 关闭遮光板模式
    /// </summary>
    public void OccluderOff()
	{
		occluder = false;
	}

    /// <summary>
    /// 切换遮光板模式
    /// </summary>
    public void OccluderSwitch()
	{
		occluder = !occluder;
	}

    /// <summary>
    /// 关掉所有类型的高亮显示
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
    /// 销毁这个HighlightableObject组件
    /// </summary>
    public void Die()
	{
		Destroy(this);
	}
    #endregion


    #region Private Methods
    // 材质的初始化
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

    // 缓存给定的渲染器属性
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

    // 更新突出显示颜色到一个给定的值
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

    // 如果需要，设置新的颜色
    private void UpdateColors()
	{
        // 如果高亮显示被禁用，不要更新颜色
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
            // 闪光频率不受时间尺度的影响
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

    // 如果需要，计算新的转换值
    private void PerformTransition()
	{
		if (transitionActive == false)
		{
			return;
		}
		
		float targetValue = constantly ? 1f : 0f;

        // 是否完成过渡
        if (transitionValue == targetValue)
		{
			transitionActive = false;
			return;
		}
		
		if (Time.timeScale != 0f)
		{
            // 计算不受时间影响的时间增量
            float unscaledDeltaTime = Time.deltaTime / Time.timeScale;

            // 计算新的过渡值
            transitionValue += (constantly ? constantOnSpeed : -constantOffSpeed) * unscaledDeltaTime;
			transitionValue = Mathf.Clamp01(transitionValue);
		}
		else
		{
			return;
		}
	}

    // 突出显示事件处理程序(主突出显示方法)
    private void UpdateEventHandler(bool trigger, bool writeDepth)
	{
        // 更新并启用高亮显示
        if (trigger)
		{
            // ZWriting状态是否改变
            if (zWrite != writeDepth)
			{
				materialsIsDirty = true;
			}

            // 如果需要，初始化新材质
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
                        // 缓存层
                        layersCache[i] = go.layer;
                        // 临时设置层渲染由高亮效果相机
                        go.layer = highlightingLayer;
						highlightableRenderers[i].SetState(true);
					}
				}
			}
		}
        // 禁用高亮显示
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
            // 在场景中的每个高光效果完成渲染后，重置一帧高光状态
            once = false;
		}
	}
	#endregion
}