using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class TextFit : Text
{
    /// <summary>
    /// 用于匹配标点符号（正则表达式）
    /// </summary>
    private readonly string strRegex = @"\p{P}(?<![《“(])"; // 除外《和“和（
    //private readonly string strRegex = @"\p{P}";
    /// <summary>
    /// 用于存储text组件中的内容
    /// </summary>
    private System.Text.StringBuilder MExplainText = null;

    /// <summary>
    /// 用于存储text生成器中的内容
    /// </summary>
    private IList<UILineInfo> MExpalinTextLine;
    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        base.OnPopulateMesh(toFill);
        StartCoroutine(MClearUpExplainMode(this, text));
    }
    /// <summary>
    /// 整理文字。确保首字母不出现标点
    /// </summary>
    /// <param name="_component">text组件</param>
    /// <param name="_text">需要填入text中的内容</param>
    /// <returns></returns>
    IEnumerator MClearUpExplainMode(Text _component, string _text)
    {
        _component.text = _text;

        // 如果直接执行下边方法的话，那么_component.cachedTextGenerator.lines将会获取的是之前text中的内容，而不是_text的内容，所以需要等待一下
        yield return new WaitForSeconds(0.001f);

        MExpalinTextLine = _component.cachedTextGenerator.lines;

        // 需要改变的字符序号
        int mChangeIndex = -1;

        // 用于存储最终修改的文本
        System.Text.StringBuilder MExplainText = new System.Text.StringBuilder(_component.text);

        // 从第二行开始进行检测
        for (int i = 1; i < MExpalinTextLine.Count; i++)
        {
            if (MExpalinTextLine[i].startCharIdx >= MExplainText.Length) continue;
            // 首位是否有标点
            bool _b = Regex.IsMatch(MExplainText[MExpalinTextLine[i].startCharIdx].ToString(), strRegex);
            if (_b)
            {
                mChangeIndex = MExpalinTextLine[i].startCharIdx - 1;

                // 解决连续多个都是标点符号的问题
                for (int j = mChangeIndex; j > 0; j--)
                {
                    bool _c = Regex.IsMatch(MExplainText[j].ToString(), strRegex);
                    if (_c)
                    {
                        //Debug.Log("连续多个都是标点符号" + "," + mChangeIndex);
                        mChangeIndex--;
                    }
                    else
                    {
                        break;
                    }
                }

                // 在指定位置插入换行符
                MExplainText.Insert(mChangeIndex, "\n");
                //Debug.Log("添加回车" + "," + mChangeIndex);

                // 更新组件文本
                _component.text = MExplainText.ToString();

                // 等待片刻以更新缓存
                yield return new WaitForSeconds(0.001f);

                // 重新获取行信息
                MExpalinTextLine = _component.cachedTextGenerator.lines;

                // 从当前行重新开始检测
                i = Mathf.Max(i - 1, 1); // 保证不小于1
            }

        }

        _component.text = MExplainText.ToString();
    }

}