using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Structs;
using UnityEditor;
using UnityEngine;
using WamWooWam.StructReader;

public class SonicInstancerImporter : EditorWindow
{
    private class Billboard
    {
        public Mesh mesh;
        public Material material;
    }

    [MenuItem("Sonic/Import Instancer Data")]
    static void ImportPath()
    {
        var path = EditorUtility.OpenFilePanel("Open a .stg.xml file", null, "stg.xml");
        if (path == null)
            return;

        var directory = EditorUtility.OpenFolderPanel("Stage data directory", null, null);

        // read the stage xml
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(path);

        foreach (var item in xmlDoc.DocumentElement.SelectNodes("//Instancer").Cast<XmlNode>())
        {
            var name = item["Name"].InnerText;
            var container = new GameObject(name);

            var resources = new List<Material>();
            foreach (var resource in item.SelectNodes("//Resource").Cast<XmlNode>())
            {
                var matchingAssets = AssetDatabase.FindAssets(resource["Texture0"].InnerText);
                if (!matchingAssets.Any())
                {
                    EditorUtility.DisplayDialog("Unable to find asset", $"Couldn't find required asset with name {resource["Texture0"].InnerText}!", "OK");
                    DestroyImmediate(container);
                    return;
                }

                var assetPath = AssetDatabase.GUIDToAssetPath(matchingAssets[0]);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                var material = new Material(Shader.Find("Particles/Standard Surface"));
                material.SetTexture("_MainTex", texture);
                material.name = resource["Texture0"].InnerText;
                resources.Add(material);
            }

            var assets = new List<Billboard>();
            foreach (var type in item.SelectNodes("//InstanceType").Cast<XmlNode>())
            {
                var scale = ImportTools.RawImportVector(type["Scale"]);
                var coords = ImportTools.RawImportVector4(type["TexTrimCoords"]);
                coords.y = 1 - coords.y;
                coords.w = 1 - coords.w;
                var mesh = new Mesh
                {
                    vertices = new Vector3[4] { new Vector3(0, 0, 0), new Vector3(scale.x, 0, 0), new Vector3(0, scale.y, 0), new Vector3(scale.x, scale.y, 0) },
                    triangles = new int[6] { 0, 2, 1, 2, 3, 1 }.Reverse().ToArray(),
                    uv = new Vector2[4] {
                        new Vector2(coords.x, coords.y),
                        new Vector2(coords.z, coords.y),
                        new Vector2(coords.x, coords.w),
                        new Vector2(coords.z, coords.w)
                    }

                };

                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();

                var asset = new Billboard();
                asset.material = resources[0];
                asset.mesh = mesh;
                assets.Add(asset);
            }

            var data = File.ReadAllBytes(Path.Combine(directory, name + ".mti"));
            var file = new MtiFile();
            DataLoader.Load(ref file, data);

            foreach (var instance in file.instances)
            {
                var obj = new GameObject("Instance", typeof(MeshFilter), typeof(MeshRenderer));
                var filter = obj.GetComponent<MeshFilter>();
                var render = obj.GetComponent<MeshRenderer>();
                var board = assets[instance.type];

                filter.sharedMesh = board.mesh;
                render.sharedMaterial = board.material;
                obj.transform.position = ImportTools.ImportVectorString($"{instance.x} {instance.y} {instance.z}");
                obj.transform.parent = container.transform;

            }

        }
    }
}

namespace Structs
{

    struct MtiHeader
    {
        public uint magic; // "MTI "
        public uint unk_1; // generally 1
        public uint instance_count;
        public uint unk_2;
        public uint unk_3; // generally 0
        public uint unk_4; // generally 0
        public uint unk_5; // generally 0
        public uint data_offset; // generally 32
    }

    struct MtiInstance
    {
        public float x;
        public float y;
        public float z;
        public byte type;

        public byte unk_1; // probably flags and additional type data
        public byte unk_2; // ^^
        public byte unk_3; // ^^
        public uint unk_4; // ^^

        public uint separator; // always 0xFFFFFFFF
    }

    [Endianness(Endianness.Big)]
    struct MtiFile
    {
        public MtiHeader header;

        [OffsetRef("header.data_offset")]
        [ArraySizeRef("header.instance_count")]
        public MtiInstance[] instances;
    }
}