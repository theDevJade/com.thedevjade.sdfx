using System;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core
{
    [CreateAssetMenu(menuName = "SDFX/Compiled Vector Texture Asset", fileName = "CompiledVectorTextureAsset")]
    public sealed class CompiledVectorTextureAsset : ScriptableObject
    {
        public string sourcePath;
        public Primitive[] primitives = Array.Empty<Primitive>();
        public Texture2D primitiveDataTexture;
        public Texture2D gridLookupTexture;
        public Texture2D gridIndexTexture;
        public Texture2D pathDataTexture;
        public Texture2D msdfTexture;
        public Material material;
        public CompileReport compileReport = new CompileReport();
    }
}