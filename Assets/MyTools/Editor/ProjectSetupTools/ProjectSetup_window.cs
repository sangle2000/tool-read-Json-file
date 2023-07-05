using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using static Codice.CM.WorkspaceServer.DataStore.IncomingChanges.StoreIncomingChanges.FileConflicts;
using log4net.Util;
using UnityEngine.UI;
using System.Drawing.Printing;
using static Codice.CM.WorkspaceServer.WorkspaceTreeDataStore;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Specialized;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using PlasticPipe.PlasticProtocol.Messages;
using System.Linq;
using System.Linq.Expressions;
using static PlasticPipe.PlasticProtocol.Messages.Serialization.ItemHandlerMessagesSerialization;
using System.Globalization;
using UnityEditor.AnimatedValues;
using System.Security.Cryptography;
using UnityEngine.UIElements;
using Unity.VisualScripting;

namespace MyTools
{
    public class ProjectSetup_window : EditorWindow
    {
        #region Variables
        static ProjectSetup_window win;
        protected SerializedObject serializedObject;
        protected SerializedProperty currentProperty;

        private string dir = "";
        string nameAttribArr = "";
        string nameAttribObj = "";
        string nameAttrib = "";
        string valueSearch = "";
        string searchKey = "";
        string searchValueField = "";
        string searchKeyField = "";


        bool nameAttribBool;
        bool showAttribArr = true;
        bool writeData = false;
        bool reloadData = false;
        bool checkLoad = true;
        bool showData = false;
        bool foldout = true;

        Vector2 paletteScrollPos = new Vector2(0, 0);

        Dictionary<string, bool> showAttribObj = new Dictionary<string, bool>();

        private bool checkBtn = false;
        JObject data = null;
        JObject curData = new();
        JObject DataSearched = new();
        #endregion

        #region Main Methods

        public static void InitWindow()
        {
            win = EditorWindow.GetWindow<ProjectSetup_window>("Project Setup");
            win.Show();
        }

        void OnGUI()
        {

            dir = EditorGUILayout.TextField("Diretion: ", dir);

            EditorGUILayout.Space();

            if (GUILayout.Button("Find Data", GUILayout.Height(20), GUILayout.Width(100)))
            {
                checkBtn = true;

                try
                {
                    data = LoadJson(dir);
                    curData = new();
                    searchKeyField = "";
                    searchValueField = "";

                    foreach (JProperty property in data.Properties())
                    {
                        curData.Add(property.Name, property.Value);
                    }
                }
                catch (Exception e)
                {
                    this.LogError(e);
                }
            }

            paletteScrollPos = EditorGUILayout.BeginScrollView(paletteScrollPos, GUILayout.ExpandWidth(true));

            if (checkBtn && data != null)
            {
                EditorGUI.BeginChangeCheck();
                searchValueField = EditorGUILayout.TextField("Search By Value: ", searchValueField);
                searchKeyField = EditorGUILayout.TextField("Search By Key: ", searchKeyField);
                if (EditorGUI.EndChangeCheck())
                {
                    DataSearched = new();
                    showData = true;
                    valueSearch = searchValueField;
                    searchKey = searchKeyField;
                }

                if (valueSearch == "" && searchKey == "")
                {
                    showData = true;
                }

                if (showData)
                {
                    foreach (JProperty property in curData.Properties())
                    {
                        if (property.Value.Type.ToString() == "Object")
                        {
                            if (!showAttribObj.ContainsKey(property.Name))
                            {
                                showAttribObj.Add(property.Name, true);
                            }

                            nameAttribObj = property.Name;
                            showAttribObj[property.Name] = EditorGUILayout.Foldout(showAttribObj[property.Name], nameAttribObj);
                            if (showAttribObj[property.Name])
                            {
                                if (!Selection.activeTransform)
                                {
                                    JObject newProperty = (JObject)property.Value;
                                    JObject dataOrigin = (JObject)data[property.Name];

                                    foreach (JProperty child in newProperty.Properties())
                                    {
                                        EditorGUI.indentLevel++;
                                        ExportDeepDict(child.Name, child.Value, newProperty, dataOrigin, nameAttribObj);
                                        EditorGUI.indentLevel--;
                                    }
                                }
                                else if (Selection.activeTransform)
                                {
                                    nameAttribObj = property.Name;
                                    showAttribObj[property.Name] = false;
                                }
                            }
                        }
                        else if (property.Value.Type.ToString() == "Array")
                        {
                            JArray childData = (JArray)property.Value;
                            JArray dataOrigin = (JArray)data[property.Name];
                            nameAttribArr = property.Name;
                            showAttribArr = EditorGUILayout.Foldout(showAttribArr, nameAttribArr);
                            if (showAttribArr)
                            {
                                if (!Selection.activeTransform)
                                {
                                    for (int i = 0; i < childData.Count; i++)
                                    {
                                        ExportDeepList(childData[i], childData, dataOrigin[i], nameAttribArr, i);
                                    }
                                }

                                if (Selection.activeTransform)
                                {
                                    nameAttribArr = property.Name;
                                    showAttribArr = false;
                                }
                            }
                        }

                        else if (property.Value.Type.ToString() == "Boolean")
                        {
                            if (showData)
                            {
                                nameAttribBool = (bool)property.Value;
                                nameAttrib = property.Name;
                                nameAttribBool = EditorGUILayout.Toggle(nameAttrib + CheckValueChange(data[property.Name].ToString() == curData[property.Name].ToString()) + ":", nameAttribBool);
                                curData[property.Name] = nameAttribBool;
                            }
                        }

                        else
                        {

                            EditorGUI.BeginChangeCheck();
                            GUILayout.BeginHorizontal();
                            nameAttrib = property.Name;
                            nameAttrib = EditorGUILayout.TextField(nameAttrib + CheckValueChange(data[property.Name].ToString() == curData[property.Name].ToString()) + ":", property.Value.Type.ToString() == "Float" ? ((float)property.Value).ToString("0.0##") : property.Value.ToString(), GUILayout.Width(200));

                            GUILayout.EndHorizontal();

                            if (EditorGUI.EndChangeCheck())
                            {
                                ValidateData(curData[property.Name].Type.ToString(), nameAttrib, null, curData, 0, property.Name);
                            }
                        }
                    }
                }
                else
                {
                    foreach (JProperty property in DataSearched.Properties())
                    {
                        nameAttrib = property.Name;
                        if (property.Value.Type.ToString() == "Object")
                        {
                            JObject newObject = (JObject)property.Value;
                            foldout = EditorGUILayout.Foldout(foldout, nameAttrib);

                            if (foldout)
                            {
                                foreach (JProperty child in newObject.Properties())
                                {
                                    if (child.Value.Type.ToString() == "Object")
                                    {
                                        continue;
                                    }
                                    else if (child.Value.Type.ToString() == "Array")
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        EditorGUI.indentLevel++;
                                        nameAttrib = child.Name;
                                        nameAttrib = EditorGUILayout.TextField(nameAttrib, child.Value.Type.ToString() == "Float" ? ((float)child.Value).ToString("0.0##") : child.Value.ToString());
                                        EditorGUI.indentLevel--;
                                    }
                                }
                            }
                        }

                        else if (property.Value.Type.ToString() == "Array")
                        {
                            JArray newArrayValue = (JArray)property.Value;
                            foldout = EditorGUILayout.Foldout(foldout, nameAttrib);

                            if (foldout)
                            {
                                for (int i = 0; i < newArrayValue.Count; i++)
                                {
                                    EditorGUILayout.BeginVertical("box");
                                    if (newArrayValue[i].Type.ToString() == "Object")
                                    {
                                        JObject newObject = (JObject)newArrayValue[i];
                                        foreach (JProperty child in newObject.Properties())
                                        {
                                            EditorGUI.indentLevel++;
                                            nameAttrib = child.Name;
                                            nameAttrib = EditorGUILayout.TextField(nameAttrib, child.Value.Type.ToString() == "Float" ? ((float)child.Value).ToString("0.0##") : child.Value.ToString());
                                            EditorGUI.indentLevel--;
                                        }
                                    } else
                                    {
                                        EditorGUI.indentLevel++;
                                        nameAttrib = EditorGUILayout.TextField(newArrayValue[i].Type.ToString() == "Float" ? ((float)newArrayValue[i]).ToString("0.0##") : newArrayValue[i].ToString());
                                        EditorGUI.indentLevel--;
                                    }
                                    EditorGUILayout.EndVertical();
                                }
                            }
                        }

                    }
                }
            }

            EditorGUILayout.EndScrollView();

            if (checkBtn && data != null)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Data", GUILayout.Height(30), GUILayout.Width(200)))
                {
                    writeData = true;
                }

                if (GUILayout.Button("Reload Data", GUILayout.Height(30), GUILayout.Width(200)))
                {
                    reloadData = true;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (writeData)
            {
                if (checkLoad)
                {
                    WriteToJson(curData, dir);
                    data = LoadJson(dir);
                    writeData = false;
                }
                else
                {
                    this.LogError("Write Data Failed");
                    writeData = false;
                }

            }
            if (reloadData)
            {
                data = LoadJson(dir);
                curData = new();
                foreach (JProperty property in data.Properties())
                {
                    curData.Add(property.Name, property.Value);
                }
                reloadData = false;
            }
        }
        #endregion

        public JObject LoadJson(string filePath)
        {
            //JsonDictionaryAttribute jsonDictionaryAttribute = new JsonDictionaryAttribute();
            JObject data = JObject.Parse(File.ReadAllText(filePath));
            return data;
        }
        public void WriteToJson(JObject data, string pathFile)
        {
            using (StreamWriter file = File.CreateText(@pathFile))
            {
                JsonSerializer serializer = new JsonSerializer();
                //serialize object directly into file stream
                serializer.Serialize(file, data);
            }
        }

        public void ExportDeepDict(string dataName, JToken dataObject, JObject parentDict, JObject dataOrigin, string parentKey = "", JArray parentArray = null, int index = 0)
        {

            if (dataObject.Type.ToString() == "Object")
            {
                if (!showAttribObj.ContainsKey(dataName))
                {
                    showAttribObj.Add(dataName, true);
                }
                JObject dataCallBack = (JObject)dataObject;
                JObject newDataOrigin = (JObject)dataOrigin[dataName];
                nameAttribObj = dataName;
                showAttribObj[dataName] = EditorGUILayout.Foldout(showAttribObj[dataName], nameAttribObj);
                if (showAttribObj[dataName])
                {
                    if (!Selection.activeTransform)
                    {
                        foreach (JProperty child in dataCallBack.Properties())
                        {
                            EditorGUI.indentLevel++;
                            ExportDeepDict(child.Name, child.Value, dataCallBack, newDataOrigin, nameAttribObj);
                            EditorGUI.indentLevel--;
                        }
                    }
                    else if (Selection.activeTransform)
                    {
                        nameAttribObj = dataName;
                        showAttribObj[dataName] = false;
                    }
                }
            }
            else if (dataObject.Type.ToString() == "Array")
            {
                if (!showAttribObj.ContainsKey(dataName))
                {
                    showAttribObj.Add(dataName, true);
                }

                JArray childData = (JArray)dataObject;
                JArray newDataOriginArray = (JArray)dataOrigin[dataName];

                nameAttribObj = dataName;
                showAttribObj[dataName] = EditorGUILayout.Foldout(showAttribObj[dataName], nameAttribObj);
                if (showAttribObj[dataName])
                {
                    if (!Selection.activeTransform)
                    {
                        EditorGUILayout.BeginVertical("box");
                        for (int i = 0; i < childData.Count; i++)
                        {
                            EditorGUI.indentLevel++;
                            ExportDeepList(childData[i], childData, newDataOriginArray[i], dataName, i);
                            EditorGUI.indentLevel--;
                        }
                        EditorGUILayout.EndVertical();
                    }
                    else if (Selection.activeTransform)
                    {
                        nameAttribObj = dataName;
                        showAttribObj[dataName] = false;
                    }
                }
            }

            else if (dataObject.Type.ToString() == "Boolean")
            {
                nameAttribBool = (bool)dataObject;
                EditorGUI.indentLevel++;
                nameAttribBool = EditorGUILayout.Toggle($"{dataName}" + CheckValueChange(dataOrigin[dataName].ToString() == parentDict[dataName].ToString()), nameAttribBool);
                EditorGUI.indentLevel--;
                parentDict[dataName] = nameAttribBool;
            }
            else
            {
                if (valueSearch == "" && searchKey == "")
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.indentLevel++;
                    nameAttribObj = EditorGUILayout.TextField($"{dataName}" + CheckValueChange(dataOrigin[dataName].ToString() == parentDict[dataName].ToString()), dataObject.Type.ToString() == "Float" ? ((float)dataObject).ToString("0.0##") : dataObject.ToString());
                    if (EditorGUI.EndChangeCheck())
                    {
                        ValidateData(parentDict[dataName].Type.ToString(), nameAttribObj, null, parentDict, 0, dataName);
                    }
                    EditorGUI.indentLevel--;
                }
                else if (valueSearch.ToLower() == dataObject.ToString().ToLower() || searchKey.ToLower() == dataName.ToLower())
                {
                    
                    if (parentArray == null)
                    {
                        DataSearched.Add(parentKey, dataOrigin);
                    }
                    else
                    {
                        if (!DataSearched.ContainsKey(parentKey))
                        {
                            DataSearched.Add(parentKey, parentArray);
                        }
                    }
                    
                    showData = false;
                }
            }
        }

        public void ExportDeepList(JToken data, JArray parentData, JToken dataOrigin, string parentName, int index)
        {
            if (data.Type.ToString() == "Object")
            {
                JObject dataCallBack = (JObject)data;
                JObject newDataOrigin = (JObject)dataOrigin;
                EditorGUILayout.BeginVertical("box");
                foreach (JProperty child in dataCallBack.Properties())
                {
                    EditorGUI.indentLevel++;
                    ExportDeepDict(child.Name, child.Value, dataCallBack, newDataOrigin, parentName, parentData, index);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }

            else if (data.Type.ToString() == "Boolean")
            {
                nameAttribBool = (bool)data;
                EditorGUI.indentLevel++;
                nameAttribBool = EditorGUILayout.Toggle(CheckValueChange(dataOrigin.ToString() == parentData[index].ToString()), nameAttribBool);
                EditorGUI.indentLevel--;
                parentData[index] = nameAttribBool;
            }

            else
            {
                if (valueSearch == "" && searchKey == "")
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.indentLevel++;
                    nameAttribArr = EditorGUILayout.TextField(CheckValueChange(dataOrigin.ToString() == parentData[index].ToString()), data.Type.ToString() == "Float" ? ((float)data).ToString("0.0##") : data.ToString());
                    if (EditorGUI.EndChangeCheck())
                    {
                        ValidateData(parentData[index].Type.ToString(), nameAttribArr, parentData, null, index, "");
                    }
                    EditorGUI.indentLevel--;
                }

                else if (valueSearch.ToLower() == data.ToString().ToLower() || searchKey.ToLower() == parentName.ToString().ToLower())
                {
                    DataSearched.Add(parentName, parentData);
                    showData = false;
                }
            }
        }

        public string CheckValueChange(bool change)
        {
            if (!change)
            {
                return "*";
            }
            else
            {
                return " ";
            }
        }

        public void ValidateData(string dataType, string inputData, JArray parentList = null, JObject parentDict = null, int index = 0, string dataName = "")
        {
            if (parentDict != null)
            {
                if (dataType == "Integer")
                {
                    try
                    {
                        checkLoad = true;
                        parentDict[dataName] = inputData.Contains(".") ? (int)float.Parse(inputData, CultureInfo.InvariantCulture.NumberFormat) : Int32.Parse(inputData);
                    }
                    catch (FormatException e)
                    {
                        checkLoad = false;
                        this.LogError(e);
                    }
                }
                else if (dataType == "Float")
                {
                    try
                    {
                        checkLoad = true;
                        parentDict[dataName] = Math.Round(float.Parse(inputData, CultureInfo.InvariantCulture.NumberFormat), 3);
                    }
                    catch (FormatException e)
                    {
                        checkLoad = false;
                        this.LogError(e);
                    }
                }
                else
                {
                    parentDict[dataName] = inputData;
                }
            }
            else
            {
                if (dataType == "Integer")
                {
                    try
                    {
                        checkLoad = true;
                        parentList[index] = inputData.Contains(".") ? (int)float.Parse(inputData, CultureInfo.InvariantCulture.NumberFormat) : Int32.Parse(inputData);
                    }
                    catch (FormatException e)
                    {
                        checkLoad = false;
                        this.LogError(e);
                    }
                }
                else if (dataType == "Float")
                {
                    try
                    {
                        checkLoad = true;
                        parentList[index] = Math.Round(float.Parse(inputData, CultureInfo.InvariantCulture.NumberFormat), 3);

                    }
                    catch (FormatException e)
                    {
                        checkLoad = false;
                        this.LogError(e);
                    }
                }
                else
                {
                    parentList[index] = inputData;
                }
            }
        }
    }
}