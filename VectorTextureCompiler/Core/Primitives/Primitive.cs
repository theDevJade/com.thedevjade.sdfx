using System;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Primitives
{
    [Serializable]
    public struct Primitive
    {
        public PrimitiveType Type;
        public Vector2 Position;
        public Vector2 Size;
        public float RotationDegrees;
        public Color Color;

        public int ParameterIndex;

        public int ParameterCount;

        public float StrokeRadius;

        /// <summary>
        /// Gradient run reference into the path data texture, encoded as start+1
        /// so the struct default (0) means "solid color, no gradient".
        /// </summary>
        public int GradientIndex;

        public float Softness;
        public byte Layer;
    }
}
