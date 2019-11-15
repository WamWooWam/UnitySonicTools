using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SonicTools.Instancer
{
    [ExecuteInEditMode]
    public class InstancerBehaviour : MonoBehaviour
    {
        public Material material;
        public Mesh[] meshes;
        public InstancerData[] instances;
        public float cullRadius = 100f;

        private Matrix4x4[] _drawInstanceMatrices;
        private Matrix4x4[] _instanceMatrices;
        private int[][] _instanceGroups;
        private int[] _instanceGroupSizes;
        private Vector3 _pos;
        private Quaternion _rot;
        private Vector3 _forward;
        private Camera _camera;

        private void Awake()
        {

        }

        private void LateUpdate()
        {
            if (_instanceGroups == null)
            {
                instances = instances.OrderByDescending(i => Vector3.Distance(Vector3.zero, i.position)).ToArray();
                _drawInstanceMatrices = new Matrix4x4[1023]; // maximum number of instances
                _instanceMatrices = new Matrix4x4[instances.Length];
                _instanceGroupSizes = new int[meshes.Length];
                _instanceGroups = new int[meshes.Length][];
                for (var i = 0; i < _instanceGroups.Length; i++)
                {
                    _instanceGroups[i] = new int[instances.Length];
                }
            }

            for (int i = 0; i < _instanceGroupSizes.Length; i++)
            {
                _instanceGroupSizes[i] = 0;
            }

            var camera = CameraFacingBillboard.camera;
            if (camera == null)
            {
                camera = Camera.current;
            }

            if (camera != null)
            {
                _pos = camera.transform.position;
                _rot = camera.transform.rotation;
                _forward = camera.transform.forward;
            }

            // if at all possible, nothing within these loops should ever allocate.
            Parallel.For(0, instances.Length, SortInstance);

            for (var g = 0; g < _instanceGroups.Length; g++)
            {
                var size = _instanceGroupSizes[g];

                if (size == 0)
                    continue;

                var instanceGroup = _instanceGroups[g];
                var offset = 0;
                while (offset < size)
                {
                    var length = 1023 > (size - offset) ? size - offset : 1023;
                    for (var i = 0; i < length; i++)
                    {
                        _drawInstanceMatrices[i] = _instanceMatrices[instanceGroup[offset + i]];
                    }

                    offset += length;

                    Graphics.DrawMeshInstanced(meshes[g], 0, material, _drawInstanceMatrices, length, null, ShadowCastingMode.On, true, 9, null, LightProbeUsage.BlendProbes, null);
                }
            }
        }

        private void SortInstance(int i)
        {
            var instance = instances[i];
            if (SphereCheck(ref instance.position, _pos + (_forward * (cullRadius * 0.95f)), ref cullRadius))
            {
                var look = instance.position - _pos;
                look.y = 0;

                _instanceMatrices[i] = Matrix4x4.TRS(instance.position, Quaternion.LookRotation(look), Vector3.one);
                _instanceGroups[instance.type][Interlocked.Increment(ref _instanceGroupSizes[instance.type])] = i;
            }
        }

        public static bool SphereCheck(ref Vector3 pos, Vector3 center, ref float radius)
        {
            if (radius < 0)
                return true;

            var x1 = (pos.x - center.x) * (pos.x - center.x);
            var y1 = (pos.y - center.y) * (pos.y - center.y);
            var z1 = (pos.z - center.z) * (pos.z - center.z);
            return (x1 + y1 + z1) <= (radius * radius);
        }
    }
}
