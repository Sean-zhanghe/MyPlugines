using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace WeiDaEdu
{
    public static class MethodExtension
    {
        /// <summary>
        /// 将一个物体的所有子物体的材质都设置为透明
        /// </summary>
        /// <param name="trans"></param>
        public static void ChangeRenderToTransparent(this Transform trans, bool exceptParticle = true)
        {
            if (exceptParticle && trans.GetComponent<ParticleSystem>() != null)
                return;

            //改变自己，如果有子物体则遍历改变子物体
            if (trans.GetComponent<Renderer>() != null)
            {
                var transparentMat = Resources.Load<Material>("Materials/Transparent");
                Debug.Assert(transparentMat != null, "Transparent Material Doesn't Exist！");
                trans.GetComponent<Renderer>().material = transparentMat;
            }

            var childCount = trans.childCount;
            for (int i = 0; i < childCount; i++)
                ChangeRenderToTransparent(trans.GetChild(i));
        }

        /// <summary>
        /// 将一个物体的所有子物体的材质都设置为透明
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="duration">fade效果持续时间</param>
        /// <param name="easeMode">缓动曲线</param>
        /// <param name="callback">回调</param>
        public static void RendererFadeOut(this Transform trans, float duration = 1, Action callback = null, Ease easeMode = Ease.Linear)
        {
            for (int i = 0; i < trans.childCount; i++)
            {
                var child = trans.GetChild(i);
                RendererFadeOut(child, duration, easeMode: easeMode);
                if (child.childCount != 0) //非最底层子物体则更改自己的材质
                    FadeOutATrans(child, duration, easeMode);
            }

            if (trans.childCount == 0) //针对根节点无子物体及最底层子物体进行材质更改。
                FadeOutATrans(trans, duration, easeMode);

            DOVirtual.DelayedCall(duration, () => callback?.Invoke());
        }

        private static void FadeOutATrans(Transform child, float duration, Ease ease = Ease.Linear)
        {
            if (child.GetComponent<Renderer>() != null)
            {
                //设置材质的透明度
                var material = child.GetComponent<Renderer>().material;
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                child.GetComponent<Renderer>().material.DOFade(0, duration).SetEase(ease);
            }
        }

        /// <summary>
        /// 将一个物体的所有子物体的材质都从透明渐显
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="duration">fade效果持续时间</param>
        /// <param name="easeMode">缓动曲线</param>
        /// <param name="callback">回调</param>
        public static void RendererFadeIn(this Transform trans, float duration = 1, Action callback = null, Ease easeMode = Ease.Linear)
        {
            for (int i = 0; i < trans.childCount; i++)
            {
                var child = trans.GetChild(i);
                RendererFadeIn(child, duration, easeMode: easeMode);
                if (child.childCount != 0) //非最底层子物体则更改自己的材质
                    FadeInATrans(child, duration, easeMode);
            }

            if (trans.childCount == 0)
                FadeInATrans(trans, duration, easeMode);

            DOVirtual.DelayedCall(duration, () => callback?.Invoke());
        }

        private static void FadeInATrans(Transform child, float duration, Ease ease = Ease.Linear)
        {
            if (child.GetComponent<Renderer>() != null)
            {
                //设置材质的透明度
                var material = child.GetComponent<Renderer>().material;
                var originMaterial = new Material(material);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;

                //alpha = 0
                var originColor = child.GetComponent<Renderer>().material.color;
                child.GetComponent<Renderer>().material.color =
                    new Color(originColor.r, originColor.g, originColor.b, 0);

                //fade in & reset material when complete
                child.GetComponent<Renderer>().material.DOFade(1, duration)
                    .OnComplete(() => child.GetComponent<Renderer>().material = originMaterial).SetEase(ease);
            }
        }

        /// <summary>
        /// 删除旗下所有子物体
        /// </summary>
        /// <param name="trans"></param>
        public static List<string> DestroyChild(this Transform trans, List<string> exceptNames = null)
        {
            List<string> destroyNames = new List<string>();
            if (trans == null)
                return destroyNames;

            for (int i = 0; i < trans.childCount; i++)
            {
                //排除不删除的物体
                if (exceptNames != null && exceptNames.Contains(trans.GetChild(i).name))
                    continue;

                //防止trans内寻找同名物体时找到被打destroy标记的物体，导致逻辑异常
                var tempChild = trans.GetChild(i--);
                tempChild.SetParent(null);

                //记录被删除的物体的名字
                destroyNames.Add(tempChild.name);
                GameObject.Destroy(tempChild.gameObject);
            }

            return destroyNames;
        }

        /// <summary>
        /// 随机销毁count数量的子物体
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="count">删除的子物体的数量</param>
        /// <returns>返回被删除的对象Position</returns>
        public static Vector3[] DestroyChildRandomly(this Transform trans, int count)
        {
            if (trans == null)
                return null;

            Vector3[] childPoses = new Vector3[count];
            for (int i = 0; i < trans.childCount && i < count; i++)
            {
                var sibling = Random.Range(0, trans.childCount);
                var targetChild = trans.GetChild(sibling);
                childPoses[i] = targetChild.position;
                targetChild.SetParent(null);
                GameObject.Destroy(targetChild.gameObject);
            }

            return childPoses;
        }

        /// <summary>
        /// 随机销毁count数量的子物体
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="count">删除的子物体的数量</param>
        /// <returns>返回被删除的对象Position</returns>
        public static List<string> ActiveAllChilden(this Transform trans, bool isShow = true, List<string> exceptNames = null)
        {
            List<string> hideNames = new List<string>();
            if (trans == null)
                return hideNames;

            for (int i = 0; i < trans.childCount; i++)
            {
                //排除不删除的物体
                if (exceptNames != null && exceptNames.Contains(trans.GetChild(i).name))
                    continue;

                var tempChild = trans.GetChild(i);

                //记录被隐藏的物体的名字
                hideNames.Add(tempChild.name);
                tempChild.gameObject.SetActive(isShow);
            }

            return hideNames;
        }

        public static Transform ActiveChild(this Transform trans, string name, bool isShow = true)
        {
            for (int i = 0; i < trans.childCount; i++)
            {
                var child = trans.GetChild(i);
                if (child.name == name)
                {
                    child.gameObject.SetActive(isShow);
                    return child;
                }
            }

            return null;
        }

        public static void ScrollToTarget(this ScrollRect rect, RectTransform target)
        {

            float length;
            //如果是横向
            if (rect.horizontal)
            {
                length = rect.content.rect.width - rect.viewport.rect.width;
                float targetX = target.localPosition.x - target.rect.width / 2;
                rect.DOHorizontalNormalizedPos(targetX / length, 1f);
            }
          
            if (rect.vertical)
            {
                length = rect.content.rect.height - rect.viewport.rect.height;
                float targetY = rect.content.rect.height + target.localPosition.y + target.rect.height / 2;
                targetY-= rect.viewport.rect.height;
                rect.DOVerticalNormalizedPos(targetY / length, 1f);
            }
        }

    }

}
