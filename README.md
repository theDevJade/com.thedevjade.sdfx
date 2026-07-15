# SDFX

Editor-time GPU-native **vector texture compiler** for Unity Built-in Render Pipeline (BIRP) workflows.

**License:** [MIT](LICENSE)

## Requirements

- Unity **2022.3** or newer
- Built-in Render Pipeline (BIRP)
- Dependency: [Unity Vector Graphics](https://docs.unity3d.com/Packages/com.unity.vectorgraphics@latest) (`com.unity.vectorgraphics`)

## Install from Git

1. Open your Unity project.
2. **Window > Package Manager**.
3. Click **+** (top left) > **Add package from git URL…**
4. Paste:

```text
https://github.com/theDevJade/com.thedevjade.sdfx.git
```

5. Click **Add** and wait for import to finish.

## What it does

At a high level, SDFX takes something with vector shape data and turns it into a GPU BIRP shader/material to render it.

We start with an SVG, custom text, or a raster texture.
If it's a regular image/texture, we can optionally run it through a rasterizer that converts it into a clean SVG using multiple algorithms.
Then we process it like this:
Parse > Simplify > Boolean operations > Quantize > Build spatial grid
Finally, we bake the data into textures + generate HLSL shader code (with optional modules), which gets compiled into a BIRP material or a CompiledVectorTextureAsset.

There are really two tools living in one package:

- **Vector Texture Compiler** is the main product. It parses your source, optimizes and packs the resulting primitives, bakes them into data textures, and generates a modular BIRP shader to read them back at render time.
- **Rasterizer** is the on-ramp for people who don't already have vector art. It turns a raster texture into an SVG using whichever of several algorithms fits best, so you can feed that SVG straight into the compiler.

Both have their own window under **SDFX** in the Unity menu bar.

## The Shader Module System
Instead of writing a new shader for every possible combination of features, SDFX uses a modular plugin system. Each feature is a compile-time module that gets injected into the final shader only when you enable it.
Every module has:

- A unique ID
- A display name and category
- An order (so they stack predictably)
- Optional properties (sliders, colors, textures, etc.)
- Conflict rules (e.g. two different lighting models can’t both be active)

You add behavior by writing small HLSL snippets for different hooks such as Vertex, UV, Fragment, etc.

Three ways to create modules:

### ScriptableObject (easiest)
Create one via Create > SDFX > Shader Module Definition. Point it to your .hlsl files and define its properties in the inspector. Great for custom effects, just PLEASE set the order to 800+ so it runs after the built-ins.
### C# Classes
Inherit from ShaderModule and add [SdfxModule]. The system auto-detects them on domain reload.
Built-ins
SDFX ships with ~40 ready-made modules (PBR, Toon, Rim Light, Outline, Dissolve, VRChat audio reactivity, etc.). There are also presets (Avatar, World, UI, Toon, PBR, etc.) that enable a whole set of modules with one click.