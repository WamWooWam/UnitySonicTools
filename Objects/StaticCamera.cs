using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class StaticCamera : MonoBehaviour
{
    public static Camera camera;

    public void Awake()
    {
        camera = this.GetComponent<Camera>();
    }
}
