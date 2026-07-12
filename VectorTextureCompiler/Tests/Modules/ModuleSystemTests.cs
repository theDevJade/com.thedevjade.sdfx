using System;
using NUnit.Framework;
using SDFX.VectorTextureCompiler.Core.Modules;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Tests.Modules
{
    public sealed class ModuleSystemTests
    {
        [Test]
        public void SanitizeId_NormalizesCaseAndInvalidCharacters()
        {
            Assert.AreEqual("my_module_", SdfxModuleDefinition.SanitizeId("My Module!"));
            Assert.AreEqual("outline", SdfxModuleDefinition.SanitizeId("  Outline  "));
            Assert.AreEqual("my_module", SdfxModuleDefinition.SanitizeId("My-Module"));
        }

        [Test]
        public void SanitizeId_Empty_ReturnsDefault()
        {
            Assert.AreEqual("custommodule", SdfxModuleDefinition.SanitizeId(""));
            Assert.AreEqual("custommodule", SdfxModuleDefinition.SanitizeId("   "));
        }

        [Test]
        public void SanitizeId_LeadingDigit_PrefixesM()
        {
            Assert.AreEqual("m_2tone", SdfxModuleDefinition.SanitizeId("2tone"));
        }

        [Test]
        public void ModuleProperty_Range_EmitsShaderLabAndHlsl()
        {
            var prop = ModuleProperty.Range("_Glow", "Glow", 0f, 2f, 0.5f);
            Assert.AreEqual("Range(0, 2)", prop.ShaderLabType);
            StringAssert.Contains("_Glow (\"Glow\", Range(0, 2)) = 0.5", prop.ToShaderLabLine());
            Assert.AreEqual("float _Glow;", prop.ToHlslDeclaration());
        }

        [Test]
        public void ModuleProperty_Enum_EmitsShaderLabWhenSevenOrFewer()
        {
            var prop = ModuleProperty.Enum("_Mode", "Mode", new[] { "A", "B", "C" }, 1);
            StringAssert.StartsWith("[Enum(", prop.Attributes);
            Assert.AreEqual("1", prop.DefaultValue);
            Assert.AreEqual("float _Mode;", prop.ToHlslDeclaration());
        }

        [Test]
        public void ModuleProperty_Enum_SkipsShaderLabAttributeWhenTooLarge()
        {
            var labels = new[] { "A", "B", "C", "D", "E", "F", "G", "H" };
            var prop = ModuleProperty.Enum("_Big", "Big", labels);
            Assert.AreEqual(string.Empty, prop.Attributes);
            Assert.AreEqual(ModuleProperty.MaxShaderLabEnumEntries, 7);
        }

        [Test]
        public void PropertyDefinition_ToModuleProperty_MapsKinds()
        {
            var range = new SdfxModulePropertyDefinition
            {
                Name = "_Strength",
                DisplayName = "Strength",
                Kind = ModulePropertyKind.Range,
                RangeMin = 0f,
                RangeMax = 1f,
                DefaultFloat = 0.25f
            }.ToModuleProperty();
            Assert.AreEqual(ModulePropertyKind.Range, range.Kind);
            Assert.AreEqual("0.25", range.DefaultValue);

            var color = new SdfxModulePropertyDefinition
            {
                Name = "_Tint",
                Kind = ModulePropertyKind.Color,
                DefaultColor = new Color(1f, 0f, 0f, 1f),
                Attributes = "[HDR]"
            }.ToModuleProperty();
            Assert.AreEqual(ModulePropertyKind.Color, color.Kind);
            StringAssert.Contains("[HDR]", color.Attributes);

            var tex = new SdfxModulePropertyDefinition
            {
                Name = "_Mask",
                Kind = ModulePropertyKind.Texture2D,
                DefaultTexture = "white"
            }.ToModuleProperty();
            Assert.AreEqual(ModulePropertyKind.Texture2D, tex.Kind);
            Assert.AreEqual("sampler2D _Mask;", tex.ToHlslDeclaration());
        }

        [Test]
        public void PropertyDefinition_EmptyName_Throws()
        {
            var def = new SdfxModulePropertyDefinition { Name = "  " };
            Assert.Throws<InvalidOperationException>(() => def.ToModuleProperty());
        }

        [Test]
        public void Registry_FindAndPresets_ResolveBuiltIns()
        {
            ShaderModuleRegistry.ResetBuiltIns();

            Assert.IsNotNull(ShaderModuleRegistry.Find("toon"));
            Assert.IsNotNull(ShaderModuleRegistry.FindPreset("avatar"));
            var avatarIds = ShaderModuleRegistry.ResolvePreset("avatar");
            Assert.IsNotNull(avatarIds);
            CollectionAssert.Contains(avatarIds, "toon");
            Assert.Greater(ShaderModuleRegistry.All.Count, 10);
        }

        [Test]
        public void Registry_ValidateSelection_ReportsDiffuseConflicts()
        {
            ShaderModuleRegistry.ResetBuiltIns();
            var warnings = ShaderModuleRegistry.ValidateSelection(new[] { "toon", "pbr" });
            Assert.Greater(warnings.Count, 0);
            StringAssert.Contains("conflicts", warnings[0]);
        }

        [Test]
        public void Registry_Resolve_FiltersByLodTier()
        {
            ShaderModuleRegistry.ResetBuiltIns();
            var all = ShaderModuleRegistry.Resolve(null, maxLodTier: 0);
            var limited = ShaderModuleRegistry.Resolve(null, maxLodTier: 1);
            Assert.GreaterOrEqual(all.Count, limited.Count);
            foreach (var module in limited)
            {
                Assert.LessOrEqual(module.LodTier, 1);
            }
        }
    }
}
