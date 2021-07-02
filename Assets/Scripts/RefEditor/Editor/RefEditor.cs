//Log:在面板上输入的内容无法判断输入的整数是那种数据类型的，想加一个下拉菜单用来选择
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

public enum ParameType
{
    String = 0,
    UInt8,
    UInt16,
    UInt32,
    UInt64,
    Int8,
    Int16,
    Int32,
    Int64,
    Boolean,
    Float,
}
public struct Param
{
    public object o;
    public ParameType t;
}

public class RefEditor : EditorWindow
{
    [MenuItem("Tools/反射调用函数")]
    public static void ShowAsset()
    {
        EditorWindow.GetWindow<RefEditor>(false);
    }


    private string m_ClassName;
    private string m_MethodName;
    private ParameterInfo[] m_ParamsInfo;
    private List<object> m_Params = new List<object>();
    private List<string> m_ParamNames = new List<string>();

    public GameObject m_Target;

    void ShowLabel(string str)
    {
        this.ShowNotification(new GUIContent(str));
    }
    public void refMethod()
    {
        EditorGUILayout.BeginVertical("box");

        m_ClassName = EditorGUILayout.TextField("ClassName", m_ClassName);
        m_MethodName = EditorGUILayout.TextField("MethodName", m_MethodName);

        if (GUILayout.Button("GetParame", GUILayout.Height(20)))
        {
            m_Params.Clear();
            m_ParamNames.Clear();

            var tAssembly = Assembly.Load("Assembly-CSharp");
            var tType = tAssembly.GetType(m_ClassName);
            var tMethod = tType.GetMethod(m_MethodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
            m_ParamsInfo = tMethod.GetParameters();
            if (m_ParamsInfo.Length == 0)
                return;
            for (int i = 0; i < m_ParamsInfo.Length; i++)
            {
                if (tAssembly.GetType(m_ParamsInfo[i].ParameterType.Name) == null)
                {
                    //TODO: 收集函数的参数并把参数类型和名字分别存到数组里 参数是值类型和String
                    AddParam(m_ParamsInfo[i].Name, m_ParamsInfo[i].ParameterType);
                }
                else
                {
                    //TODO: 参数是类类型
                    var tParamsClass = tAssembly.GetType(m_ParamsInfo[i].ParameterType.Name);
                    var tFields = tParamsClass.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
                    foreach (var item in tFields)
                        AddParam(item.Name, item.FieldType, "_Class" + tParamsClass.ToString() + "," + m_ParamsInfo[i].Name);
                }
            }
        }
        if (m_ParamsInfo != null && m_ParamsInfo.Length > 0)//在界面中显示参数 和 填值
        {
            string s = "";
            for (int i = 0; i < m_ParamNames.Count; i++)
            {
                string temp = "";
                if (m_ParamNames[i].Contains("_"))
                {
                    temp = m_ParamNames[i].Split('_')[1];
                    if (s != temp)
                        GUILayout.Label("------" + temp + "--------");
                    ShowParamsOnGUI(i);
                }
                else
                {
                    temp = UnityEngine.Random.Range(0, 10000).ToString();
                    ShowParamsOnGUI(i);
                }
                s = temp;
            }
        }
        if (GUILayout.Button("DO", GUILayout.Height(20)))
        {
            if (string.IsNullOrEmpty(m_ClassName) || string.IsNullOrEmpty(m_MethodName)) return;
            //判断用户设计中是否有当前程序集没有就提示一下
            var tAssembly = Assembly.Load("Assembly-CSharp");
            var tType = tAssembly.GetType(m_ClassName);
            var tMethod = tType.GetMethod(m_MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance| BindingFlags.Static);
            if (PlayerPrefs.HasKey(GetType().Name) == false)
                Debug.Log("此程序集=" + Assembly.GetExecutingAssembly().FullName);
            PlayerPrefs.SetString(GetType().Name, GetType().Name);

            try
            {
                //t表示type
                var tObejct = m_Target != null ? m_Target.GetComponent(m_ClassName) : Activator.CreateInstance(tType);
                m_ParamsInfo = tMethod.GetParameters();
                if (m_ParamsInfo.Length == 0)
                {
                    tMethod.Invoke(tObejct, null);
                    ShowLabel("无参数,已调用方法");
                }
                else
                {
                    object tPobj = null;//类类型参数实例
                    List<object> tParams = new List<object>();
                    int index = 0;
                    for (int i = 0; i < m_ParamsInfo.Length; i++)
                    {
                        if (tAssembly.GetType(m_ParamsInfo[i].ParameterType.Name) != null)
                        {
                            var tPobjDto = tAssembly.GetType(m_ParamsInfo[i].ParameterType.Name);
                            tPobj = Activator.CreateInstance(tPobjDto);
                            var tFields = tPobjDto.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
                            for (int j = 0; j < tFields.Length; j++)
                            {
                                SetFieldValue(tPobj, tFields[j].Name, m_Params[index]);
                                index++;
                            }
                            tParams.Add(tPobj);
                        }
                        else
                        {
                            index++;
                            tParams.Add(m_Params[i]);
                        }
                    }
                    var tObj = Activator.CreateInstance(tType);
                    if (tPobj == null)
                    {//基础类型.无类参数                      
                        tMethod.Invoke(tObj, m_Params.ToArray());
                    }
                    else
                    {
                        tMethod.Invoke(tObj, tParams.ToArray());
                    }
                    ShowLabel("有参数方法,已调用方法");
                }
            }
            catch (Exception)
            {
                Debug.LogError(m_ParamNames.Count);
                Debug.LogError(m_ParamsInfo.Length);
                Debug.LogError("异常了--查看类名.方法.参数");
                throw;
            }
        }
       
        EditorGUILayout.EndVertical();
    }

    void AddParam(string paramName, Type paramType, string ClassName = "")
    {
        Debug.LogError(paramType.Name);
        if (paramType.Name.Equals("String"))
            m_Params.Add("" as string);
        //else if (paramType.Name.Equals("UInt8"))
        //    m_Params.Add(0);
        //else if (paramType.Name.Equals("UInt16"))
        //    m_Params.Add(0);
        else if (paramType.Name.Equals("Int32"))
            m_Params.Add(0);
        else if (paramType.Name.Equals("Int64"))
            m_Params.Add(0);
        else if (paramType.Name.Equals("Boolean"))
            m_Params.Add(false);
        else if (paramType.Name.Equals("float"))
            m_Params.Add(0);
        m_ParamNames.Add(paramName + "(" + paramType.Name + ")" + ClassName);
    }
    void ShowParamsOnGUI(int i)
    {
        EditorGUIUtility.labelWidth = 120;
        string label = m_ParamNames[i];
        if (m_ParamNames.Contains("_"))
            label = m_ParamNames[i].Split('_')[0];
        Debug.LogError(m_Params.Count);
        if (m_Params[i] is string)
            m_Params[i] = EditorGUILayout.TextField(label, m_Params[i] as string);
        else if (m_Params[i] is int)
            m_Params[i] = EditorGUILayout.IntField(label, int.Parse(m_Params[i].ToString()));
        else if (m_Params[i] is long)
            m_Params[i] = EditorGUILayout.LongField(label, long.Parse(m_Params[i].ToString()));
        else if (m_Params[i] is float)
            m_Params[i] = EditorGUILayout.FloatField(label, float.Parse(m_Params[i].ToString()));
        else if (m_Params[i] is bool)
            m_Params[i] = EditorGUILayout.Toggle(label,bool.Parse(m_Params[i].ToString()));
    }
    void SetFieldValue(object classObj, string pField, object value)
    {
        var tAssembly = Assembly.Load("Assembly-CSharp");
        tAssembly.GetType(classObj.ToString()).GetField(pField).SetValue(classObj, value);
    }
    private void OnGUI()
    {
        refMethod();
        if (Application.isPlaying && Selection.gameObjects.Length > 0)
        {
            m_Target = Selection.gameObjects[0];
        }
        GUILayout.Space(10);
    }
}
