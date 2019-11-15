using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

public abstract class BaseObjectBehaviour : MonoBehaviour
{
    public int setObjectId = 0;
    public bool isHomingTarget = true;
    public float range = 100;

    public virtual void OnTriggerEnter(Collider other)
    {
        var behaviour = other.gameObject.GetComponent<ICharacterBehaviour>();
        if (behaviour != null)
        {
            OnActivated(behaviour);
        }
    }

    public virtual void OnTriggerExit(Collider other)
    {
        var behaviour = other.gameObject.GetComponent<ICharacterBehaviour>();
        if (behaviour != null)
        {
            OnDeactivated(behaviour);
        }
    }

    public virtual void ReadXml(XmlDocument document, XmlNode node)
    {
        isHomingTarget = node.GetProperty("IsHomingAttackEnable", false);
        range = node.GetProperty("Range", 100f);
        setObjectId = node.GetProperty("SetObjectID", 0);
    }

    public virtual void Postprocess(IReadOnlyDictionary<int, GameObject> importedObjects)
    {

    }

    public abstract void OnActivated(ICharacterBehaviour character);
    public virtual void OnDeactivated(ICharacterBehaviour character) { }
}
