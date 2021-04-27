using UnityEngine;
using System.Collections.Generic;



namespace ModPack
{
    static public class Extensions_UnityObjects_Component
    {
        //------------------------------------------------------------------------------------------------------------------------------- Hierarchy

        /// <summary> Attaches a to this object. </summary>
        static public void BecomeParentOf(this Component t, GameObject a, bool retainWorldTransform = false)
        => t.gameObject.BecomeParentOf(a, retainWorldTransform);
        /// <summary> Attaches a to this object. </summary>
        static public void BecomeParentOf(this Component t, Component a, bool retainWorldTransform = false)
        => t.gameObject.BecomeParentOf(a.gameObject, retainWorldTransform);
        /// <summary> Attaches this object to a's parent. </summary>
        static public void BecomeSiblingOf(this Component t, GameObject a, bool retainWorldTransform = false)
        => t.gameObject.BecomeSiblingOf(a, retainWorldTransform);
        /// <summary> Attaches this object to a's parent. </summary>
        static public void BecomeSiblingOf(this Component t, Component a, bool retainWorldTransform = false)
        => t.gameObject.BecomeSiblingOf(a.gameObject, retainWorldTransform);
        /// <summary> Attaches this object to a. </summary>
        static public void BecomeChildOf(this Component t, GameObject a, bool retainWorldTransform = false)
        => t.gameObject.BecomeChildOf(a, retainWorldTransform);
        /// <summary> Attaches this object to a. </summary>
        static public void BecomeChildOf(this Component t, Component a, bool retainWorldTransform = false)
        => t.gameObject.BecomeChildOf(a.gameObject, retainWorldTransform);
        /// <summary> Places this object at the root of the hierarchy. </summary>
        static public void Unparent(this Component t, bool retainWorldTransform = false)
        => t.gameObject.Unparent(retainWorldTransform);

        /// <summary> Checks whether this object is right above a in the heirarchy. </summary>
        static public bool IsParentOf(this Component t, GameObject a)
        => t.gameObject.IsParentOf(a);
        /// <summary> Checks whether this object is right above a in the heirarchy. </summary>
        static public bool IsParentOf(this Component t, Component a)
        => t.gameObject.IsParentOf(a.gameObject);
        /// <summary> Checks whether this object has the same parent as a. </summary>
        static public bool IsSiblingOf(this Component t, GameObject a)
        => t.gameObject.IsSiblingOf(a);
        /// <summary> Checks whether this object has the same parent as a. </summary>
        static public bool IsSiblingOf(this Component t, Component a)
        => t.gameObject.IsSiblingOf(a.gameObject);
        /// <summary> Checks whether this object is right below a in the hierarchy. </summary>
        static public bool IsChildOf(this Component t, GameObject a)
        => t.gameObject.IsChildOf(a);
        /// <summary> Checks whether this object is right below a in the hierarchy. </summary>
        static public bool IsChildOf(this Component t, Component a)
        => t.gameObject.IsChildOf(a.gameObject);
        /// <summary> Checks whether this object is at the topmost level in the hierarchy. </summary>
        static public bool IsAtRoot(this Component t)
        => t.gameObject.IsAtRoot();
        /// <summary> Checks whether this object has a parent. </summary>
        static public bool HasParent(this Component t)
        => t.gameObject.HasParent();
        /// <summary> Checks whether this object has a child. </summary>
        static public bool HasChild(this Component t)
        => t.gameObject.HasChild();
        /// <summary> Returns this object's child with name a. If it doesn't exist, returns null.</summary>
        static public GameObject FindChild(this Component t, string a)
        => t.gameObject.FindChild(a);
        /// <summary> Returns component T attached to this object's child with name a. If it doesn't exist, returns null.</summary>
        static public T FindChild<T>(this Component t, string a) where T : Component
        => t.gameObject.FindChild<T>(a);

        /// <summary> Returns this object's parent. </summary>
        static public GameObject GetParent(this Component t)
        => t.gameObject.GetParent();
        /// <summary> Returns an array of this object's children. </summary>
        static public GameObject[] GetChildren(this Component t)
        => t.gameObject.GetChildren();
        /// <summary> Returns all of this object's ancestors. </summary>
        static public List<GameObject> GetAncestors(this Component t)
        => t.gameObject.GetAncestors();

        /// <summary> Checks whether this object has a component T. </summary>
        static public bool HasComponent<T>(this Component t) where T : Component
        => t.gameObject.HasComponent<T>();
        /// <summary> Checks whether this object's parent has a component T. </summary>
        static public bool ParentHasComponent<T>(this Component t) where T : Component
        => t.gameObject.ParentHasComponent<T>();
        /// <summary> Checks whether any of this object's children has a component T. </summary>
        static public bool ChildHasComponent<T>(this Component t) where T : Component
        => t.gameObject.ChildHasComponent<T>();

        /// <summary> Adds component T to this object. </summary>
        static public T AddComponent<T>(this Component t) where T : Component
        => t.gameObject.AddComponent<T>();
        /// <summary> Returns component T attached to this object. If there's none, adds one. </summary>
        static public T GetOrAddComponent<T>(this Component t) where T : Component
        => t.gameObject.GetOrAddComponent<T>();
        /// <summary> Assigns component T to a. Returns whether any component was found. </summary>
        static public bool TryGetComponent<T>(this Component t, out T a) where T : Component
        => t.TryGetComponent(out a);

        //------------------------------------------------------------------------------------------------------------------------------- Various

        /// <summary> Returns the offset from this object to a. </summary>
        static public Vector3 OffsetTo(this Component t, GameObject a)
        => t.gameObject.OffsetTo(a);
        /// <summary> Returns the offset from this object to a. </summary>
        static public Vector3 OffsetTo(this Component t, Component a)
        => t.gameObject.OffsetTo(a.gameObject);
        /// <summary> Returns the offset from a to this object. </summary>
        static public Vector3 OffsetFrom(this Component t, GameObject a)
        => t.gameObject.OffsetFrom(a);
        /// <summary> Returns the offset from a to this object. </summary>
        static public Vector3 OffsetFrom(this Component t, Component a)
        => t.gameObject.OffsetFrom(a.gameObject);
        /// <summary> Returns the direction from this object towards a. </summary>
        static public Vector3 DirectionTowards(this Component t, GameObject a)
        => t.gameObject.DirectionTowards(a);
        /// <summary> Returns the direction from this object towards a. </summary>
        static public Vector3 DirectionTowards(this Component t, Component a)
        => t.gameObject.DirectionTowards(a.gameObject);
        /// <summary> Returns the direction from a towards this vector. </summary>
        static public Vector3 DirectionAwayFrom(this Component t, GameObject a)
        => t.gameObject.DirectionAwayFrom(a);
        /// <summary> Returns the direction from a towards this vector. </summary>
        static public Vector3 DirectionAwayFrom(this Component t, Component a)
        => t.gameObject.DirectionAwayFrom(a.gameObject);
        /// <summary> Returns the distance between this vector and a. </summary>
        static public float DistanceTo(this Component t, GameObject a)
        => t.gameObject.DistanceTo(a);
        /// <summary> Returns the distance between this vector and a. </summary>
        static public float DistanceTo(this Component t, Component a)
        => t.gameObject.DistanceTo(a.gameObject);

        /// <summary> Makes this object react to (or ignore) physics forces. </summary>
        static public void SetPhysics(this Component t, bool state)
        => t.gameObject.SetPhysics(state);
        /// <summary> Makes this object collide with (or ignore) other colliders. </summary>
        static public void SetCollisions(this Component t, bool state)
        => t.gameObject.SetCollisions(state);
        /// <summary> Makes this object collide with (or ignore) object a. </summary>
        static public void SetCollisionsWith(this Component t, GameObject a, bool state)
        => t.gameObject.SetCollisionsWith(a, state);
        /// <summary> Makes this object collide with (or ignore) object a. </summary>
        static public void SetCollisionsWith(this Component t, Component a, bool state)
        => t.gameObject.SetCollisionsWith(a.gameObject, state);
        /// <summary> Makes this object collide with (or ignore) objects from aCollection. </summary>
        static public void SetCollisionsWith(this Component t, IEnumerable<GameObject> aCollection, bool state)
        {
            foreach (var a in aCollection)
                t.gameObject.SetCollisionsWith(a, state);
        }
        /// <summary> Makes this object collide with (or ignore) objects from aCollection. </summary>
        static public void SetCollisionsWith(this Component t, IEnumerable<Component> aCollection, bool state)
        {
            foreach (var a in aCollection)
                t.gameObject.SetCollisionsWith(a.gameObject, state);
        }

        /// <summary> Copies a's transform. </summary>
        static public void CopyTransformFrom(this Component t, GameObject a)
        => t.gameObject.CopyTransformFrom(a);
        /// <summary> Copies a's transform. </summary>
        static public void CopyTransformFrom(this Component t, Component a)
        => t.gameObject.CopyTransformFrom(a.gameObject);
        /// <summary> Copies a's rigidbody. </summary>
        static public void CopyRigidbodyFrom(this Component t, GameObject a, bool copyMassAndTensors = false)
        => t.gameObject.CopyRigidbodyFrom(a, copyMassAndTensors);
        /// <summary> Copies a's rigidbody. </summary>
        static public void CopyRigidbodyFrom(this Component t, Component a, bool copyMassAndTensors = false)
        => t.gameObject.CopyRigidbodyFrom(a.gameObject, copyMassAndTensors);

        /// <summary> Adds a BoxCollider component to this object. </summary>
        static public BoxCollider AddBoxCollider(this Component t, Bounds a, bool isTrigger = false)
        => t.gameObject.AddBoxCollider(a, isTrigger);

        /// <summary> Destroys this component. </summary>
        static public void Destroy(this Component t)
        {
            if (Application.isPlaying)
                Object.Destroy(t);
            else
                Object.DestroyImmediate(t);
        }
        /// <summary> Destroys this component's object. </summary>
        static public void DestroyObject(this Component t)
        => t.gameObject.Destroy();
        /// <summary> Destroys these components. </summary>
        static public void Destroy(this Component[] t)
        {
            foreach (var component in t)
                component.Destroy();
        }
        /// <summary> Destroys these components' objects. </summary>
        static public void DestroyObjects(this Component[] t)
        {
            foreach (var component in t)
                component.gameObject.Destroy();
        }
        /// <summary> Destroys all components of type T attached to this object. </summary>
        static public void DestroyAll<T>(this Component t) where T : Component
        => t.GetComponents<T>().Destroy();
    }
}