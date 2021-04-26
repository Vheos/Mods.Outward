using UnityEngine;
using System.Collections.Generic;



namespace ModPack
{
    static public class Extensions_UnityObjects_GameObject
    {
        //------------------------------------------------------------------------------------------------------------------------------- Hierarchy

        /// <summary> Attaches a to this object. </summary>
        static public void BecomeParentOf(this GameObject t, GameObject a, bool retainWorldTransform = false)
        => a.transform.SetParent(t.transform, retainWorldTransform);
        /// <summary> Attaches a to this object. </summary>
        static public void BecomeParentOf(this GameObject t, Component a, bool retainWorldTransform = false)
        => t.BecomeParentOf(a.gameObject, retainWorldTransform);
        /// <summary> Attaches this objects to a's parent. </summary>
        static public void BecomeSiblingOf(this GameObject t, GameObject a, bool retainWorldTransform = false)
        => t.transform.SetParent(a.transform.parent, retainWorldTransform);
        /// <summary> Attaches this objects to a's parent. </summary>
        static public void BecomeSiblingOf(this GameObject t, Component a, bool retainWorldTransform = false)
        => t.BecomeSiblingOf(a.gameObject, retainWorldTransform);
        /// <summary> Attaches this object to a. </summary>
        static public void BecomeChildOf(this GameObject t, GameObject a, bool retainWorldTransform = false)
        => t.transform.SetParent(a.transform, retainWorldTransform);
        /// <summary> Attaches this object to a. </summary>
        static public void BecomeChildOf(this GameObject t, Component a, bool retainWorldTransform = false)
        => t.BecomeChildOf(a.gameObject, retainWorldTransform);
        /// <summary> Places this object at the root of the hierarchy. </summary>
        static public void Unparent(this GameObject t, bool retainWorldTransform = false)
        => t.transform.SetParent(null, retainWorldTransform);

        /// <summary> Checks whether this object is right above a in the heirarchy. </summary>
        static public bool IsParentOf(this GameObject t, GameObject a)
        => t.transform == a.transform.parent;
        /// <summary> Checks whether this object is right above a in the heirarchy. </summary>
        static public bool IsParentOf(this GameObject t, Component a)
        => t.IsParentOf(a.gameObject);
        /// <summary> Checks whether this object has the same parent as a. </summary>
        static public bool IsSiblingOf(this GameObject t, GameObject a)
        => t.transform.parent == a.transform.parent;
        /// <summary> Checks whether this object has the same parent as a. </summary>
        static public bool IsSiblingOf(this GameObject t, Component a)
        => t.IsSiblingOf(a.gameObject);
        /// <summary> Checks whether this object is right below a in the hierarchy. </summary>
        static public bool IsChildOf(this GameObject t, GameObject a)
        => t.transform.parent == a.transform;
        /// <summary> Checks whether this object is right below a in the hierarchy. </summary>
        static public bool IsChildOf(this GameObject t, Component a)
        => t.IsChildOf(a.gameObject);
        /// <summary> Checks whether this object is at the topmost level in the hierarchy. </summary>
        static public bool IsAtRoot(this GameObject t)
        => t.transform.parent == null;
        /// <summary> Checks whether this object has a parent. </summary>
        static public bool HasParent(this GameObject t)
        => t.transform.parent != null;
        /// <summary> Checks whether this object has a child. </summary>
        static public bool HasChild(this GameObject t)
        => t.transform.childCount > 0;
        /// <summary> Returns this object's child with name a. If it doesn't exist, returns null.</summary>
        static public GameObject FindChild(this GameObject t, string a)
        {
            Transform foundTransform = t.transform.Find(a);
            if (foundTransform != null)
                return foundTransform.gameObject;
            return null;
        }
        static public T FindChild<T>(this GameObject t, string a) where T : Component
        {
            Transform foundTransform = t.transform.Find(a);
            if (foundTransform != null)
                return foundTransform.GetComponent<T>();
            return null;
        }

        /// <summary> Returns this object's parent. </summary>
        static public GameObject GetParent(this GameObject t)
        {
            if (t.HasParent())
                return t.transform.parent.gameObject;
            return null;
        }
        /// <summary> Returns an array of this object's children. </summary>
        static public GameObject[] GetChildren(this GameObject t)
        {
            GameObject[] children = new GameObject[t.transform.childCount];
            for (int i = 0; i < children.Length; i++)
                children[i] = t.transform.GetChild(i).gameObject;
            return children;
        }
        /// <summary> Returns all of this object's ancestors. </summary>
        static public List<GameObject> GetAncestors(this GameObject t)
        {
            List<GameObject> ancestors = new List<GameObject>();
            for (Transform i = t.transform.parent; i != null; i = i.parent)
                ancestors.Add(i.gameObject);
            return ancestors;
        }

        /// <summary> Checks whether this object has a component T. </summary>
        static public bool HasComponent<T>(this GameObject t) where T : Component
        => t.GetComponent<T>() != null;
        /// <summary> Checks whether this object's parent has a component T. </summary>
        static public bool ParentHasComponent<T>(this GameObject t) where T : Component
        => t.HasParent() && t.GetComponentInParent<T>() != null;
        /// <summary> Checks whether any of this object's children has a component T. </summary>
        static public bool ChildHasComponent<T>(this GameObject t) where T : Component
        => t.HasChild() && t.GetComponentInChildren<T>() != null;

        /// <summary> Returns component T attached to this object. If there's none, adds one. </summary>
        static public T GetOrAddComponent<T>(this GameObject t) where T : Component
        {
            T component = t.GetComponent<T>();
            if (component != null)
                return component;
            return t.AddComponent<T>();
        }
        /// <summary> Assigns component T to a. Returns whether any component was found. </summary>
        static public bool TryGetComponent<T>(this GameObject t, out T a) where T : Component
        {
            a = t.GetComponent<T>();
            return a != null;
        }

        //------------------------------------------------------------------------------------------------------------------------------- Various

        /// <summary> Returns the offset from this object to a. </summary>
        static public Vector3 OffsetTo(this GameObject t, GameObject a)
        => t.transform.position.OffsetTo(a.transform.position);
        /// <summary> Returns the offset from this object to a. </summary>
        static public Vector3 OffsetTo(this GameObject t, Component a)
        => t.OffsetTo(a.gameObject);
        /// <summary> Returns the offset from a to this object. </summary>
        static public Vector3 OffsetFrom(this GameObject t, GameObject a)
        => t.transform.position.OffsetFrom(a.transform.position);
        /// <summary> Returns the offset from a to this object. </summary>
        static public Vector3 OffsetFrom(this GameObject t, Component a)
        => t.OffsetFrom(a.gameObject);
        /// <summary> Returns the direction from this object towards a. </summary>
        static public Vector3 DirectionTowards(this GameObject t, GameObject a)
        => t.transform.position.DirectionTowards(a.transform.position);
        /// <summary> Returns the direction from this object towards a. </summary>
        static public Vector3 DirectionTowards(this GameObject t, Component a)
        => t.DirectionTowards(a.gameObject);
        /// <summary> Returns the direction from a towards this vector. </summary>
        static public Vector3 DirectionAwayFrom(this GameObject t, GameObject a)
        => t.transform.position.DirectionAwayFrom(a.transform.position);
        /// <summary> Returns the direction from a towards this vector. </summary>
        static public Vector3 DirectionAwayFrom(this GameObject t, Component a)
        => t.DirectionAwayFrom(a.gameObject);
        /// <summary> Returns the distance between this vector and a. </summary>
        static public float DistanceTo(this GameObject t, GameObject a)
        => t.transform.position.DistanceTo(a.transform.position);
        /// <summary> Returns the distance between this vector and a. </summary>
        static public float DistanceTo(this GameObject t, Component a)
        => t.DistanceTo(a.gameObject);

        /// <summary> Makes this object react to (or ignore) physics forces. </summary>
        static public void SetPhysics(this GameObject t, bool state)
        => t.GetComponent<Rigidbody>().isKinematic = !state;
        /// <summary> Makes this object collide with (or ignore) other colliders. </summary>
        static public void SetCollisions(this GameObject t, bool state)
        {
            foreach (var collider in t.GetComponents<Collider>())
                collider.isTrigger = !state;
        }
        /// <summary> Makes this object collide with (or ignore) object a. </summary>
        static public void SetCollisionsWith(this GameObject t, GameObject a, bool state)
        {
            foreach (var tCollider in t.GetComponents<Collider>())
                foreach (var aCollider in a.GetComponents<Collider>())
                    Physics.IgnoreCollision(tCollider, aCollider, !state);
        }
        /// <summary> Makes this object collide with (or ignore) object a. </summary>
        static public void SetCollisionsWith(this GameObject t, Component a, bool state)
        => t.SetCollisionsWith(a.gameObject, state);
        /// <summary> Makes this object collide with (or ignore) objects from aCollection. </summary>
        static public void SetCollisionsWith(this GameObject t, IEnumerable<GameObject> aCollection, bool state)
        {
            foreach (var a in aCollection)
                t.SetCollisionsWith(a, state);
        }
        /// <summary> Makes this object collide with (or ignore) objects from aCollection. </summary>
        static public void SetCollisionsWith(this GameObject t, IEnumerable<Component> aCollection, bool state)
        {
            foreach (var a in aCollection)
                t.SetCollisionsWith(a.gameObject, state);
        }

        /// <summary> Copies a's transform. </summary>
        static public void CopyTransformFrom(this GameObject t, GameObject a)
        => CopyTransform(a.transform, t.transform);
        /// <summary> Copies a's transform. </summary>
        static public void CopyTransformFrom(this GameObject t, Component a)
        => t.CopyTransformFrom(a.gameObject);
        /// <summary> Copies a's rigidbody. </summary>
        static public void CopyRigidbodyFrom(this GameObject t, GameObject a, bool copyMassAndTensors = false)
        => CopyRigidbody(a.GetComponent<Rigidbody>(), t.GetComponent<Rigidbody>(), copyMassAndTensors);
        /// <summary> Copies a's rigidbody. </summary>
        static public void CopyRigidbodyFrom(this GameObject t, Component a, bool copyMassAndTensors = false)
        => t.CopyRigidbodyFrom(a.gameObject, copyMassAndTensors);

        /// <summary> Adds a BoxCollider component to this object. </summary>
        static public BoxCollider AddBoxCollider(this GameObject t, Bounds a, bool isTrigger = false)
        {
            BoxCollider boxCollider = t.AddComponent<BoxCollider>();
            boxCollider.center = a.center;
            boxCollider.size = a.size;
            boxCollider.isTrigger = isTrigger;
            return boxCollider;
        }

        /// <summary> Destroys this object. </summary>
        static public void Destroy(this GameObject t)
        {
            if (Application.isPlaying)
                Object.Destroy(t);
            else
                Object.DestroyImmediate(t);
        }
        /// <summary> Destroys this object immediately. </summary>
        static public void DestroyImmediately(this GameObject t)
        => Object.DestroyImmediate(t);
        /// <summary> Destroys these objects. </summary>
        static public void Destroy(this GameObject[] t)
        {
            foreach (var gameObject in t)
                gameObject.Destroy();
        }
        /// <summary> Destroys these objects. </summary>
        static public void DestroyImmediately(this GameObject[] t)
        {
            foreach (var gameObject in t)
                gameObject.DestroyImmediately();
        }

        // Privates
        static private void CopyTransform(Transform from, Transform to)
        {
            to.localPosition = from.localPosition;
            to.localRotation = from.localRotation;
            to.localScale = from.localScale;
        }
        static private void CopyRigidbody(Rigidbody from, Rigidbody to, bool copyMassAndTensors = false)
        {
            // Linear
            to.velocity = from.velocity;
            to.drag = from.drag;

            // Angular
            to.angularVelocity = from.angularVelocity;
            to.angularDrag = from.angularDrag;

            // Affected by
            to.isKinematic = from.isKinematic;
            to.useGravity = from.useGravity;
            to.freezeRotation = from.freezeRotation;
            to.constraints = from.constraints;

            // Mass & Tensors
            to.mass = 1f;
            to.ResetCenterOfMass();
            to.ResetInertiaTensor();
            if (copyMassAndTensors)
            {
                to.mass = from.mass;
                to.centerOfMass = from.centerOfMass;
                to.inertiaTensor = from.inertiaTensor;
                to.inertiaTensorRotation = from.inertiaTensorRotation;
            }

            // Collisions
            to.detectCollisions = from.detectCollisions;
            to.collisionDetectionMode = from.collisionDetectionMode;
            to.interpolation = from.interpolation;

            // Max & Min
            to.maxAngularVelocity = from.maxAngularVelocity;
            to.maxDepenetrationVelocity = from.maxDepenetrationVelocity;
            to.sleepThreshold = from.sleepThreshold;

            // Precision
            to.solverIterations = from.solverIterations;
            to.solverVelocityIterations = from.solverVelocityIterations;
        }
    }
}