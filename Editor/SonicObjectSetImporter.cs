#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using BezierSolution;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SonicObjectSetImporter : EditorWindow
{
    [MenuItem("Sonic/Import Object Set XML")]
    static void ImportPath()
    {
        var path = EditorUtility.OpenFilePanel("Open an SetObject file", null, "set.xml");
        if (path == null)
            return;

        ImportFromFile(path);

        EditorUtility.ClearProgressBar();
    }

    public static void ImportFromFile(string path, GameObject baseObject = null)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(path);

        var objects = xmlDoc["SetObject"].ChildNodes
            .Cast<XmlNode>()
            .OrderByDescending(n => n.GetProperty<int>("SetObjectID"))
            .ToArray();

        var importedObjects = new Dictionary<int, GameObject>();

        XmlNode item = null;
        GameObject gameObject = null;

        var parent = new GameObject(Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path)));
        parent.transform.position = Vector3.zero;
        parent.transform.rotation = Quaternion.identity;

        try
        {
            for (var i = 0; i < objects.Length; i++)
            {
                item = objects[i];
                EditorUtility.DisplayProgressBar("Processing stage objects", $"{i + 1}/{objects.Length} ({item.Name})", (float)i / objects.Length);
                gameObject = ImportTools.FindAndCacheObject(item.Name);

                if (gameObject != null)
                {
                    var tag = int.Parse(item["SetObjectID"].InnerText);
                    var currentObjects = FindObjectsOfType<BaseObjectBehaviour>();
                    var obj = currentObjects.FirstOrDefault(imp => imp.setObjectId == tag)?.gameObject;

                    if (obj == null)
                    {
                        var set = item.SelectSingleNode("MultiSetParam");
                        if (set != null)
                        {
                            obj = new GameObject(item.Name + "Set");
                            obj.transform.parent = parent.transform;

                            var interval = float.Parse(set["Interval"].InnerText);
                            var count = int.Parse(set["Count"].InnerText);
                            var direction = int.Parse(set["Direction"].InnerText);

                            var basePosition = ImportTools.ImportVector(item["Position"]);
                            var baseRotation = ImportTools.ImportRotation(item["Rotation"]);
                            var baseTranslate = (baseRotation * DirectionToVector(direction) * interval);

                            obj.transform.position = basePosition;
                            obj.transform.rotation = baseRotation;

                            var setElements = set.SelectNodes("Element").OfType<XmlNode>().ToArray();
                            for (var j = 0; j < count; j++)
                            {
                                var element = setElements.ElementAtOrDefault(j);
                                Vector3 position;
                                Quaternion rotation;

                                if (element != null)
                                {
                                    position = ImportTools.ImportVector(element["Position"]);
                                    rotation = ImportTools.ImportRotation(element["Rotation"]);
                                }
                                else
                                {
                                    position = basePosition + (baseTranslate * (setElements.Length - j));
                                    rotation = baseRotation;
                                }

                                var newObj = (GameObject)PrefabUtility.InstantiatePrefab(gameObject, obj.transform);
                                newObj.transform.position = position;
                                newObj.transform.rotation = rotation;

                                var baseObj = newObj.GetComponent<BaseObjectBehaviour>();
                                if (baseObj != null)
                                {
                                    baseObj.ReadXml(xmlDoc, item);
                                }
                            }

                        }
                        else
                        {
                            obj = (GameObject)PrefabUtility.InstantiatePrefab(gameObject, parent.transform);
                            obj.transform.position = ImportTools.ImportVector(item["Position"]);
                            obj.transform.rotation = ImportTools.ImportRotation(item["Rotation"]);

                            var baseObj = obj.GetComponent<BaseObjectBehaviour>();
                            if (baseObj != null)
                            {
                                baseObj.ReadXml(xmlDoc, item);
                            }
                        }

                        // is this even used?
                        // obj.transform.position += (float.Parse(item["GroundOffset"].InnerText) * Vector3.up);
                        // no, no it isn't.
                    }

                    if (baseObject != null)
                        obj.transform.parent = baseObject.transform;
                    importedObjects.Add(tag, obj);
                }
            }

            for (int i = 0; i < importedObjects.Count; i++)
            {
                var imported = importedObjects.ElementAt(i);
                EditorUtility.DisplayProgressBar("Wiring things up!", $"{i + 1}/{importedObjects.Count} ({imported.Value.name}, {imported.Key})", (float)i / importedObjects.Count);

                var behaviours = imported.Value.GetComponents<BaseObjectBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    behaviour.Postprocess(importedObjects);
                }

            }


            EditorSceneManager.SaveOpenScenes();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to import {Path.GetFileName(path)}!");
            Debug.LogException(ex);
            if (item != null)
            {
                Debug.LogError($"An error occured when importing {item["SetObjectID"].InnerText} of type {item.Name}.");
            }
        }
    }

    private static Vector3 DirectionToVector(int direction)
    {
        var ret = Vector3.zero;

        switch (direction)
        {
            case 0:
                ret = Vector3.forward;
                break;
            case 1:
                ret = Vector3.right;
                break;
            case 2:
                ret = Vector3.up;
                break;
            case 3:
            default:
                break;
        }

        return ret;
    }
}

#endif