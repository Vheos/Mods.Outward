using UnityEngine;
using System.Collections.Generic;



namespace ModPack
{
    static public class Math_Extensions_Common
    {
        //------------------------------------------------------------------------------------------------------------------------------- int

        /// <summary> Adds a to this value. </summary>
        static public int Add(this int t, int a)
        => t + a;
        /// <summary> Subtracts a from this value. </summary>
        static public int Sub(this int t, int a)
        => t - a;
        /// <summary> Multiplies this value by a. </summary>
        static public int Mul(this int t, int a)
        => t * a;
        /// <summary> Divides this value by a. </summary>
        static public float Div(this int t, int a)
        => t / (float)a;
        /// <summary> Raises this value to the power of a. </summary>
        static public int Pow(this int t, int a)
        => (int)Mathf.Round(Mathf.Pow(t, a));
        /// <summary> Returns the remainder of this value divded by a. </summary>
        static public int Mod(this int t, int a)
        => t % a;
        /// <summary> Returns the positive remainder of this value divded by a. </summary>
        static public int PosMod(this int t, int a)
        => (t % a + a) % a;

        /// <summary> Multiplies this value by -1. </summary>
        static public int Neg(this int t)
        => -t;
        /// <summary> Divides 1 by this value. </summary>
        static public float Inv(this int t)
        => 1 / (float)t;
        /// <summary> Removes this value's sign. </summary>
        static public float Abs(this int t)
        => Mathf.Abs(t);
        /// <summary> Returns this value's sign. </summary>
        static public int Sig(this int t)
        => System.Math.Sign(t);

        /// <summary> Rounds this value to the nearest multiple of a. </summary>
        static public int RoundToMultiple(this int t, int a)
        => (int)Mathf.Round(t / (float)a) * a;
        /// <summary> Rounds this value to the nearest multiple of a. </summary>
        static public float RoundToMultiple(this int t, float a)
        => Mathf.Round(t / a) * a;

        /// <summary> Returns the smaller between this value and a. </summary>
        static public int Min(this int t, int a)
        => Mathf.Min(t, a);
        /// <summary> Returns the greater between this value and a. </summary>
        static public int Max(this int t, int a)
        => Mathf.Max(t, a);
        /// <summary> Returns the average of this value and a. </summary>
        static public float Avg(this int t, int a)
        => (t + a) / 2f;

        /// <summary> Clamps this value to a minimum of a. </summary>
        static public int ClampMin(this int t, int a)
        => Mathf.Max(t, a);
        /// <summary> Clamps this value to a maximum of a. </summary>
        static public int ClampMax(this int t, int a)
        => Mathf.Min(t, a);
        /// <summary> Clamps this value between a and b.</summary>
        static public int Clamp(this int t, int a, int b)
        => Mathf.Clamp(t, a, b);

        /// <summary> Lerps from this value to a at alpha b. </summary>
        static public float Lerp(this int t, int a, float b)
        => Mathf.LerpUnclamped(t, a, b);
        /// <summary> Lerps from this value to a at alpha b (clamped between 0 and 1). </summary>
        static public float LerpClamped(this int t, int a, float b)
        => Mathf.Lerp(t, a, b);
        /// <summary> Maps this value from the range [a, b] to [c, d]. </summary>
        static public float Map(this int t, int a, int b, int c, int d)
        => (t - a) * (d - c) / (float)(b - a) + c;
        /// <summary> Maps this value from the range [a, b] to [c, d] (with clamped output). </summary>
        static public float MapClamped(this int t, int a, int b, int c, int d)
        {
            return t <= a ? c
                 : t >= b ? d
                 : t.Map(a, b, c, d);
        }



        //------------------------------------------------------------------------------------------------------------------------------- float

        /// <summary> Adds a to this value. </summary>
        static public float Add(this float t, float a)
        => t + a;
        /// <summary> Subtracts a from this value. </summary>
        static public float Sub(this float t, float a)
        => t - a;
        /// <summary> Multiplies this value by a. </summary>
        static public float Mul(this float t, float a)
        => t * a;
        /// <summary> Divides this value by a. </summary>
        static public float Div(this float t, float a)
        => t / a;
        /// <summary> Raises this value to the power of a. </summary>
        static public float Pow(this float t, float a)
        => Mathf.Pow(t, a);
        /// <summary> Returns the remainder of this value divded by a. </summary>
        static public float Mod(this float t, float a)
        => t % a;
        /// <summary> Returns the positive remainder of this value divded by a. </summary>
        static public float PosMod(this float t, float a)
        => (t % a + a) % a;

        /// <summary> Multiplies this value by -1. </summary>
        static public float Neg(this float t)
        => -t;
        /// <summary> Divides 1 by this value. </summary>
        static public float Inv(this float t)
        => 1 / t;
        /// <summary> Removes this value's sign. </summary>
        static public float Abs(this float t)
        => Mathf.Abs(t);
        /// <summary> Returns this value's sign. </summary>
        static public int Sig(this float t)
        => System.Math.Sign(t);

        /// <summary> Rounds this value to the nearest integer. </summary>
        static public int Round(this float t)
        => (int)Mathf.Round(t);
        /// <summary> Rounds this value to the smaller integer. </summary>
        static public int RoundDown(this float t)
        => Mathf.FloorToInt(t);
        /// <summary> Rounds this value to the greater integer. </summary>
        static public int RoundUp(this float t)
        => Mathf.CeilToInt(t);
        /// <summary> Rounds this value to the nearest multiple of a. </summary>
        static public int RoundToMultiple(this float t, int a)
        => (int)Mathf.Round(t / a) * a;
        /// <summary> Rounds this value to the nearest multiple of a. </summary>
        static public float RoundToMultiple(this float t, float a)
        => Mathf.Round(t / a) * a;

        /// <summary> Returns the smaller between this value and a. </summary>
        static public float Min(this float t, float a)
        => Mathf.Min(t, a);
        /// <summary> Returns the greater between this value and a. </summary>
        static public float Max(this float t, float a)
        => Mathf.Max(t, a);
        /// <summary> Returns the average of this value and a. </summary>
        static public float Avg(this float t, float a)
        => (t + a) / 2f;

        /// <summary> Clamps this value to a minimum of a. </summary>
        static public float ClampMin(this float t, float a)
        => Mathf.Max(t, a);
        /// <summary> Clamps this value to a maximum of a. </summary>
        static public float ClampMax(this float t, float a)
        => Mathf.Min(t, a);
        /// <summary> Clamps this value between a and b.</summary>
        static public float Clamp(this float t, float a, float b)
        => Mathf.Clamp(t, a, b);
        /// <summary> Clamps this value between 0 and 1.</summary>
        static public float Clamp01(this float t)
        => t.Clamp(0f, 1f);

        /// <summary> Lerps from this value to a at alpha b. </summary>
        static public float Lerp(this float t, float a, float b)
        => Mathf.LerpUnclamped(t, a, b);
        /// <summary> Lerps from this value to a at alpha b (clamped between 0 and 1). </summary>
        static public float LerpClamped(this float t, float a, float b)
        => Mathf.Lerp(t, a, b);
        /// <summary> Maps this value from the range [a, b] to [c, d]. </summary>
        static public float Map(this float t, float a, float b, float c, float d)
        => (t - a) * (d - c) / (b - a) + c;
        /// <summary> Maps this value from the range [a, b] to [c, d] (with clamped output). </summary>
        static public float MapClamped(this float t, float a, float b, float c, float d)
        {
            return t <= a ? c
                 : t >= b ? d
                 : t.Map(a, b, c, d);
        }
        /// <summary> Maps this value from the range [a, b] to [0, 1]. </summary>
        static public float MapTo01(this float t, float a, float b)
        => t.Map(a, b, 0f, 1f);
        /// <summary> Maps this value from the range [0, 1] to [a, b]. </summary>
        static public float MapFrom01(this float t, float a, float b)
        => t.Map(0f, 1f, a, b);

        /// <summary> Tests whether this value is NaN. </summary>
        static public bool IsNaN(this float value)
        => float.IsNaN(value);
        /// <summary> Tests whether this value is infinity. </summary>
        static public bool IsInf(this float value)
        => float.IsInfinity(value);



        //------------------------------------------------------------------------------------------------------------------------------- Vector2

        /// <summary> Adds (x, y) to this vector's components. </summary>
        static public Vector2 Add(this Vector2 t, float x, float y)
        => new Vector2(t.x.Add(x), t.y.Add(y));
        /// <summary> Adds (a, a) to this vector's components. </summary>
        static public Vector2 Add(this Vector2 t, float a)
        => t.Add(a, a);
        /// <summary> Adds (a.x, a.y) to this vector's components. </summary>
        static public Vector2 Add(this Vector2 t, Vector2 a)
        => t.Add(a.x, a.y);
        /// <summary> Subtracts (x, y) from this vector's components. </summary>
        static public Vector2 Sub(this Vector2 t, float x, float y)
        => new Vector2(t.x.Sub(x), t.y.Sub(y));
        /// <summary> Subtracts (a, a) from this vector's components. </summary>
        static public Vector2 Sub(this Vector2 t, float a)
        => t.Sub(a, a);
        /// <summary> Subtracts (a.x, a.y) from this vector's components. </summary>
        static public Vector2 Sub(this Vector2 t, Vector2 a)
        => t.Sub(a.x, a.y);
        /// <summary> Multiplies this vector's components by (x, y). </summary>
        static public Vector2 Mul(this Vector2 t, float x, float y)
        => new Vector2(t.x.Mul(x), t.y.Mul(y));
        /// <summary> Multiplies this vector's components by (a, a). </summary>
        static public Vector2 Mul(this Vector2 t, float a)
        => t.Mul(a, a);
        /// <summary> Multiplies this vector's components by (a.x, a.y). </summary>
        static public Vector2 Mul(this Vector2 t, Vector2 a)
        => t.Mul(a.x, a.y);
        /// <summary> Divides this vector's components by (x, y). </summary>
        static public Vector2 Div(this Vector2 t, float x, float y)
        => new Vector2(t.x.Div(x), t.y.Div(y));
        /// <summary> Divides this vector's components by (a, a). </summary>
        static public Vector2 Div(this Vector2 t, float a)
        => t.Div(a, a);
        /// <summary> Divides this vector's components by (a.x, a.y). </summary>
        static public Vector2 Div(this Vector2 t, Vector2 a)
        => t.Div(a.x, a.y);
        /// <summary> Raises this vector's components to the power of (x, y). </summary>
        static public Vector2 Pow(this Vector2 t, float x, float y)
        => new Vector2(t.x.Pow(x), t.y.Pow(y));
        /// <summary> Raises this vector's components to the power of (a, a). </summary>
        static public Vector2 Pow(this Vector2 t, float a)
        => t.Pow(a, a);
        /// <summary> Raises this vector's components to the power of (a.x, a.y). </summary>
        static public Vector2 Pow(this Vector2 t, Vector2 a)
        => t.Pow(a.x, a.y);
        /// <summary> Returns the remainder of this vector's components divided by (x, y). </summary>
        static public Vector2 Mod(this Vector2 t, float x, float y)
        => new Vector2(t.x.Mod(x), t.y.Mod(y));
        /// <summary> Returns the remainder of this vector's components divided by (a, yy). </summary>
        static public Vector2 Mod(this Vector2 t, float a)
        => t.Mod(a, a);
        /// <summary> Returns the remainder of this vector's components divided by (a.x, a.y). </summary>
        static public Vector2 Mod(this Vector2 t, Vector2 a)
        => t.Mod(a.x, a.y);
        /// <summary> Returns the positive remainder of this vector's components divided by (x, y). </summary>
        static public Vector2 PosMod(this Vector2 t, float x, float y)
        => new Vector2(t.x.PosMod(x), t.y.PosMod(y));
        /// <summary> Returns the positive remainder of this vector's components divided by (a, yy). </summary>
        static public Vector2 PosMod(this Vector2 t, float a)
        => t.PosMod(a, a);
        /// <summary> Returns the positive remainder of this vector's components divided by (a.x, a.y). </summary>
        static public Vector2 PosMod(this Vector2 t, Vector2 a)
        => t.PosMod(a.x, a.y);

        /// <summary> Multiplies this vector's components by -1. </summary>
        static public Vector2 Neg(this Vector2 t)
        => new Vector2(t.x.Neg(), t.y.Neg());
        /// <summary> Divides 1 by this vector's components. </summary>
        static public Vector2 Inv(this Vector2 t)
        => new Vector2(t.x.Inv(), t.y.Inv());
        /// <summary> Removes this vector's components' sign. </summary>
        static public Vector2 Abs(this Vector2 t)
        => new Vector2(t.x.Abs(), t.y.Abs());
        /// <summary> Returns this vector's components' sign. </summary>
        static public Vector2 Sig(this Vector2 t)
        => new Vector2(t.x.Sig(), t.y.Sig());

        /// <summary> Rounds this value to the nearest integer. </summary>
        static public Vector2Int Round(this Vector2 t)
        => new Vector2Int(t.x.Round(), t.y.Round());
        /// <summary> Rounds this value to the smaller integer. </summary>
        static public Vector2Int RoundDown(this Vector2 t)
        => new Vector2Int(t.x.RoundDown(), t.y.RoundDown());
        /// <summary> Rounds this value to the greater integer. </summary>
        static public Vector2Int RoundUp(this Vector2 t)
        => new Vector2Int(t.x.RoundUp(), t.y.RoundUp());
        /// <summary> Rounds this vector's components to the nearest multiple of (x, y). </summary>
        static public Vector2Int RoundToMultiple(this Vector2 t, int x, int y)
        => new Vector2Int(t.x.RoundToMultiple(x), t.y.RoundToMultiple(y));
        /// <summary> Rounds this vector's components to the nearest multiple of (a, a). </summary>
        static public Vector2Int RoundToMultiple(this Vector2 t, int a)
        => t.RoundToMultiple(a, a);
        /// <summary> Rounds this vector's components to the nearest multiple of (a.x, a.y). </summary>
        static public Vector2Int RoundToMultiple(this Vector2 t, Vector2Int a)
        => t.RoundToMultiple(a.x, a.y);
        /// <summary> Rounds this vector's components to the nearest multiple of (x, y). </summary>
        static public Vector2 RoundToMultiple(this Vector2 t, float x, float y)
        => new Vector2(t.x.RoundToMultiple(x), t.y.RoundToMultiple(y));
        /// <summary> Rounds this vector's components to the nearest multiple of (a, a). </summary>
        static public Vector2 RoundToMultiple(this Vector2 t, float a)
        => t.RoundToMultiple(a, a);
        /// <summary> Rounds this vector's components to the nearest multiple of (a.x, a.y). </summary>
        static public Vector2 RoundToMultiple(this Vector2 t, Vector2 a)
        => t.RoundToMultiple(a.x, a.y);

        /// <summary> Returns the smaller between this vector's components and (x, y). </summary>
        static public Vector2 Min(this Vector2 t, float x, float y)
        => new Vector2(t.x.Min(x), t.y.Min(y));
        /// <summary> Returns the smaller between this vector's components and (a, a). </summary>
        static public Vector2 Min(this Vector2 t, float a)
        => t.Min(a, a);
        /// <summary> Returns the smaller between this vector's components and (a.x, a.y). </summary>
        static public Vector2 Min(this Vector2 t, Vector2 a)
        => t.Min(a.x, a.y);
        /// <summary> Returns the greater between this vector's components and (x, y). </summary>
        static public Vector2 Max(this Vector2 t, float x, float y)
        => new Vector2(t.x.Max(x), t.y.Max(y));
        /// <summary> Returns the greater between this vector's components and (a, a). </summary>
        static public Vector2 Max(this Vector2 t, float a)
        => t.Max(a, a);
        /// <summary> Returns the greater between this vector's components and (a.x, a.y). </summary>
        static public Vector2 Max(this Vector2 t, Vector2 a)
        => t.Max(a.x, a.y);
        /// <summary> Returns the average of this vector's components and (x, y). </summary>
        static public Vector2 Avg(this Vector2 t, float x, float y)
        => new Vector2(t.x.Avg(x), t.y.Avg(y));
        /// <summary> Returns the average of this vector's components and (a, a). </summary>
        static public Vector2 Avg(this Vector2 t, float a)
        => t.Avg(a, a);
        /// <summary> Returns the average of this vector's components and (a.x, a.y). </summary>
        static public Vector2 Avg(this Vector2 t, Vector2 a)
        => t.Avg(a.x, a.y);

        /// <summary> Clamps this vector's components to a minimum of (x, y). </summary>
        static public Vector2 ClampMin(this Vector2 t, float x, float y)
        => new Vector2(t.x.ClampMin(x), t.y.ClampMin(y));
        /// <summary> Clamps this vector's components to a minimum of (a, a). </summary>
        static public Vector2 ClampMin(this Vector2 t, float a)
        => t.ClampMin(a, a);
        /// <summary> Clamps this vector's components to a minimum of (a.x, a.y). </summary>
        static public Vector2 ClampMin(this Vector2 t, Vector2 a)
        => t.ClampMin(a.x, a.y);
        /// <summary> Clamps this vector's components to a maximum of (x, y). </summary>
        static public Vector2 ClampMax(this Vector2 t, float x, float y)
        => new Vector2(t.x.ClampMax(x), t.y.ClampMax(y));
        /// <summary> Clamps this vector's components to a maximum of (a, a). </summary>
        static public Vector2 ClampMax(this Vector2 t, float a)
        => t.ClampMax(a, a);
        /// <summary> Clamps this vector's components to a maximum of (a.x, a.y). </summary>
        static public Vector2 ClampMax(this Vector2 t, Vector2 a)
        => t.ClampMax(a.x, a.y);
        /// <summary> Clamps this vector's components between (a, a) and (b, b).</summary>
        static public Vector2 Clamp(this Vector2 t, float a, float b)
        => new Vector2(t.x.Clamp(a, b), t.y.Clamp(a, b));
        /// <summary> Clamps this vector's components between (a.x, a.y) and (b.x, b.y).</summary>
        static public Vector2 Clamp(this Vector2 t, Vector2 a, Vector2 b)
        => new Vector2(t.x.Clamp(a.x, b.x), t.y.Clamp(a.y, b.y));
        /// <summary> Clamps this vector's components between (0, 0) and (1, 1).</summary>
        static public Vector2 Clamp01(this Vector2 t)
        => t.Clamp(0f, 1f);

        /// <summary> Lerps from this vector to (a, a) at alpha b. </summary>
        static public Vector2 Lerp(this Vector2 t, float a, float b)
        => new Vector2(t.x.Lerp(a, b), t.y.Lerp(a, b));
        /// <summary> Lerps from this vector to a at alpha b. </summary>
        static public Vector2 Lerp(this Vector2 t, Vector2 a, float b)
        => new Vector2(t.x.Lerp(a.x, b), t.y.Lerp(a.y, b));
        /// <summary> Lerps from this vector to (a, a) at alpha b (clamped between 0 and 1). </summary>
        static public Vector2 LerpClamped(this Vector2 t, float a, float b)
        => new Vector2(t.x.LerpClamped(a, b), t.y.LerpClamped(a, b));
        /// <summary> Lerps from this vector to a at alpha b (clamped between 0 and 1). </summary>
        static public Vector2 LerpClamped(this Vector2 t, Vector2 a, float b)
        => new Vector2(t.x.LerpClamped(a.x, b), t.y.LerpClamped(a.y, b));
        /// <summary> Maps this vector from the range [a, b] to [c, d]. </summary>
        static public Vector2 Map(this Vector2 t, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        => new Vector2(t.x.Map(a.x, b.x, c.x, d.x), t.y.Map(a.y, b.y, c.y, d.y));
        /// <summary> Maps this vector from the range [a, b] to [c, d] (with clamped output). </summary>
        static public Vector2 MapClamped(this Vector2 t, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        => new Vector2(t.x.MapClamped(a.x, b.x, c.x, d.x), t.y.MapClamped(a.y, b.y, c.y, d.y));
        /// <summary> Maps this vector from the range [a, b] to [0, 1]. </summary>
        static public Vector2 MapTo01(this Vector2 t, Vector2 a, Vector2 b)
        => new Vector2(t.x.MapTo01(a.x, b.x), t.y.MapTo01(a.y, b.y));
        /// <summary> Maps this vector from the range [0, 1] to [a, b]. </summary>
        static public Vector2 MapFrom01(this Vector2 t, Vector2 a, Vector2 b)
        => new Vector2(t.x.MapFrom01(a.x, b.x), t.y.MapFrom01(a.y, b.y));

        /// <summary> Tests whether any of this vector's components is NaN. </summary>
        static public bool AnyNaN(this Vector2 t)
        => t.x.IsNaN() || t.y.IsNaN();
        /// <summary> Tests whether any of this vector's components is infinity. </summary>
        static public bool AnyInf(this Vector2 t)
        => t.x.IsInf() || t.y.IsInf();
        /// <summary> Tests whether all of this vector's components are NaN. </summary>
        static public bool AllNaN(this Vector2 t)
        => t.x.IsNaN() && t.y.IsNaN();
        /// <summary> Tests whether all of this vector's components are infinity. </summary>
        static public bool AllInf(this Vector2 t)
        => t.x.IsInf() && t.y.IsInf();



        //------------------------------------------------------------------------------------------------------------------------------- Vector3

        /// <summary> Adds (x, y, z) to this vector's components. </summary>
        static public Vector3 Add(this Vector3 t, float x, float y, float z)
        => new Vector3(t.x.Add(x), t.y.Add(y), t.z.Add(z));
        /// <summary> Adds (a, a, a) to this vector's components. </summary>
        static public Vector3 Add(this Vector3 t, float a)
        => t.Add(a, a, a);
        /// <summary> Adds (a.x, a.y, a.z) to this vector's components. </summary>
        static public Vector3 Add(this Vector3 t, Vector3 a)
        => t.Add(a.x, a.y, a.z);
        /// <summary> Subtracts (x, y, z) from this vector's components. </summary>
        static public Vector3 Sub(this Vector3 t, float x, float y, float z)
        => new Vector3(t.x.Sub(x), t.y.Sub(y), t.z.Sub(z));
        /// <summary> Subtracts (a, a, a) from this vector's components. </summary>
        static public Vector3 Sub(this Vector3 t, float a)
        => t.Sub(a, a, a);
        /// <summary> Subtracts (a.x, a.y, a.z) from this vector's components. </summary>
        static public Vector3 Sub(this Vector3 t, Vector3 a)
        => t.Sub(a.x, a.y, a.z);
        /// <summary> Multiplies this vector's components by (x, y). </summary>
        static public Vector3 Mul(this Vector3 t, float x, float y, float z)
        => new Vector3(t.x.Mul(x), t.y.Mul(y), t.z.Mul(z));
        /// <summary> Multiplies this vector's components by (a, a, a). </summary>
        static public Vector3 Mul(this Vector3 t, float a)
        => t.Mul(a, a, a);
        /// <summary> Multiplies this vector's components by (a.x, a.y, a.z). </summary>
        static public Vector3 Mul(this Vector3 t, Vector3 a)
        => t.Mul(a.x, a.y, a.z);
        /// <summary> Divides this vector's components by (x, y). </summary>
        static public Vector3 Div(this Vector3 t, float x, float y, float z)
        => new Vector3(t.x.Div(x), t.y.Div(y), t.z.Div(z));
        /// <summary> Divides this vector's components by (a, a, a). </summary>
        static public Vector3 Div(this Vector3 t, float a)
        => t.Div(a, a, a);
        /// <summary> Divides this vector's components by (a.x, a.y, a.z). </summary>
        static public Vector3 Div(this Vector3 t, Vector3 a)
        => t.Div(a.x, a.y, a.z);
        /// <summary> Raises this vector's components to the power of (x, y). </summary>
        static public Vector3 Pow(this Vector3 t, float x, float y, float z)
        => new Vector3(t.x.Pow(x), t.y.Pow(y), t.z.Pow(z));
        /// <summary> Raises this vector's components to the power of (a, a, a). </summary>
        static public Vector3 Pow(this Vector3 t, float a)
        => t.Pow(a, a, a);
        /// <summary> Raises this vector's components to the power of (a.x, a.y, a.z). </summary>
        static public Vector3 Pow(this Vector3 t, Vector3 a)
        => t.Pow(a.x, a.y, a.z);
        /// <summary> Returns the remainder of this vector's components divided by (x, y). </summary>
        static public Vector3 Mod(this Vector3 t, float x, float y, float z)
        => new Vector3(t.x.Mod(x), t.y.Mod(y), t.z.Mod(z));
        /// <summary> Returns the remainder of this vector's components divided by (a, a, a). </summary>
        static public Vector3 Mod(this Vector3 t, float a)
        => t.Mod(a, a, a);
        /// <summary> Returns the remainder of this vector's components divided by (a.x, a.y, a.z). </summary>
        static public Vector3 Mod(this Vector3 t, Vector3 a)
        => t.Mod(a.x, a.y, a.z);
        /// <summary> Returns the positive remainder of this vector's components divided by (x, y). </summary>
        static public Vector3 PosMod(this Vector3 t, float x, float y, float z)
        => new Vector3(t.x.PosMod(x), t.y.PosMod(y), t.z.PosMod(z));
        /// <summary> Returns the positive remainder of this vector's components divided by (a, a, a). </summary>
        static public Vector3 PosMod(this Vector3 t, float a)
        => t.PosMod(a, a, a);
        /// <summary> Returns the positive remainder of this vector's components divided by (a.x, a.y, a.z). </summary>
        static public Vector3 PosMod(this Vector3 t, Vector3 a)
        => t.PosMod(a.x, a.y, a.z);

        /// <summary> Multiplies this vector's components by -1. </summary>
        static public Vector3 Neg(this Vector3 t)
        => new Vector3(t.x.Neg(), t.y.Neg(), t.z.Neg());
        /// <summary> Divides 1 by this vector's components. </summary>
        static public Vector3 Inv(this Vector3 t)
        => new Vector3(t.x.Inv(), t.y.Inv(), t.z.Inv());
        /// <summary> Removes this vector's components' sign. </summary>
        static public Vector3 Abs(this Vector3 t)
        => new Vector3(t.x.Abs(), t.y.Abs(), t.z.Abs());
        /// <summary> Returns this vector's components' sign. </summary>
        static public Vector3 Sig(this Vector3 t)
        => new Vector3(t.x.Sig(), t.y.Sig(), t.z.Sig());

        /// <summary> Rounds this value to the nearest integer. </summary>
        static public Vector3Int Round(this Vector3 t)
        => new Vector3Int(t.x.Round(), t.y.Round(), t.z.Round());
        /// <summary> Rounds this value to the smaller integer. </summary>
        static public Vector3Int RoundDown(this Vector3 t)
        => new Vector3Int(t.x.RoundDown(), t.y.RoundDown(), t.z.RoundDown());
        /// <summary> Rounds this value to the greater integer. </summary>
        static public Vector3Int RoundUp(this Vector3 t)
        => new Vector3Int(t.x.RoundUp(), t.y.RoundUp(), t.z.RoundUp());
        /// <summary> Rounds this vector's components to the nearest multiple of (x, y). </summary>
        static public Vector3Int RoundToMultiple(this Vector3 t, int x, int y, int z)
        => new Vector3Int(t.x.RoundToMultiple(x), t.y.RoundToMultiple(y), t.z.RoundToMultiple(z));
        /// <summary> Rounds this vector's components to the nearest multiple of (a, a, a). </summary>
        static public Vector3Int RoundToMultiple(this Vector3 t, int a)
        => t.RoundToMultiple(a, a, a);
        /// <summary> Rounds this vector's components to the nearest multiple of (a.x, a.y, a.z). </summary>
        static public Vector3Int RoundToMultiple(this Vector3 t, Vector3Int a)
        => t.RoundToMultiple(a.x, a.y, a.z);
        /// <summary> Rounds this vector's components to the nearest multiple of (x, y). </summary>
        static public Vector3 RoundToMultiple(this Vector3 t, float x, float y, float z)
        => new Vector3(t.x.RoundToMultiple(x), t.y.RoundToMultiple(y), t.z.RoundToMultiple(z));
        /// <summary> Rounds this vector's components to the nearest multiple of (a, a, a). </summary>
        static public Vector3 RoundToMultiple(this Vector3 t, float a)
        => t.RoundToMultiple(a, a, a);
        /// <summary> Rounds this vector's components to the nearest multiple of (a.x, a.y, a.z). </summary>
        static public Vector3 RoundToMultiple(this Vector3 t, Vector3 a)
        => t.RoundToMultiple(a.x, a.y, a.z);

        /// <summary> Returns the smaller between this vector's components and (x, y). </summary>
        static public Vector3 Min(this Vector3 t, float x, float y, float z)
        => new Vector3(t.x.Min(x), t.y.Min(y), t.z.Min(z));
        /// <summary> Returns the smaller between this vector's components and (a, a, a). </summary>
        static public Vector3 Min(this Vector3 t, float a)
        => t.Min(a, a, a);
        /// <summary> Returns the smaller between this vector's components and (a.x, a.y, a.z). </summary>
        static public Vector3 Min(this Vector3 t, Vector3 a)
        => t.Min(a.x, a.y, a.z);
        /// <summary> Returns the greater between this vector's components and (x, y). </summary>
        static public Vector3 Max(this Vector3 t, float x, float y, float z)
        => new Vector3(t.x.Max(x), t.y.Max(y), t.z.Max(z));
        /// <summary> Returns the greater between this vector's components and (a, a, a). </summary>
        static public Vector3 Max(this Vector3 t, float a)
        => t.Max(a, a, a);
        /// <summary> Returns the greater between this vector's components and (a.x, a.y, a.z). </summary>
        static public Vector3 Max(this Vector3 t, Vector3 a)
        => t.Max(a.x, a.y, a.z);
        /// <summary> Returns the average of this vector's components and (x, y). </summary>
        static public Vector3 Avg(this Vector3 t, float x, float y, float z)
        => new Vector3(t.x.Avg(x), t.y.Avg(y), t.y.Avg(z));
        /// <summary> Returns the average of this vector's components and (a, a, a). </summary>
        static public Vector3 Avg(this Vector3 t, float a)
        => t.Avg(a, a, a);
        /// <summary> Returns the average of this vector's components and (a.x, a.y, a.z). </summary>
        static public Vector3 Avg(this Vector3 t, Vector3 a)
        => t.Avg(a.x, a.y, a.z);

        /// <summary> Clamps this vector's components to a minimum of (x, y). </summary>
        static public Vector3 ClampMin(this Vector3 t, float x, float y, float z)
        => new Vector3(t.x.ClampMin(x), t.y.ClampMin(y), t.z.ClampMin(z));
        /// <summary> Clamps this vector's components to a minimum of (a, a, a). </summary>
        static public Vector3 ClampMin(this Vector3 t, float a)
        => t.ClampMin(a, a, a);
        /// <summary> Clamps this vector's components to a minimum of (a.x, a.y, a.z). </summary>
        static public Vector3 ClampMin(this Vector3 t, Vector3 a)
        => t.ClampMin(a.x, a.y, a.z);
        /// <summary> Clamps this vector's components to a maximum of (x, y). </summary>
        static public Vector3 ClampMax(this Vector3 t, float x, float y, float z)
        => new Vector3(t.x.ClampMax(x), t.y.ClampMax(y), t.z.ClampMax(z));
        /// <summary> Clamps this vector's components to a maximum of (a, a, a). </summary>
        static public Vector3 ClampMax(this Vector3 t, float a)
        => t.ClampMax(a, a, a);
        /// <summary> Clamps this vector's components to a maximum of (a.x, a.y, a.z). </summary>
        static public Vector3 ClampMax(this Vector3 t, Vector3 a)
        => t.ClampMax(a.x, a.y, a.z);
        /// <summary> Clamps this vector's components between (a, a, a) and (b, b, b).</summary>
        static public Vector3 Clamp(this Vector3 t, float a, float b)
        => new Vector3(t.x.Clamp(a, b), t.y.Clamp(a, b), t.z.Clamp(a, b));
        /// <summary> Clamps this vector's components between (a.x, a.y, a.z) and (b.x, b.y, b.z).</summary>
        static public Vector3 Clamp(this Vector3 t, Vector3 a, Vector3 b)
        => new Vector3(t.x.Clamp(a.x, b.x), t.y.Clamp(a.y, b.y), t.z.Clamp(a.z, b.z));
        /// <summary> Clamps this vector's components between (0, 0, 0) and (1, 1, 1).</summary>
        static public Vector3 Clamp01(this Vector3 t)
        => t.Clamp(0f, 1f);

        /// <summary> Lerps from this vector to (a, a, a) at alpha b. </summary>
        static public Vector3 Lerp(this Vector3 t, float a, float b)
        => new Vector3(t.x.Lerp(a, b), t.y.Lerp(a, b), t.z.Lerp(a, b));
        /// <summary> Lerps from this vector to a at alpha b. </summary>
        static public Vector3 Lerp(this Vector3 t, Vector3 a, float b)
        => new Vector3(t.x.Lerp(a.x, b), t.y.Lerp(a.y, b), t.z.Lerp(a.z, b));
        /// <summary> Lerps from this vector to (a, a, a) at alpha b (clamped between 0 and 1). </summary>
        static public Vector3 LerpClamped(this Vector3 t, float a, float b)
        => new Vector3(t.x.LerpClamped(a, b), t.y.LerpClamped(a, b), t.z.LerpClamped(a, b));
        /// <summary> Lerps from this vector to a at alpha b (clamped between 0 and 1). </summary>
        static public Vector3 LerpClamped(this Vector3 t, Vector3 a, float b)
        => new Vector3(t.x.LerpClamped(a.x, b), t.y.LerpClamped(a.y, b), t.z.LerpClamped(a.z, b));
        /// <summary> Maps this vector from the range [a, b] to [c, d]. </summary>
        static public Vector3 Map(this Vector3 t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        => new Vector3(t.x.Map(a.x, b.x, c.x, d.x), t.y.Map(a.y, b.y, c.y, d.y), t.z.Map(a.z, b.z, c.z, d.z));
        /// <summary> Maps this vector from the range [a, b] to [c, d] (with clamped output). </summary>
        static public Vector3 MapClamped(this Vector3 t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        => new Vector3(t.x.MapClamped(a.x, b.x, c.x, d.x), t.y.MapClamped(a.y, b.y, c.y, d.y), t.z.MapClamped(a.z, b.z, c.z, d.z));
        /// <summary> Maps this vector from the range [a, b] to [0, 1]. </summary>
        static public Vector3 MapTo01(this Vector3 t, Vector3 a, Vector3 b)
        => new Vector3(t.x.MapTo01(a.x, b.x), t.y.MapTo01(a.y, b.y), t.z.MapTo01(a.z, b.z));
        /// <summary> Maps this vector from the range [a, b] to [0, 1]. </summary>
        static public Vector3 MapFrom01(this Vector3 t, Vector3 a, Vector3 b)
        => new Vector3(t.x.MapFrom01(a.x, b.x), t.y.MapFrom01(a.y, b.y), t.z.MapFrom01(a.z, b.z));

        /// <summary> Tests whether any of this vector's components is NaN. </summary>
        static public bool AnyNaN(this Vector3 t)
        => t.x.IsNaN() || t.y.IsNaN() || t.z.IsNaN();
        /// <summary> Tests whether any of this vector's components is infinity. </summary>
        static public bool AnyInf(this Vector3 t)
        => t.x.IsInf() || t.y.IsInf() || t.z.IsInf();
        /// <summary> Tests whether all of this vector's components are NaN. </summary>
        static public bool AllNaN(this Vector3 t)
        => t.x.IsNaN() && t.y.IsNaN() && t.z.IsNaN();
        /// <summary> Tests whether all of this vector's components are infinity. </summary>
        static public bool AllInf(this Vector3 t)
        => t.x.IsInf() && t.y.IsInf() && t.z.IsInf();
    }
}