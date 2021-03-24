using UnityEngine;
using System.Collections.Generic;



namespace ModPack
{
    static public class Math_Extensions_Unique
    {
        //------------------------------------------------------------------------------------------------------------------------------- int

        /// <summary> Tests whether this value is even. </summary>
        static public bool IsEven(this int t)
        => t % 2 == 0;
        /// <summary> Tests whether this value is odd. </summary>
        static public bool IsOdd(this int t)
        => t % 2 == 1;
        /// <summary> Returns the sum of all elements in the collection. </summary>
        static public float Sum(this ICollection<int> t)
        {
            float r = 0;
            foreach (var element in t)
                r += element;
            return r;
        }
        /// <summary> Returns this value as float. </summary>
        static public float AsFloat(this int t)
        => (float)t;



        //------------------------------------------------------------------------------------------------------------------------------- float

        /// <summary> Returns the square root of this value. </summary>
        static public float Sqrt(this float t)
        => Mathf.Sqrt(t);
        /// <summary> Returns the absolute difference between this value and a. </summary>
        static public float DistanceTo(this float t, float a)
        => (t - a).Abs();

        /// <summary> Tests whether this value is similar to a. </summary>
        static public bool IsSimilarTo(this float t, float a)
        => Mathf.Approximately(t, a);
        /// <summary> Tests whether this value is greater than or similar to a. </summary>
        static public bool IsGreaterOrSimilarTo(this float t, float a)
        => t > a || Mathf.Approximately(t, a);
        /// <summary> Tests whether this value is smaller than or similar to a. </summary>
        static public bool IsLessOrSimilarTo(this float t, float a)
        => t < a || Mathf.Approximately(t, a);
        /// <summary> Tests whether this value is different than a. </summary>
        static public bool IsDifferentThan(this float t, float a)
        => !Mathf.Approximately(t, a);
        /// <summary> Tests whether this value is greater than a. </summary>
        static public bool IsGreaterThan(this float t, float a)
        => t > a && !Mathf.Approximately(t, a);
        /// <summary> Tests whether this value is smaller than a. </summary>
        static public bool IsLessThan(this float t, float a)
        => t < a && !Mathf.Approximately(t, a);
        /// <summary> Tests whether this value is between than a and b. </summary>
        static public bool IsBetween(this float t, float a, float b, bool leftInclusive = true, bool rightInclusive = true)
        {
            return (leftInclusive ? t.IsGreaterOrSimilarTo(a) : t.IsGreaterThan(a))
                && (rightInclusive ? t.IsLessOrSimilarTo(b) : t.IsLessThan(b));
        }

        /// <summary> Returns the sum of all elements in the collection. </summary>
        static public float Sum(this ICollection<float> t)
        {
            float r = 0;
            foreach (var element in t)
                r += element;
            return r;
        }
        /// <summary> Returns the product of all elements in the collection. </summary>
        static public float Product(this ICollection<float> t)
        {
            float r = 1;
            foreach (var element in t)
                r *= element;
            return r;
        }
        /// <summary> Returns the smallest of all elements in the collection. </summary>
        static public float Min(this ICollection<float> t)
        {
            float r = float.MaxValue;
            foreach (var element in t)
                if (element < r)
                    r = element;
            return r;
        }
        /// <summary> Returns the greatest of all elements in the collection. </summary>
        static public float Max(this ICollection<float> t)
        {
            float r = float.MinValue;
            foreach (var element in t)
                if (element > r)
                    r = element;
            return r;
        }
        /// <summary> Returns the average of all elements in the collection. </summary>
        static public float Avg(this ICollection<float> t)
        => t.Sum() / t.Count;

        /// <summary> Returns Vector2 with all components set to this value. </summary>
        static public Vector2 ToVector2(this float t)
        => new Vector2(t, t);
        /// <summary> Returns Vector3 with all components set to this value. </summary>
        static public Vector3 ToVector3(this float t)
        => new Vector3(t, t, t);



        //------------------------------------------------------------------------------------------------------------------------------- Vector2

        /// <summary> Returns the dot product of this vector and a. </summary>
        static public float Dot(this Vector2 t, Vector2 a)
        => Vector2.Dot(t, a);
        /// <summary> Returns the cross product of this vector and a. </summary>
        static public Vector3 Cross(this Vector2 t, Vector2 a)
        => Vector3.Cross(t, a);
        /// <summary> Checks whether all of this vector's components are 0. </summary>
        static public bool IsZero(this Vector2 t)
        => t == Vector2.zero;

        /// <summary> Returns the offset from this vector to a. </summary>
        static public Vector2 OffsetTo(this Vector2 t, Vector2 a)
        => (a - t);
        /// <summary> Returns the offset from a to this vector. </summary>
        static public Vector2 OffsetFrom(this Vector2 t, Vector2 a)
        => (t - a);
        /// <summary> Returns the direction from this vector towards a. </summary>
        static public Vector2 DirectionTowards(this Vector2 t, Vector2 a)
        => (a - t).normalized;
        /// <summary> Returns the direction from a towards this vector. </summary>
        static public Vector2 DirectionAwayFrom(this Vector2 t, Vector2 a)
        => (t - a).normalized;
        /// <summary> Returns the distance between this vector and a. </summary>
        static public float DistanceTo(this Vector2 t, Vector2 a)
        => (a - t).magnitude;
        /// <summary> Returns the rotation from this vector to a. </summary>
        static public Quaternion RotationTo(this Vector2 t, Vector2 a)
        => Quaternion.FromToRotation(t, a);
        /// <summary> Returns the rotation from a to this vector. </summary>
        static public Quaternion RotationFrom(this Vector2 t, Vector2 a)
        => Quaternion.FromToRotation(a, t);

        /// <summary> Applies the rotation a to this vector. </summary>
        static public Vector3 Rotate(this Vector2 t, Quaternion a)
        => a * t;
        /// <summary> Reverts the rotation a from this vector. </summary>
        static public Vector3 Unrotate(this Vector2 t, Quaternion a)
        => a.Neg() * t;
        /// <summary> Applies chosen a transform to this vector. </summary>
        static public Vector3 Transform(this Vector2 t, Transform a)
        => a.TransformPoint(t);
        /// <summary> Reverts chosen a transform from this vector. </summary>
        static public Vector3 Untransform(this Vector2 t, Transform a)
        => a.InverseTransformPoint(t);

        /// <summary> Swaps the x and y components. </summary>
        static public Vector2 SwapComps(this Vector2 t)
        => new Vector2(t.y, t.x);
        /// <summary> Appends z component to this vector. </summary>
        static public Vector3 Append(this Vector2 t, float z = 0)
        => new Vector3(t.x, t.y, z);
        /// <summary> Appends z and w components to this vector. </summary>
        static public Vector4 Append(this Vector2 t, float z, float w)
        => new Vector4(t.x, t.y, z, w);
        /// <summary> Appends a's components as z and w to this vector. </summary>
        static public Vector4 Append(this Vector2 t, Vector2 a)
        => t.Append(a.x, a.y);

        /// <summary> Returns the smallest of this vector's components. </summary>
        static public float MinComp(this Vector2 t)
        => t.x.Min(t.y);
        /// <summary> Returns the greatest of this vector's components. </summary>
        static public float MaxComp(this Vector2 t)
        => t.x.Max(t.y);
        /// <summary> Returns the average of this vector's components. </summary>
        static public float AvgComp(this Vector2 t)
        => (t.x + t.y) / 2f;
        /// <summary> Divides this vector's components by a component with the greatest absolute value. </summary>
        static public Vector2 NormalizeComps(this Vector2 t)
        => t / t.Abs().MaxComp();




        //------------------------------------------------------------------------------------------------------------------------------- Vector3

        /// <summary> Returns the dot product of this vector and a. </summary>
        static public float Dot(this Vector3 t, Vector3 a)
        => Vector3.Dot(t, a);
        /// <summary> Returns the cross product of this vector and a. </summary>
        static public Vector3 Cross(this Vector3 t, Vector3 a)
        => Vector3.Cross(t, a);
        /// <summary> Checks whether all of this vector's components are 0. </summary>
        static public bool IsZero(this Vector3 t)
        => t == Vector3.zero;

        /// <summary> Returns the offset from this vector to a. </summary>
        static public Vector3 OffsetTo(this Vector3 t, Vector3 a)
        => (a - t);
        /// <summary> Returns the offset from a to this vector. </summary>
        static public Vector3 OffsetFrom(this Vector3 t, Vector3 a)
        => (t - a);
        /// <summary> Returns the direction from this vector towards a. </summary>
        static public Vector3 DirectionTowards(this Vector3 t, Vector3 a)
        => (a - t).normalized;
        /// <summary> Returns the direction from a towards this vector. </summary>
        static public Vector3 DirectionAwayFrom(this Vector3 t, Vector3 a)
        => (t - a).normalized;
        /// <summary> Returns the distance between this vector and a. </summary>
        static public float DistanceTo(this Vector3 t, Vector3 a)
        => (a - t).magnitude;
        /// <summary> Returns the rotation from this vector to a. </summary>
        static public Quaternion RotationTo(this Vector3 t, Vector3 a)
        => Quaternion.FromToRotation(t, a);
        /// <summary> Returns the rotation from a to this vector. </summary>
        static public Quaternion RotationFrom(this Vector3 t, Vector3 a)
        => Quaternion.FromToRotation(a, t);

        /// <summary> Applies the rotation a to this vector. </summary>
        static public Vector3 Rotate(this Vector3 t, Quaternion a)
        => a * t;
        /// <summary> Reverts the rotation a from this vector. </summary>
        static public Vector3 Unrotate(this Vector3 t, Quaternion a)
        => a.Neg() * t;
        /// <summary> Applies the transform a to this vector. </summary>
        static public Vector3 Transform(this Vector3 t, Transform a)
        => a.TransformPoint(t);
        /// <summary> Reverts the transform a from this vector. </summary>
        static public Vector3 Untransform(this Vector3 t, Transform a)
        => a.InverseTransformPoint(t);

        /// <summary> Appends w component to this vector. </summary>
        static public Vector4 Append(this Vector3 t, float w = 0)
        => new Vector4(t.x, t.y, t.z, w);
        /// <summary> Returns this vector's XY components. </summary>
        static public Vector2 XY(this Vector3 t)
        => new Vector4(t.x, t.y);

        /// <summary> Returns the smallest of this vector's components. </summary>
        static public float MinComp(this Vector3 t)
        => Mathf.Min(t.x, t.y, t.z);
        /// <summary> Returns the greatest of this vector's components. </summary>
        static public float MaxComp(this Vector3 t)
        => Mathf.Max(t.x, t.y, t.z);
        /// <summary> Returns the average of this vector's components. </summary>
        static public float AvgComp(this Vector3 t)
        => (t.x + t.y + t.z) / 3f;
        /// <summary> Divides this vector's components by a component with the greatest absolute value. </summary>
        static public Vector3 NormalizeComps(this Vector3 t)
        => t / t.Abs().MaxComp();



        //------------------------------------------------------------------------------------------------------------------------------- Quaternion

        /// <summary> Adds a to this rotation. </summary>
        static public Quaternion Add(this Quaternion t, Quaternion a)
        => a * t;
        /// <summary> Subtracts a from this rotation. </summary>
        static public Quaternion Sub(this Quaternion t, Quaternion a)
        => a.Neg() * t;
        /// <summary> Returns the opposite of this rotation. </summary>
        static public Quaternion Neg(this Quaternion t)
        => Quaternion.Inverse(t);

        /// <summary> Lerps from this rotation to a at alpha b. </summary>
        static public Quaternion Lerp(this Quaternion t, Quaternion a, float b)
        => Quaternion.LerpUnclamped(t, a, b);
        /// <summary> Lerps from this rotation to a at alpha b (clamped between 0 and 1). </summary>
        static public Quaternion LerpClamped(this Quaternion t, Quaternion a, float b)
        => Quaternion.Lerp(t, a, b);
        /// <summary> Spherically lerps from this rotation to a at alpha b. </summary>
        static public Quaternion SLerp(this Quaternion t, Quaternion a, float b)
        => Quaternion.SlerpUnclamped(t, a, b);
        /// <summary> Spherically lerps from this rotation to a at alpha b (clamped between 0 and 1). </summary>
        static public Quaternion SLerpClamped(this Quaternion t, Quaternion a, float b)
        => Quaternion.Slerp(t, a, b);

        /// <summary> Returns right direction relative to this rotation. </summary>
        static public Vector3 Right(this Quaternion t)
        => Vector3.right.Rotate(t);
        /// <summary> Returns left direction relative to this rotation. </summary>
        static public Vector3 Left(this Quaternion t)
        => Vector3.left.Rotate(t);
        /// <summary> Returns up direction relative to this rotation. </summary>
        static public Vector3 Up(this Quaternion t)
        => Vector3.up.Rotate(t);
        /// <summary> Returns down direction relative to this rotation. </summary>
        static public Vector3 Down(this Quaternion t)
        => Vector3.down.Rotate(t);
        /// <summary> Returns far direction relative to this rotation. </summary>
        static public Vector3 Far(this Quaternion t)
        => Vector3.forward.Rotate(t);
        /// <summary> Returns near direction relative to this rotation. </summary>
        static public Vector3 Near(this Quaternion t)
        => Vector3.back.Rotate(t);
    }
}