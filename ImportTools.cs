using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEditor;
using UnityEngine;

public static class ImportTools
{
    public static Dictionary<string, GameObject> AssetCache { get; set; } = new Dictionary<string, GameObject>();

    public static Vector3 ImportVector(XmlNode node)
    {
        var x = -float.Parse(node["x"].InnerText);
        var y = float.Parse(node["y"].InnerText);
        var z = float.Parse(node["z"].InnerText);

        return new Vector3(x, y, z);
    }

    public static Quaternion ImportRotation(XmlNode node)
    {
        var rotation = RawImportRotation(node).eulerAngles;
        return Quaternion.Euler(rotation.x, -rotation.y, -rotation.z);
    }

    public static Vector3 RawImportVector(XmlNode node)
    {
        var x = float.Parse(node["x"].InnerText);
        var y = float.Parse(node["y"].InnerText);
        var z = float.Parse(node["z"].InnerText);

        return new Vector3(x, y, z);
    }

    public static Vector4 RawImportVector4(XmlNode node)
    {
        var x = float.Parse(node["x"].InnerText);
        var y = float.Parse(node["y"].InnerText);
        var z = float.Parse(node["z"].InnerText);
        var w = float.Parse(node["w"].InnerText);

        return new Vector4(x, y, z, w);
    }

    public static Quaternion RawImportRotation(XmlNode node)
    {
        var w = float.Parse(node["w"].InnerText);
        var x = float.Parse(node["x"].InnerText);
        var y = float.Parse(node["y"].InnerText);
        var z = float.Parse(node["z"].InnerText);

        return new Quaternion(x, y, z, w);
    }

    public static Vector3 ImportVectorString(string innerText)
    {
        var points = innerText.Split(' ').Select(s => float.Parse(s)).ToArray();

        var x = points[0];
        var y = points[1];
        var z = points[2];

        return new Vector3(-x, y, z);
    }

    public static Vector3 RawImportVectorString(string innerText)
    {
        var points = innerText.Split(' ').Select(s => float.Parse(s)).ToArray();

        var x = points[0];
        var y = points[1];
        var z = points[2];

        return new Vector3(x, y, z);
    }

    public static Quaternion ImportRotationString(string innerText)
    {
        var points = innerText.Split(' ').Select(s => float.Parse(s)).ToArray();

        var x = points[0];
        var y = points[1];
        var z = points[2];
        var w = points[3];

        var rotation = new Quaternion(x, y, z, w).eulerAngles;
        return Quaternion.Euler(rotation.x, -rotation.y, -rotation.z);
    }

    public static Quaternion ExtractRotation(this Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }

    public static Vector3 ExtractPosition(this Matrix4x4 matrix)
    {
        Vector3 position;
        position.x = matrix.m03;
        position.y = matrix.m13;
        position.z = matrix.m23;
        return position;
    }

    public static Vector3 ExtractScale(this Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }

    public static T GetProperty<T>(this XmlNode node, string xpath, T def = default)
    {
        var x = node.SelectSingleNode(xpath);
        if (x == null)
            return def;

        var method = typeof(T).GetMethod("Parse", new[] { typeof(string) });
        if (method == null)
            throw new Exception($"{typeof(T)} is not parseable");

        return (T)method.Invoke(null, new[] { x.InnerText });
    }

#if UNITY_EDITOR
    public static GameObject FindAndCacheObject(string name)
    {
        name = name.ToLowerInvariant();
        if (AssetCache.TryGetValue(name, out var obj))
        {
            return obj;
        }
        else
        {
            var assets = AssetDatabase.FindAssets($"{name} t:Prefab");
            var potentialObjects = new List<GameObject>();
            foreach (var asset in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(asset);
                obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                potentialObjects.Add(obj);

                Debug.Log($"Potential: {obj.name} for {name} ({Math.Abs(string.CompareOrdinal(obj.name.ToLowerInvariant(), name.ToLowerInvariant()))})");
            }

            var found = potentialObjects.OrderBy(s => Math.Abs(string.CompareOrdinal(s.name.ToLowerInvariant(), name.ToLowerInvariant()))).FirstOrDefault();
            if (found != null && string.Compare(found.name, name, true) == 0)
            {
                Debug.Log($"Found {found.name} for {name}");
                AssetCache[name] = found;
                return found;
            }

            return null;
        }
    }
#endif
}