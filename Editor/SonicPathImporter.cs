#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using BezierSolution;
using UnityEditor;
using UnityEngine;

public class SonicPathImporter : EditorWindow
{
    [MenuItem("Sonic/Import SonicPath XML")]
    static void ImportPath()
    {
        var path = EditorUtility.OpenFilePanel("Open a .path.xml file", null, "path.xml");
        if (path == null)
            return;

        ImportFromFile(path);

        EditorUtility.ClearProgressBar();
    }

    private static void ImportFromFile(string path)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(path);

        var scene = xmlDoc.DocumentElement["scene"].SelectNodes("//node").Cast<XmlNode>().ToDictionary(k => k.Attributes["id"].InnerText, v => v);

        var geometry = xmlDoc.DocumentElement["library"].SelectNodes("geometry").Cast<XmlNode>().ToArray();
        for (var j = 0; j < geometry.Length; j++)
        {
            var item = geometry[j];
            var id = item.Attributes["id"].InnerText;
            var nodeId = id.Substring(0, id.Length - 9);
            var splines = new List<BezierSpline>();
            var container = new GameObject(nodeId);

            EditorUtility.DisplayProgressBar("Creating Splines", nodeId, (float)j / geometry.Length);
            foreach (var spline in item.SelectNodes("spline").Cast<XmlNode>())
            {
                var rawSplines = spline.SelectNodes("spline3d").Cast<XmlNode>().ToArray();
                for (var s = 0; s < rawSplines.Length; s++)
                {
                    var spline3d = rawSplines[s];
                    var obj = new GameObject($"{nodeId}_{s}", typeof(BezierSpline));
                    obj.transform.parent = container.transform;
                    var unitySpline = obj.GetComponent<BezierSpline>();

                    var points = spline3d.SelectNodes("knot").Cast<XmlNode>().Reverse();
                    // float angle = 0;
                    foreach (var knot in points)
                    {
                        var point = unitySpline.InsertNewPointAt(0);
                        point.handleMode = BezierPoint.HandleMode.Free;
                        var invec = ImportTools.ImportVectorString(knot["invec"].InnerText);
                        var outvec = ImportTools.ImportVectorString(knot["outvec"].InnerText);

                        // var axis = Vector3.Cross(invec.normalized, outvec.normalized).normalized;
                        // var angle = Vector3.SignedAngle(invec.normalized, outvec.normalized, axis);
                        // Debug.Log($"{axis} {angle}");
                        // point.transform.rotation = Quaternion.AngleAxis(angle, axis);

                        point.transform.position = ImportTools.ImportVectorString(knot["point"].InnerText);
                        point.precedingControlPointPosition = invec;
                        point.followingControlPointPosition = outvec;
                    }

                    unitySpline.RemovePointAt(points.Count());
                    unitySpline.RemovePointAt(points.Count());

                    splines.Add(unitySpline);
                }

                if (scene.TryGetValue(nodeId, out var node))
                {
                    container.transform.localScale = ImportTools.RawImportVectorString(node["scale"].InnerText);
                    container.transform.position = ImportTools.ImportVectorString(node["translate"].InnerText);
                    container.transform.rotation = ImportTools.ImportRotationString(node["rotate"].InnerText);

                    //var group = container.AddComponent<SplineGroup>();
                    //if (splines.Count == 2)
                    //{
                    //    group.GenerateGeometry(splines);
                    //}
                }
                else
                {
                    Debug.LogError($"No node found for {nodeId}!!");
                }

            }

        }
    }
}

#endif