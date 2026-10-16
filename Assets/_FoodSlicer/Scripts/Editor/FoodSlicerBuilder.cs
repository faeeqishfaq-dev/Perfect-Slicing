using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FoodSlicer.EditorTools
{
    public static class FoodSlicerBuilder
    {
        private const string ROOT_PATH = "Assets/_FoodSlicer";
        private const string BUILD_KEY = "FoodSlicer_AutoRebuild_TargetVisuals_v6";

        [InitializeOnLoadMethod]
        private static void AutoRunOnEditorLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(BUILD_KEY, false))
                {
                    EditorPrefs.SetBool(BUILD_KEY, true);
                    BuildCompleteGame();
                }
            };
        }

        [MenuItem("FoodSlicer/Build, Validate and Capture")]
        public static void BuildValidateAndCapture()
        {
            BuildCompleteGame();
            FoodSlicerValidator.ValidateCompleteProject();
            FoodSlicerScreenCapture.CaptureGameplayView();
        }

        [MenuItem("FoodSlicer/Build Complete Game (Target Visuals)")]
        public static void BuildCompleteGame()
        {
            Debug.Log("<color=cyan><b>[FoodSlicer] Rebuilding Game to match Target Visual Reference (Image 2)...</b></color>");

            EnsureDirectories();
            Shader litShader = FindURPShader();
            Shader unlitShader = FindURPUnlitShader();

            // 1. Create Procedural Textures & Sprites
            var (textures, sprites) = CreateTexturesAndSprites();

            // 2. Create Materials
            var materials = CreateMaterials(litShader, unlitShader, textures);

            // 3. Create Meshes
            var meshes = CreateAllMeshes();

            // 4. Create VFX Prefabs
            var vfxPrefabs = CreateVFXPrefabs(materials["Mat_Particle_Unlit"]);

            // 5. Create Food Slice & Whole Prefabs
            var foodPrefabs = CreateFoodPrefabs(materials, meshes);

            // 6. Create Knife Prefabs
            var knifePrefabs = CreateKnifePrefabs(materials, meshes);

            // 7. Create UI Floating Text Prefab
            var floatingTextPrefab = CreateFloatingTextPrefab(sprites.uiCircle);

            // 8. Create ScriptableObjects (FoodData & KnifeData)
            var foodDataList = CreateFoodDataAssets(foodPrefabs, sprites);
            var knifeDataList = CreateKnifeDataAssets(knifePrefabs, sprites);

            // 9. Create 20 Level Data ScriptableObjects (Level 1 tailored to match Image 2)
            var levelDataList = Create20LevelDataAssets(foodDataList);

            // 10. Build Gameplay Scene
            BuildGameplayScene(knifeDataList, levelDataList, vfxPrefabs, floatingTextPrefab, materials, meshes, sprites);

            // 11. Build Main Menu Scene
            BuildMainMenuScene(knifeDataList, materials, meshes, sprites);

            // 12. Update Build Settings
            UpdateBuildSettings();

            // 13. Copy to Resources
            CopyAssetsToResources();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[FoodSlicer] Complete Production-Ready Game Rebuilt Successfully! Target Visual Reference Matched!</b></color>");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = new string[]
            {
                $"{ROOT_PATH}/Art/Food",
                $"{ROOT_PATH}/Art/Knives",
                $"{ROOT_PATH}/Art/Environment",
                $"{ROOT_PATH}/Art/Textures",
                $"{ROOT_PATH}/Art/UI",
                $"{ROOT_PATH}/Art/Materials",
                $"{ROOT_PATH}/Prefabs/Food",
                $"{ROOT_PATH}/Prefabs/Knives",
                $"{ROOT_PATH}/Prefabs/Environment",
                $"{ROOT_PATH}/Prefabs/UI",
                $"{ROOT_PATH}/Prefabs/VFX",
                $"{ROOT_PATH}/Scenes",
                $"{ROOT_PATH}/ScriptableObjects/Foods",
                $"{ROOT_PATH}/ScriptableObjects/Knives",
                $"{ROOT_PATH}/ScriptableObjects/Levels",
                $"{ROOT_PATH}/Resources/Foods",
                $"{ROOT_PATH}/Resources/Knives",
                $"{ROOT_PATH}/Resources/Levels"
            };

            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
        }

        private static void CopyAssetsToResources()
        {
            void CopyAll(string src, string dst)
            {
                if (!Directory.Exists(src)) return;
                if (!Directory.Exists(dst)) Directory.CreateDirectory(dst);
                string[] files = Directory.GetFiles(src, "*.asset");
                foreach (var f in files)
                {
                    string fileName = Path.GetFileName(f);
                    File.Copy(f, Path.Combine(dst, fileName), true);
                }
            }

            CopyAll($"{ROOT_PATH}/ScriptableObjects/Foods", $"{ROOT_PATH}/Resources/Foods");
            CopyAll($"{ROOT_PATH}/ScriptableObjects/Knives", $"{ROOT_PATH}/Resources/Knives");
            CopyAll($"{ROOT_PATH}/ScriptableObjects/Levels", $"{ROOT_PATH}/Resources/Levels");
        }

        private static Shader FindURPShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return shader;
        }

        private static Shader FindURPUnlitShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            return shader;
        }

        #region Textures & Sprites Creation
        private static (Dictionary<string, Texture2D> textures, (Sprite uiCircle, Sprite uiPillBg, Sprite uiPillFill, Sprite uiComboFrame, Sprite uiButton, Sprite uiCheckmark, Dictionary<FoodType, Sprite> foodIcons) sprites) CreateTexturesAndSprites()
        {
            var textures = new Dictionary<string, Texture2D>();
            var foodIcons = new Dictionary<FoodType, Sprite>();

            Texture2D CreateTexture(string name, int width, int height, Action<Texture2D> painter, bool isSprite = false, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
            {
                string folder = isSprite ? "UI" : "Textures";
                string path = $"{ROOT_PATH}/Art/{folder}/{name}.png";
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                painter(tex);
                tex.Apply();

                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = isSprite ? TextureImporterType.Sprite : TextureImporterType.Default;
                    if (isSprite) importer.spritePixelsPerUnit = 100;
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = wrapMode;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }

                Texture2D loadedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                textures[name] = loadedTex;
                return loadedTex;
            }

            Sprite CreateTextureSprite(string name, int width, int height, Action<Texture2D> painter)
            {
                CreateTexture(name, width, height, painter, true);
                string path = $"{ROOT_PATH}/Art/UI/{name}.png";
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            // 1. Tex_BackgroundGradient (Pink/Magenta Top -> Soft Pink Mid -> Light Peach/Yellow Bottom matching Image 2)
            CreateTexture("Tex_BackgroundGradient", 256, 512, tex =>
            {
                Color topMagenta = new Color(0.96f, 0.10f, 0.62f);  // Vibrant saturated hot pink/magenta
                Color midPink = new Color(0.98f, 0.52f, 0.72f);     // Soft vibrant pink
                Color warmPeach = new Color(1.0f, 0.74f, 0.48f);   // Apricot / Peach
                Color botYellow = new Color(1.0f, 0.88f, 0.36f);   // Sunny warm golden yellow

                for (int y = 0; y < 512; y++)
                {
                    float t = (float)y / 511f;
                    Color rowColor;
                    if (t < 0.30f)
                        rowColor = Color.Lerp(botYellow, warmPeach, t / 0.30f);
                    else if (t < 0.65f)
                        rowColor = Color.Lerp(warmPeach, midPink, (t - 0.30f) / 0.35f);
                    else
                        rowColor = Color.Lerp(midPink, topMagenta, (t - 0.65f) / 0.35f);

                    for (int x = 0; x < 256; x++)
                    {
                        tex.SetPixel(x, y, rowColor);
                    }
                }
            });

            // 2. Tex_BananaCutFace (Outer yellow peel + creamy pale ivory flesh + 3 carpel seed core)
            CreateTexture("Tex_BananaCutFace", 256, 256, tex =>
            {
                Vector2 center = new Vector2(128, 128);
                Color peelColor = new Color(1.0f, 0.88f, 0.12f);
                Color fleshColor = new Color(0.98f, 0.96f, 0.84f);
                Color coreColor = new Color(0.86f, 0.82f, 0.68f);
                Color seedColor = new Color(0.35f, 0.28f, 0.18f);

                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist > 106f)
                        {
                            // Outer yellow peel rim
                            tex.SetPixel(x, y, peelColor);
                        }
                        else if (dist > 94f)
                        {
                            // Smooth transition from peel to flesh
                            float t = (dist - 94f) / 12f;
                            tex.SetPixel(x, y, Color.Lerp(fleshColor, peelColor, t));
                        }
                        else
                        {
                            // Creamy pale flesh with central 3-point star carpel markings
                            Color col = fleshColor;
                            if (dist < 32f)
                            {
                                float angle = Mathf.Atan2(y - center.y, x - center.x);
                                float starFactor = Mathf.Cos(angle * 3f);
                                float starDist = 18f + starFactor * 8f;
                                if (dist < starDist)
                                {
                                    col = Color.Lerp(fleshColor, coreColor, 0.75f);
                                }
                                // Tiny central dark seeds at 3 carpel lobes
                                for (int s = 0; s < 3; s++)
                                {
                                    float sAngle = s * Mathf.PI * 2f / 3f;
                                    Vector2 sPos = center + new Vector2(Mathf.Cos(sAngle), Mathf.Sin(sAngle)) * 13f;
                                    if (Vector2.Distance(new Vector2(x, y), sPos) < 4.5f)
                                    {
                                        col = seedColor;
                                    }
                                }
                            }
                            tex.SetPixel(x, y, col);
                        }
                    }
                }
            });

            // 3. Tex_Watermelon (Green body with dark green wavy vertical stripes)
            CreateTexture("Tex_Watermelon", 512, 256, tex =>
            {
                Color lightGreen = new Color(0.24f, 0.68f, 0.28f);
                Color darkGreen = new Color(0.08f, 0.32f, 0.12f);

                for (int y = 0; y < 256; y++)
                {
                    float wave1 = Mathf.Sin(y * 0.08f) * 12f;
                    float wave2 = Mathf.Cos(y * 0.15f) * 6f;
                    float wave = wave1 + wave2;

                    for (int x = 0; x < 512; x++)
                    {
                        float u = (x + wave) % 64f;
                        float stripe = Mathf.Sin(u / 64f * Mathf.PI);
                        Color c = stripe > 0.45f ? darkGreen : lightGreen;
                        tex.SetPixel(x, y, c);
                    }
                }
            }, false, TextureWrapMode.Repeat);

            // 4. Tex_WatermelonCutFace (Dark green rind + white pith + vibrant crimson flesh + black seeds)
            CreateTexture("Tex_WatermelonCutFace", 256, 256, tex =>
            {
                Vector2 center = new Vector2(128, 128);
                Color rindColor = new Color(0.10f, 0.42f, 0.16f);
                Color pithColor = new Color(0.92f, 0.96f, 0.88f);
                Color redFlesh = new Color(0.95f, 0.16f, 0.22f);
                Color seedColor = new Color(0.12f, 0.10f, 0.10f);

                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist > 118f)
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                        else if (dist > 105f)
                        {
                            tex.SetPixel(x, y, rindColor);
                        }
                        else if (dist > 94f)
                        {
                            tex.SetPixel(x, y, pithColor);
                        }
                        else
                        {
                            Color c = redFlesh;
                            // 10 seeds arranged in circle at radius 55
                            for (int s = 0; s < 10; s++)
                            {
                                float angle = s * Mathf.PI * 2f / 10f;
                                Vector2 seedPos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 55f;
                                if (Vector2.Distance(new Vector2(x, y), seedPos) < 5f)
                                {
                                    c = seedColor;
                                }
                            }
                            tex.SetPixel(x, y, c);
                        }
                    }
                }
            });

            // 5. Tex_OrangeSlice / Tex_LemonSlice (Citrus wheel with radial segments)
            CreateTexture("Tex_OrangeSlice", 256, 256, tex =>
            {
                Vector2 center = new Vector2(128, 128);
                Color rind = new Color(1.0f, 0.58f, 0.05f);
                Color pith = new Color(0.98f, 0.98f, 0.92f);
                Color pulp = new Color(1.0f, 0.70f, 0.15f);

                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist > 116f)
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                        else if (dist > 102f)
                        {
                            tex.SetPixel(x, y, rind);
                        }
                        else if (dist > 94f || dist < 14f)
                        {
                            tex.SetPixel(x, y, pith);
                        }
                        else
                        {
                            float angle = Mathf.Atan2(y - center.y, x - center.x);
                            float segmentLine = Mathf.Abs(Mathf.Sin(angle * 4.5f));
                            if (segmentLine < 0.12f)
                            {
                                tex.SetPixel(x, y, pith);
                            }
                            else
                            {
                                tex.SetPixel(x, y, pulp);
                            }
                        }
                    }
                }
            });

            CreateTexture("Tex_LemonSlice", 256, 256, tex =>
            {
                Vector2 center = new Vector2(128, 128);
                Color rind = new Color(1.0f, 0.92f, 0.08f);
                Color pith = new Color(0.98f, 0.98f, 0.92f);
                Color pulp = new Color(0.96f, 0.88f, 0.35f);

                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist > 116f)
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                        else if (dist > 102f)
                        {
                            tex.SetPixel(x, y, rind);
                        }
                        else if (dist > 94f || dist < 14f)
                        {
                            tex.SetPixel(x, y, pith);
                        }
                        else
                        {
                            float angle = Mathf.Atan2(y - center.y, x - center.x);
                            float segmentLine = Mathf.Abs(Mathf.Sin(angle * 4.5f));
                            if (segmentLine < 0.12f)
                            {
                                tex.SetPixel(x, y, pith);
                            }
                            else
                            {
                                tex.SetPixel(x, y, pulp);
                            }
                        }
                    }
                }
            });

            // Textured Citrus Rind matching Image 2 (stippled pores and brown navel)
            CreateTexture("Tex_CitrusRind", 256, 256, tex =>
            {
                Color baseOrange = new Color(1.0f, 0.72f, 0.05f);
                Color poreColor = new Color(0.92f, 0.60f, 0.02f);
                Color navelColor = new Color(0.32f, 0.20f, 0.10f);

                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float noise = Mathf.Sin(x * 0.85f) * Mathf.Cos(y * 0.85f);
                        Color c = noise > 0.6f ? poreColor : baseOrange;

                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(128, 128));
                        if (dist < 10f)
                        {
                            float star = Mathf.Cos(Mathf.Atan2(y - 128, x - 128) * 5f);
                            if (dist < 6f + star * 2.5f) c = navelColor;
                        }
                        tex.SetPixel(x, y, c);
                    }
                }
            }, false, TextureWrapMode.Repeat);

            // 6. Tex_AppleCutFace (Red skin + crisp cream interior + core seeds)
            CreateTexture("Tex_AppleCutFace", 256, 256, tex =>
            {
                Vector2 center = new Vector2(128, 128);
                Color skin = new Color(0.90f, 0.12f, 0.15f);
                Color flesh = new Color(0.98f, 0.96f, 0.86f);
                Color seed = new Color(0.28f, 0.16f, 0.10f);

                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist > 116f)
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                        else if (dist > 105f)
                        {
                            tex.SetPixel(x, y, skin);
                        }
                        else
                        {
                            Color c = flesh;
                            // 2 core seeds
                            if (Vector2.Distance(new Vector2(x, y), center + new Vector2(-12, 0)) < 6f ||
                                Vector2.Distance(new Vector2(x, y), center + new Vector2(12, 0)) < 6f)
                            {
                                c = seed;
                            }
                            tex.SetPixel(x, y, c);
                        }
                    }
                }
            });

            // 7. Tex_TomatoCutFace
            CreateTexture("Tex_TomatoCutFace", 256, 256, tex =>
            {
                Vector2 center = new Vector2(128, 128);
                Color skin = new Color(0.92f, 0.15f, 0.12f);
                Color flesh = new Color(0.85f, 0.18f, 0.15f);
                Color gel = new Color(0.92f, 0.35f, 0.18f);

                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist > 116f) tex.SetPixel(x, y, Color.clear);
                        else if (dist > 104f) tex.SetPixel(x, y, skin);
                        else if (dist < 25f) tex.SetPixel(x, y, flesh);
                        else
                        {
                            float angle = Mathf.Atan2(y - center.y, x - center.x);
                            float chambers = Mathf.Sin(angle * 2f);
                            tex.SetPixel(x, y, chambers > 0.2f ? gel : flesh);
                        }
                    }
                }
            });

            // 8. Tex_CarrotCutFace
            CreateTexture("Tex_CarrotCutFace", 256, 256, tex =>
            {
                Vector2 center = new Vector2(128, 128);
                Color outer = new Color(1.0f, 0.48f, 0.05f);
                Color core = new Color(1.0f, 0.65f, 0.15f);

                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist > 115f) tex.SetPixel(x, y, Color.clear);
                        else if (dist < 45f) tex.SetPixel(x, y, core);
                        else tex.SetPixel(x, y, outer);
                    }
                }
            });

            // 9. Tex_KnifeBlade (Polished Steel metallic reflection with dark spine bevel and razor cutting edge)
            CreateTexture("Tex_KnifeBlade", 512, 128, tex =>
            {
                Color spineBevel = new Color(0.72f, 0.78f, 0.85f);
                Color bladeBody = new Color(0.92f, 0.95f, 0.98f);
                Color sharpEdge = new Color(1.0f, 1.0f, 1.0f);

                for (int y = 0; y < 128; y++)
                {
                    float t = (float)y / 127f;
                    Color c = t > 0.85f ? Color.Lerp(bladeBody, spineBevel, (t - 0.85f) / 0.15f)
                            : t < 0.15f ? Color.Lerp(sharpEdge, bladeBody, t / 0.15f)
                            : bladeBody;

                    for (int x = 0; x < 512; x++)
                    {
                        tex.SetPixel(x, y, c);
                    }
                }
            });

            // 10. Tex_KnifeHandle (Braided diamond cord wrap pattern matching Image 2)
            CreateTexture("Tex_KnifeHandle", 256, 128, tex =>
            {
                Color cordHighlight = new Color(0.68f, 0.42f, 0.16f);
                Color cordBase = new Color(0.44f, 0.25f, 0.10f);
                Color cordShadow = new Color(0.20f, 0.10f, 0.04f);

                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float d1 = (x * 0.9f + y * 1.4f) % 28f;
                        float d2 = (x * 0.9f - y * 1.4f + 280f) % 28f;
                        bool isSeam = d1 < 3.2f || d2 < 3.2f;
                        if (isSeam)
                        {
                            tex.SetPixel(x, y, cordShadow);
                        }
                        else
                        {
                            float diamondCenter = Mathf.Min(Mathf.Abs(d1 - 14f), Mathf.Abs(d2 - 14f));
                            float t = Mathf.Clamp01(diamondCenter / 14f);
                            tex.SetPixel(x, y, Color.Lerp(cordHighlight, cordBase, t));
                        }
                    }
                }
            }, false, TextureWrapMode.Repeat);

            // ================== UI SPRITES ==================
            // Sprite_TargetBadge (Crisp pure white circular card with soft shadow for HUD matching Image 2)
            Sprite uiCircle = CreateTextureSprite("Sprite_TargetBadge", 256, 256, tex =>
            {
                Vector2 center = new Vector2(128, 128);
                float radius = 112f;
                Color cardWhite = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                Color shadowColor = new Color(0.0f, 0.0f, 0.0f, 0.22f);

                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist <= radius)
                        {
                            tex.SetPixel(x, y, cardWhite);
                        }
                        else if (dist <= radius + 8f)
                        {
                            float shadowAlpha = Mathf.Clamp01((radius + 8f - dist) / 8f) * 0.20f;
                            tex.SetPixel(x, y, new Color(0, 0, 0, shadowAlpha));
                        }
                        else
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                    }
                }
            });

            // Sprite_PillProgressBg (Rounded pill dark background for target progress bar)
            Sprite uiPillBg = CreateTextureSprite("Sprite_PillProgressBg", 256, 64, tex =>
            {
                Color bg = new Color(0.20f, 0.24f, 0.28f, 0.65f);
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        // Capsule / pill rounding at left and right
                        float leftDist = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                        float rightDist = Vector2.Distance(new Vector2(x, y), new Vector2(256 - 32, 32));

                        if (x < 32 && leftDist > 30f) tex.SetPixel(x, y, Color.clear);
                        else if (x > 256 - 32 && rightDist > 30f) tex.SetPixel(x, y, Color.clear);
                        else if (y < 2 || y > 61) tex.SetPixel(x, y, Color.clear);
                        else tex.SetPixel(x, y, bg);
                    }
                }
            });

            // Sprite_PillProgressFill (Smooth solid pill fill)
            Sprite uiPillFill = CreateTextureSprite("Sprite_PillProgressFill", 256, 64, tex =>
            {
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float leftDist = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                        float rightDist = Vector2.Distance(new Vector2(x, y), new Vector2(256 - 32, 32));

                        if (x < 32 && leftDist > 30f) tex.SetPixel(x, y, Color.clear);
                        else if (x > 256 - 32 && rightDist > 30f) tex.SetPixel(x, y, Color.clear);
                        else tex.SetPixel(x, y, Color.white);
                    }
                }
            });

            // Sprite_ComboFrame (Tapered vertical combo gauge outline matching Image 2)
            Sprite uiComboFrame = CreateTextureSprite("Sprite_ComboFrame", 128, 256, tex =>
            {
                Color border = Color.white;
                Color bgTint = new Color(1.0f, 1.0f, 1.0f, 0.12f);

                for (int y = 0; y < 256; y++)
                {
                    float t = (float)y / 255f;
                    // Taper: bottom width is 60px, top width is 100px
                    float halfW = Mathf.Lerp(30f, 52f, t);
                    float left = 64f - halfW;
                    float right = 64f + halfW;

                    for (int x = 0; x < 128; x++)
                    {
                        if (x < left || x > right)
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                        else if (x <= left + 4f || x >= right - 4f || y <= 4 || y >= 251)
                        {
                            tex.SetPixel(x, y, border); // Outer border
                        }
                        else if (y % 50 < 4) // Horizontal divider lines
                        {
                            tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.85f));
                        }
                        else
                        {
                            tex.SetPixel(x, y, bgTint);
                        }
                    }
                }
            });

            // Polished Button
            Sprite uiButton = CreateTextureSprite("Sprite_ButtonRound", 128, 64, tex =>
            {
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
            });

            // ================== HIGH QUALITY FOOD ICONS ==================
            // Apple Icon (Glossy red apple with brown stem and green leaf)
            foodIcons[FoodType.Apple] = CreateTextureSprite("Icon_Apple", 128, 128, tex =>
            {
                Vector2 center = new Vector2(64, 56);
                Color red = new Color(0.92f, 0.12f, 0.15f);
                Color stem = new Color(0.38f, 0.22f, 0.12f);
                Color leaf = new Color(0.25f, 0.75f, 0.22f);

                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        // Stem
                        if (x >= 62 && x <= 67 && y >= 85 && y <= 108)
                        {
                            tex.SetPixel(x, y, stem);
                            continue;
                        }
                        // Leaf
                        if (x >= 66 && x <= 95 && y >= 92 && y <= 112)
                        {
                            float leafDist = Vector2.Distance(new Vector2(x, y), new Vector2(80, 102));
                            if (leafDist < 12f)
                            {
                                tex.SetPixel(x, y, leaf);
                                continue;
                            }
                        }
                        // Apple Body (indented top)
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        float topIndent = (y > 70 && Mathf.Abs(x - 64) < 16) ? 8f : 0f;
                        if (dist + topIndent <= 44f)
                        {
                            tex.SetPixel(x, y, red);
                        }
                        else
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                    }
                }
            });

            // Banana Icon (Curved bright yellow banana matching Image 2)
            foodIcons[FoodType.Banana] = CreateTextureSprite("Icon_Banana", 128, 128, tex =>
            {
                Color yellow = new Color(1.0f, 0.88f, 0.10f);
                Color stem = new Color(0.42f, 0.58f, 0.18f);
                Color tip = new Color(0.32f, 0.20f, 0.10f);

                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }

                // Draw curved banana path
                for (float t = 0; t <= 1f; t += 0.005f)
                {
                    float bx = Mathf.Lerp(22f, 106f, t);
                    float by = 64f - Mathf.Sin(t * Mathf.PI) * 32f + (t * 22f);
                    float r = Mathf.Sin(t * Mathf.PI) * 14f + 3f;

                    Color c = t < 0.08f ? tip : t > 0.88f ? stem : yellow;

                    for (int dy = -16; dy <= 16; dy++)
                    {
                        for (int dx = -16; dx <= 16; dx++)
                        {
                            if (dx * dx + dy * dy <= r * r)
                            {
                                int px = Mathf.Clamp(Mathf.RoundToInt(bx + dx), 0, 127);
                                int py = Mathf.Clamp(Mathf.RoundToInt(by + dy), 0, 127);
                                tex.SetPixel(px, py, c);
                            }
                        }
                    }
                }
            });

            // Watermelon Icon (Striped green watermelon matching Image 2)
            foodIcons[FoodType.Watermelon] = CreateTextureSprite("Icon_Watermelon", 128, 128, tex =>
            {
                Vector2 center = new Vector2(64, 64);
                Color lightGreen = new Color(0.22f, 0.70f, 0.25f);
                Color darkGreen = new Color(0.08f, 0.35f, 0.12f);

                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        float dx = (x - center.x) / 46f;
                        float dy = (y - center.y) / 40f;
                        if (dx * dx + dy * dy <= 1f)
                        {
                            float stripe = Mathf.Sin(x * 0.28f + Mathf.Cos(y * 0.15f) * 1.5f);
                            tex.SetPixel(x, y, stripe > 0.25f ? darkGreen : lightGreen);
                        }
                        else
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                    }
                }
            });

            // Lemon Icon (Vibrant yellow citrus)
            foodIcons[FoodType.Lemon] = CreateTextureSprite("Icon_Lemon", 128, 128, tex =>
            {
                Vector2 center = new Vector2(64, 64);
                Color lemonYellow = new Color(1.0f, 0.92f, 0.05f);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        float dx = (x - center.x) / 48f;
                        float dy = (y - center.y) / 36f;
                        tex.SetPixel(x, y, (dx * dx + dy * dy <= 1f) ? lemonYellow : Color.clear);
                    }
                }
            });

            // Tomato Icon
            foodIcons[FoodType.Tomato] = CreateTextureSprite("Icon_Tomato", 128, 128, tex =>
            {
                Vector2 center = new Vector2(64, 58);
                Color red = new Color(0.92f, 0.14f, 0.12f);
                Color stem = new Color(0.20f, 0.72f, 0.22f);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        if (y >= 88 && Mathf.Abs(x - 64) < 22 && Vector2.Distance(new Vector2(x, y), new Vector2(64, 92)) < 16f)
                        {
                            tex.SetPixel(x, y, stem);
                        }
                        else
                        {
                            float dist = Vector2.Distance(new Vector2(x, y), center);
                            tex.SetPixel(x, y, dist <= 42f ? red : Color.clear);
                        }
                    }
                }
            });

            // Carrot Icon
            foodIcons[FoodType.Carrot] = CreateTextureSprite("Icon_Carrot", 128, 128, tex =>
            {
                Color orange = new Color(1.0f, 0.48f, 0.05f);
                Color green = new Color(0.20f, 0.75f, 0.25f);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        if (y >= 92 && Mathf.Abs(x - 64) < 18) tex.SetPixel(x, y, green);
                        else if (y < 92 && y >= 18)
                        {
                            float t = (y - 18f) / 74f;
                            float halfW = Mathf.Lerp(4f, 22f, t);
                            tex.SetPixel(x, y, Mathf.Abs(x - 64) <= halfW ? orange : Color.clear);
                        }
                        else tex.SetPixel(x, y, Color.clear);
                    }
                }
            });

            // BellPepper Icon
            foodIcons[FoodType.BellPepper] = CreateTextureSprite("Icon_BellPepper", 128, 128, tex =>
            {
                Color green = new Color(0.18f, 0.75f, 0.22f);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(64, 60));
                        tex.SetPixel(x, y, dist <= 42f ? green : Color.clear);
                    }
                }
            });

            // Potato Icon
            foodIcons[FoodType.Potato] = CreateTextureSprite("Icon_Potato", 128, 128, tex =>
            {
                Color brown = new Color(0.72f, 0.56f, 0.38f);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        float dx = (x - 64) / 46f;
                        float dy = (y - 64) / 34f;
                        tex.SetPixel(x, y, (dx * dx + dy * dy <= 1f) ? brown : Color.clear);
                    }
                }
            });

            // Cake & Bread Icons
            foodIcons[FoodType.ChocolateCake] = CreateTextureSprite("Icon_ChocCake", 128, 128, tex =>
            {
                Color choc = new Color(0.30f, 0.16f, 0.08f);
                Color cream = new Color(0.98f, 0.95f, 0.90f);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        if (x >= 24 && x <= 104 && y >= 28 && y <= 98)
                        {
                            tex.SetPixel(x, y, (y >= 88 || (y >= 54 && y <= 62)) ? cream : choc);
                        }
                        else tex.SetPixel(x, y, Color.clear);
                    }
                }
            });

            foodIcons[FoodType.CreamCake] = foodIcons[FoodType.ChocolateCake];
            foodIcons[FoodType.SwissRoll] = foodIcons[FoodType.ChocolateCake];
            foodIcons[FoodType.Pie] = foodIcons[FoodType.ChocolateCake];

            foodIcons[FoodType.Bread] = CreateTextureSprite("Icon_Bread", 128, 128, tex =>
            {
                Color crust = new Color(0.78f, 0.54f, 0.28f);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        float dx = (x - 64) / 48f;
                        float dy = (y - 58) / 36f;
                        tex.SetPixel(x, y, (dx * dx + dy * dy <= 1f) ? crust : Color.clear);
                    }
                }
            });

            // Sprite_Checkmark (Green circular badge with crisp white tick)
            Sprite uiCheckmark = CreateTextureSprite("Sprite_Checkmark", 128, 128, tex =>
            {
                Vector2 center = new Vector2(64, 64);
                Color greenCircle = new Color(0.18f, 0.82f, 0.38f);
                Color whiteCheck = Color.white;

                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist <= 56f)
                        {
                            tex.SetPixel(x, y, greenCircle);
                        }
                        else
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                    }
                }

                void DrawLine(Vector2 p1, Vector2 p2, float thickness)
                {
                    float d = Vector2.Distance(p1, p2);
                    for (float t = 0; t <= 1f; t += 0.5f / Mathf.Max(1f, d))
                    {
                        Vector2 pt = Vector2.Lerp(p1, p2, t);
                        for (int dy = -Mathf.CeilToInt(thickness); dy <= Mathf.CeilToInt(thickness); dy++)
                        {
                            for (int dx = -Mathf.CeilToInt(thickness); dx <= Mathf.CeilToInt(thickness); dx++)
                            {
                                if (dx * dx + dy * dy <= thickness * thickness)
                                {
                                    int px = Mathf.Clamp(Mathf.RoundToInt(pt.x + dx), 0, 127);
                                    int py = Mathf.Clamp(Mathf.RoundToInt(pt.y + dy), 0, 127);
                                    tex.SetPixel(px, py, whiteCheck);
                                }
                            }
                        }
                    }
                }

                DrawLine(new Vector2(34, 62), new Vector2(52, 42), 6f);
                DrawLine(new Vector2(52, 42), new Vector2(94, 84), 6f);
            });

            // Tex_CheckerboardPillow (8x8 alternating black and white squares for ceremonial cushion)
            CreateTexture("Tex_CheckerboardPillow", 256, 256, tex =>
            {
                Color whitePatch = new Color(0.96f, 0.96f, 0.96f);
                Color darkPatch = new Color(0.12f, 0.12f, 0.14f);
                int checkSize = 32; // 256 / 8 = 32

                for (int y = 0; y < 256; y++)
                {
                    int gy = y / checkSize;
                    for (int x = 0; x < 256; x++)
                    {
                        int gx = x / checkSize;
                        bool isWhite = (gx + gy) % 2 == 0;
                        tex.SetPixel(x, y, isWhite ? whitePatch : darkPatch);
                    }
                }
            });

            return (textures, (uiCircle, uiPillBg, uiPillFill, uiComboFrame, uiButton, uiCheckmark, foodIcons));
        }
        #endregion

        #region Materials
        private static Dictionary<string, Material> CreateMaterials(Shader litShader, Shader unlitShader, Dictionary<string, Texture2D> textures)
        {
            var dict = new Dictionary<string, Material>();

            Material CreateMat(string name, Color color, float smoothness = 0.5f, float metallic = 0f, Color? emission = null, Texture2D mainTex = null, bool unlit = false)
            {
                string path = $"{ROOT_PATH}/Art/Materials/{name}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                Shader chosenShader = unlit ? unlitShader : litShader;
                if (mat == null)
                {
                    mat = new Material(chosenShader);
                    AssetDatabase.CreateAsset(mat, path);
                }
                else
                {
                    mat.shader = chosenShader;
                }

                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

                if (mainTex != null)
                {
                    mat.mainTexture = mainTex;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                }

                if (emission.HasValue)
                {
                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission.Value);
                }

                EditorUtility.SetDirty(mat);
                dict[name] = mat;
                return mat;
            }

            // Environment
            // Cutting Board: glossy bright white / pale ice cyan matching Image 2
            CreateMat("Mat_CuttingBoard", new Color(0.98f, 0.99f, 1.0f), 0.85f, 0.02f, new Color(0.95f, 0.98f, 1.0f) * 0.35f);
            CreateMat("Mat_BoardTrim", new Color(0.85f, 0.90f, 0.95f), 0.6f, 0.1f);
            // Side Rails: Glowing neon cyan matching Image 2
            CreateMat("Mat_NeonCyanEdge", new Color(0f, 0.95f, 1f), 0.9f, 0.1f, new Color(0f, 0.95f, 1f) * 3.5f);
            // Gradient Background: Unlit vibrant Pink-to-Yellow gradient matching Image 2
            CreateMat("Mat_GradientBG", Color.white, 0f, 0f, null, textures["Tex_BackgroundGradient"], true);

            // Knives
            CreateMat("Mat_KnifeSteel", new Color(0.95f, 0.97f, 1.0f), 0.75f, 0.10f, null, textures["Tex_KnifeBlade"]);
            CreateMat("Mat_KnifeGold", new Color(1f, 0.85f, 0.28f), 0.85f, 0.35f, new Color(1f, 0.80f, 0.22f) * 0.35f);
            CreateMat("Mat_KnifeBlackHandle", new Color(0.12f, 0.12f, 0.15f), 0.4f, 0.1f);
            CreateMat("Mat_KnifeWoodHandle", Color.white, 0.55f, 0.05f, null, textures["Tex_KnifeHandle"]);
            CreateMat("Mat_KnifeRedGrip", new Color(0.85f, 0.15f, 0.2f), 0.45f, 0.1f);
            CreateMat("Mat_KnifeFantasyCyan", new Color(0.1f, 0.85f, 0.9f), 0.95f, 0.9f, new Color(0f, 0.6f, 0.7f) * 1.5f);

            // Fruits Exterior & Interior Textures
            CreateMat("Mat_BananaPeel", new Color(1f, 0.88f, 0.10f), 0.65f, 0f);
            CreateMat("Mat_BananaFlesh", Color.white, 0.45f, 0f, null, textures["Tex_BananaCutFace"]);

            CreateMat("Mat_WatermelonRind", Color.white, 0.75f, 0.02f, null, textures["Tex_Watermelon"]);
            CreateMat("Mat_WatermelonFlesh", Color.white, 0.65f, 0f, null, textures["Tex_WatermelonCutFace"]);

            CreateMat("Mat_LemonPeel", new Color(1.0f, 0.68f, 0.08f), 0.70f, 0f, null, textures["Tex_CitrusRind"]);
            CreateMat("Mat_LemonPulp", Color.white, 0.55f, 0f, null, textures["Tex_OrangeSlice"]);

            CreateMat("Mat_AppleSkin", new Color(0.90f, 0.12f, 0.15f), 0.85f, 0f);
            CreateMat("Mat_AppleFlesh", Color.white, 0.50f, 0f, null, textures["Tex_AppleCutFace"]);

            CreateMat("Mat_TomatoSkin", new Color(0.92f, 0.14f, 0.12f), 0.85f, 0f);
            CreateMat("Mat_TomatoPulp", Color.white, 0.60f, 0f, null, textures["Tex_TomatoCutFace"]);

            // Vegetables
            CreateMat("Mat_Carrot", new Color(1f, 0.48f, 0.05f), 0.6f, 0f, null, textures["Tex_CarrotCutFace"]);
            CreateMat("Mat_BellPepper", new Color(0.15f, 0.75f, 0.22f), 0.85f, 0f);
            CreateMat("Mat_Potato", new Color(0.72f, 0.56f, 0.38f), 0.4f, 0f);
            CreateMat("Mat_GreenLeaves", new Color(0.2f, 0.65f, 0.25f), 0.5f, 0f);

            // Bakery / Dessert
            CreateMat("Mat_ChocCake", new Color(0.25f, 0.14f, 0.08f), 0.45f, 0f);
            CreateMat("Mat_CreamWhite", new Color(0.98f, 0.97f, 0.95f), 0.75f, 0f);
            CreateMat("Mat_Strawberry", new Color(0.95f, 0.15f, 0.25f), 0.7f, 0f);
            CreateMat("Mat_SwissRollCrust", new Color(0.85f, 0.65f, 0.4f), 0.5f, 0f);
            CreateMat("Mat_PieCrust", new Color(0.82f, 0.62f, 0.35f), 0.45f, 0f);
            CreateMat("Mat_PieFilling", new Color(0.7f, 0.12f, 0.22f), 0.75f, 0f);
            CreateMat("Mat_BreadCrust", new Color(0.78f, 0.54f, 0.28f), 0.45f, 0f);
            CreateMat("Mat_BreadFlesh", new Color(0.96f, 0.93f, 0.85f), 0.35f, 0f);

            // Dedicated URP Particle Unlit Material
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null) particleShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (particleShader == null) particleShader = unlitShader;
            string pMatPath = $"{ROOT_PATH}/Art/Materials/Mat_Particle_Unlit.mat";
            Material existingPMat = AssetDatabase.LoadAssetAtPath<Material>(pMatPath);
            if (existingPMat != null)
            {
                existingPMat.shader = particleShader;
                EditorUtility.SetDirty(existingPMat);
                dict["Mat_Particle_Unlit"] = existingPMat;
            }
            else
            {
                Material matParticle = new Material(particleShader);
                AssetDatabase.CreateAsset(matParticle, pMatPath);
                dict["Mat_Particle_Unlit"] = matParticle;
            }

            // Ceremonial Checkerboard Pillow Cushion & Gold Trim Materials
            CreateMat("Mat_Pillow", Color.white, 0.25f, 0.0f, null, textures["Tex_CheckerboardPillow"]);
            CreateMat("Mat_PillowGoldTrim", new Color(1.0f, 0.82f, 0.20f), 0.65f, 0.65f);

            return dict;
        }
        #endregion

        #region Meshes
        private static Dictionary<string, Mesh> CreateAllMeshes()
        {
            var dict = new Dictionary<string, Mesh>();

            void SaveMesh(string name, Mesh mesh, string folder)
            {
                string path = $"{ROOT_PATH}/Art/{folder}/{name}.asset";
                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(mesh, existing);
                    EditorUtility.SetDirty(existing);
                    dict[name] = existing;
                }
                else
                {
                    AssetDatabase.CreateAsset(mesh, path);
                    dict[name] = mesh;
                }
            }

            // 1. Stylized Curved Banana Segments (5 segments forming curved banana like Image 2)
            SaveMesh("Mesh_Banana_Seg0", FoodSlicerMeshUtility.CreateCurvedBananaSegment(0.0f, 0.20f, 4, 16, false, true), "Food");
            SaveMesh("Mesh_Banana_Seg1", FoodSlicerMeshUtility.CreateCurvedBananaSegment(0.20f, 0.40f, 4, 16, true, true), "Food");
            SaveMesh("Mesh_Banana_Seg2", FoodSlicerMeshUtility.CreateCurvedBananaSegment(0.40f, 0.60f, 4, 16, true, true), "Food");
            SaveMesh("Mesh_Banana_Seg3", FoodSlicerMeshUtility.CreateCurvedBananaSegment(0.60f, 0.80f, 4, 16, true, true), "Food");
            SaveMesh("Mesh_Banana_Seg4", FoodSlicerMeshUtility.CreateCurvedBananaSegment(0.80f, 1.0f, 4, 16, true, true), "Food");
            SaveMesh("Mesh_Banana_Coin", FoodSlicerMeshUtility.CreateBananaCutCoin(0.18f, 0.10f, 16), "Food");
            SaveMesh("Mesh_Banana_TipSlice", FoodSlicerMeshUtility.CreateCurvedBananaSegment(0.0f, 0.15f, 4, 16, false, true, true), "Food");

            // 2. Stylized Watermelon Segments (4 slices)
            Vector3 wmRadius = new Vector3(0.18f, 0.18f, 0.28f);
            SaveMesh("Mesh_Watermelon_Seg0", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(wmRadius, -0.28f, -0.14f, 6, 24, false, true), "Food");
            SaveMesh("Mesh_Watermelon_Seg1", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(wmRadius, -0.14f, 0.0f, 6, 24, true, true), "Food");
            SaveMesh("Mesh_Watermelon_Seg2", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(wmRadius, 0.0f, 0.14f, 6, 24, true, true), "Food");
            SaveMesh("Mesh_Watermelon_Seg3", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(wmRadius, 0.14f, 0.28f, 6, 24, true, false), "Food");

            // 3. Stylized Lemon / Citrus Segments
            Vector3 lemonRadius = new Vector3(0.13f, 0.13f, 0.18f);
            SaveMesh("Mesh_Lemon_Seg0", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(lemonRadius, -0.18f, -0.09f, 6, 20, false, true), "Food");
            SaveMesh("Mesh_Lemon_Seg1", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(lemonRadius, -0.09f, 0.0f, 6, 20, true, true), "Food");
            SaveMesh("Mesh_Lemon_Seg2", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(lemonRadius, 0.0f, 0.09f, 6, 20, true, true), "Food");
            SaveMesh("Mesh_Lemon_Seg3", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(lemonRadius, 0.09f, 0.18f, 6, 20, true, false), "Food");

            // 4. Stylized Apple Segments
            Vector3 appleRadius = new Vector3(0.14f, 0.13f, 0.14f);
            SaveMesh("Mesh_Apple_Seg0", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(appleRadius, -0.14f, -0.07f, 6, 20, false, true), "Food");
            SaveMesh("Mesh_Apple_Seg1", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(appleRadius, -0.07f, 0.0f, 6, 20, true, true), "Food");
            SaveMesh("Mesh_Apple_Seg2", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(appleRadius, 0.0f, 0.07f, 6, 20, true, true), "Food");
            SaveMesh("Mesh_Apple_Seg3", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(appleRadius, 0.07f, 0.14f, 6, 20, true, false), "Food");

            // 5. Stylized Tomato Segments
            Vector3 tomatoRadius = new Vector3(0.14f, 0.12f, 0.14f);
            SaveMesh("Mesh_Tomato_Seg0", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(tomatoRadius, -0.14f, -0.07f, 6, 20, false, true), "Food");
            SaveMesh("Mesh_Tomato_Seg1", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(tomatoRadius, -0.07f, 0.0f, 6, 20, true, true), "Food");
            SaveMesh("Mesh_Tomato_Seg2", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(tomatoRadius, 0.0f, 0.07f, 6, 20, true, true), "Food");
            SaveMesh("Mesh_Tomato_Seg3", FoodSlicerMeshUtility.CreateSegmentedEllipsoid(tomatoRadius, 0.07f, 0.14f, 6, 20, true, false), "Food");

            // 6. Vegetables & Bakery
            SaveMesh("Mesh_Carrot", FoodSlicerMeshUtility.CreateCylinderSegment(0.04f, 0.08f, 0.65f), "Food");
            SaveMesh("Mesh_CarrotSlice", FoodSlicerMeshUtility.CreateCylinderSegment(0.07f, 0.07f, 0.12f), "Food");

            SaveMesh("Mesh_BellPepper", FoodSlicerMeshUtility.CreateEllipsoid(new Vector3(0.14f, 0.16f, 0.14f)), "Food");
            SaveMesh("Mesh_BellPepperSlice", FoodSlicerMeshUtility.CreateCylinderSegment(0.14f, 0.14f, 0.12f), "Food");

            SaveMesh("Mesh_Potato", FoodSlicerMeshUtility.CreateEllipsoid(new Vector3(0.13f, 0.12f, 0.18f)), "Food");
            SaveMesh("Mesh_PotatoSlice", FoodSlicerMeshUtility.CreateCylinderSegment(0.13f, 0.13f, 0.12f), "Food");

            SaveMesh("Mesh_ChocCake", FoodSlicerMeshUtility.CreateBox(new Vector3(0.28f, 0.20f, 0.55f)), "Food");
            SaveMesh("Mesh_ChocCakeSlice", FoodSlicerMeshUtility.CreateBox(new Vector3(0.28f, 0.20f, 0.12f)), "Food");

            SaveMesh("Mesh_CreamCake", FoodSlicerMeshUtility.CreateBox(new Vector3(0.28f, 0.22f, 0.55f)), "Food");
            SaveMesh("Mesh_CreamCakeSlice", FoodSlicerMeshUtility.CreateBox(new Vector3(0.28f, 0.22f, 0.12f)), "Food");

            SaveMesh("Mesh_SwissRoll", FoodSlicerMeshUtility.CreateCylinderSegment(0.15f, 0.15f, 0.55f), "Food");
            SaveMesh("Mesh_SwissRollSlice", FoodSlicerMeshUtility.CreateCylinderSegment(0.15f, 0.15f, 0.12f), "Food");

            SaveMesh("Mesh_Pie", FoodSlicerMeshUtility.CreateCylinderSegment(0.24f, 0.20f, 0.18f), "Food");
            SaveMesh("Mesh_PieSlice", FoodSlicerMeshUtility.CreateBox(new Vector3(0.22f, 0.16f, 0.12f)), "Food");

            SaveMesh("Mesh_Bread", FoodSlicerMeshUtility.CreateBox(new Vector3(0.26f, 0.22f, 0.65f)), "Food");
            SaveMesh("Mesh_BreadSlice", FoodSlicerMeshUtility.CreateBox(new Vector3(0.26f, 0.22f, 0.12f)), "Food");

            // Knife Meshes (Large Curved Chef Knife matching Image 2)
            SaveMesh("Mesh_Blade_Chef", FoodSlicerMeshUtility.CreateLargeChefBlade(1.90f, 0.24f, 0.025f), "Knives");
            SaveMesh("Mesh_Handle_Chef", FoodSlicerMeshUtility.CreateChefHandle(0.55f, 0.11f, 0.08f), "Knives");
            SaveMesh("Mesh_Bolster_Chef", FoodSlicerMeshUtility.CreateBolsterCollar(0.06f, 0.065f), "Knives");

            // Other Knives
            SaveMesh("Mesh_Blade_Katana", FoodSlicerMeshUtility.CreateKnifeBlade(1.65f, 0.20f, 0.03f, 0.2f, true), "Knives");
            SaveMesh("Mesh_Handle_Katana", FoodSlicerMeshUtility.CreateKnifeHandle(0.58f, 0.06f, 0.08f), "Knives");

            SaveMesh("Mesh_Blade_Cleaver", FoodSlicerMeshUtility.CreateKnifeBlade(1.4f, 0.55f, 0.045f, 0.05f, false), "Knives");
            SaveMesh("Mesh_Handle_Cleaver", FoodSlicerMeshUtility.CreateKnifeHandle(0.48f, 0.09f, 0.11f), "Knives");

            SaveMesh("Mesh_Blade_Fantasy", FoodSlicerMeshUtility.CreateKnifeBlade(1.75f, 0.38f, 0.04f, 0.6f, true), "Knives");
            SaveMesh("Mesh_Handle_Fantasy", FoodSlicerMeshUtility.CreateKnifeHandle(0.52f, 0.08f, 0.12f), "Knives");

            SaveMesh("Mesh_Blade_Santoku", FoodSlicerMeshUtility.CreateKnifeBlade(1.6f, 0.38f, 0.035f, 0.25f, false), "Knives");
            SaveMesh("Mesh_Handle_Santoku", FoodSlicerMeshUtility.CreateKnifeHandle(0.50f, 0.07f, 0.09f), "Knives");

            SaveMesh("Mesh_Blade_Ornamental", FoodSlicerMeshUtility.CreateKnifeBlade(1.6f, 0.34f, 0.045f, 0.45f, true), "Knives");
            SaveMesh("Mesh_Handle_Ornamental", FoodSlicerMeshUtility.CreateKnifeHandle(0.52f, 0.08f, 0.11f), "Knives");

            // Ceremonial Knife Rest Pillow, Gold Perimeter Piping & Corner Tassels
            SaveMesh("Mesh_Pillow", FoodSlicerMeshUtility.CreateCushionMesh(new Vector3(0.44f, 0.09f, 0.44f), 14), "Environment");
            SaveMesh("Mesh_PillowPiping", FoodSlicerMeshUtility.CreatePipingMesh(0.44f, 0.44f, 0.016f), "Environment");
            SaveMesh("Mesh_CornerTassel", FoodSlicerMeshUtility.CreateTasselMesh(0.065f, 0.018f), "Environment");

            // Environment: Runway extending into distance matching Image 2
            SaveMesh("Mesh_CuttingBoard", FoodSlicerMeshUtility.CreateBox(new Vector3(0.96f, 0.15f, 160.0f)), "Environment");
            SaveMesh("Mesh_NeonRail", FoodSlicerMeshUtility.CreateBox(new Vector3(0.035f, 0.18f, 160.0f)), "Environment");
            SaveMesh("Mesh_BackgroundPlane", FoodSlicerMeshUtility.CreateQuad(18.0f, 26.0f), "Environment");
            SaveMesh("Mesh_Watermelon_Whole", FoodSlicerMeshUtility.CreateWatermelonMesh(new Vector3(0.18f, 0.22f, 0.18f)), "Food");

            return dict;
        }
        #endregion

        #region Food Prefabs
        private static FoodItem AttachFoodItem(GameObject go, FoodType type)
        {
            FoodItem fi = go.AddComponent<FoodItem>();
            SerializedObject so = new SerializedObject(fi);
            SerializedProperty tp = so.FindProperty("foodType");
            if (tp != null) tp.enumValueIndex = (int)type;
            so.ApplyModifiedPropertiesWithoutUndo();
            return fi;
        }

        private static Dictionary<FoodType, (GameObject uncut, GameObject slice)> CreateFoodPrefabs(
            Dictionary<string, Material> materials,
            Dictionary<string, Mesh> meshes)
        {
            var dict = new Dictionary<FoodType, (GameObject uncut, GameObject slice)>();

            // 1. Banana Prefab (Curved banana with 5 segments and cut faces matching Image 2)
            {
                string slicePath = $"{ROOT_PATH}/Prefabs/Food/Slice_Banana.prefab";
                GameObject sliceGo = new GameObject("Slice_Banana");
                MeshFilter sliceMf = sliceGo.AddComponent<MeshFilter>();
                sliceMf.sharedMesh = meshes["Mesh_Banana_Seg1"];
                MeshRenderer sliceMr = sliceGo.AddComponent<MeshRenderer>();
                sliceMr.sharedMaterials = new Material[] { materials["Mat_BananaPeel"], materials["Mat_BananaFlesh"] };
                BoxCollider sliceCol = sliceGo.AddComponent<BoxCollider>();
                sliceCol.size = new Vector3(0.30f, 0.28f, 0.18f);
                Rigidbody sliceRb = sliceGo.AddComponent<Rigidbody>();
                sliceRb.isKinematic = true;
                sliceGo.AddComponent<FoodSlicePiece>();
                GameObject slicePrefab = PrefabUtility.SaveAsPrefabAsset(sliceGo, slicePath);
                GameObject.DestroyImmediate(sliceGo);

                string uncutPath = $"{ROOT_PATH}/Prefabs/Food/Food_Banana.prefab";
                GameObject uncutGo = new GameObject("Food_Banana");
                BoxCollider mainCol = uncutGo.AddComponent<BoxCollider>();
                mainCol.size = new Vector3(0.30f, 0.28f, 0.85f);
                mainCol.center = new Vector3(0, 0.14f, 0);

                FoodItem foodItem = AttachFoodItem(uncutGo, FoodType.Banana);
                SegmentedFood segFood = uncutGo.AddComponent<SegmentedFood>();
                FoodMover mover = uncutGo.AddComponent<FoodMover>();

                // Build 5 visual curved segments
                for (int s = 0; s < 5; s++)
                {
                    GameObject segObj = new GameObject($"Segment_{s}");
                    segObj.transform.SetParent(uncutGo.transform, false);
                    segObj.transform.localPosition = Vector3.zero;

                    MeshFilter mf = segObj.AddComponent<MeshFilter>();
                    mf.sharedMesh = meshes[$"Mesh_Banana_Seg{s}"];
                    MeshRenderer mr = segObj.AddComponent<MeshRenderer>();
                    mr.sharedMaterials = new Material[] { materials["Mat_BananaPeel"], materials["Mat_BananaFlesh"] };

                    segFood.AddVisualSegment(segObj);
                }

                GameObject uncutPrefab = PrefabUtility.SaveAsPrefabAsset(uncutGo, uncutPath);
                GameObject.DestroyImmediate(uncutGo);
                dict[FoodType.Banana] = (uncutPrefab, slicePrefab);
            }

            // 2. Watermelon Prefab (Large striped watermelon with 4 segments showing red flesh with seeds)
            {
                string slicePath = $"{ROOT_PATH}/Prefabs/Food/Slice_Watermelon.prefab";
                GameObject sliceGo = new GameObject("Slice_Watermelon");
                MeshFilter sliceMf = sliceGo.AddComponent<MeshFilter>();
                sliceMf.sharedMesh = meshes["Mesh_Watermelon_Seg1"];
                MeshRenderer sliceMr = sliceGo.AddComponent<MeshRenderer>();
                sliceMr.sharedMaterials = new Material[] { materials["Mat_WatermelonRind"], materials["Mat_WatermelonFlesh"] };
                BoxCollider sliceCol = sliceGo.AddComponent<BoxCollider>();
                sliceCol.size = new Vector3(0.38f, 0.38f, 0.16f);
                Rigidbody sliceRb = sliceGo.AddComponent<Rigidbody>();
                sliceRb.isKinematic = true;
                sliceGo.AddComponent<FoodSlicePiece>();
                GameObject slicePrefab = PrefabUtility.SaveAsPrefabAsset(sliceGo, slicePath);
                GameObject.DestroyImmediate(sliceGo);

                string uncutPath = $"{ROOT_PATH}/Prefabs/Food/Food_Watermelon.prefab";
                GameObject uncutGo = new GameObject("Food_Watermelon");
                BoxCollider mainCol = uncutGo.AddComponent<BoxCollider>();
                mainCol.size = new Vector3(0.38f, 0.38f, 0.60f);
                mainCol.center = new Vector3(0, 0.19f, 0);

                FoodItem foodItem = AttachFoodItem(uncutGo, FoodType.Watermelon);
                SegmentedFood segFood = uncutGo.AddComponent<SegmentedFood>();
                FoodMover mover = uncutGo.AddComponent<FoodMover>();

                for (int s = 0; s < 4; s++)
                {
                    GameObject segObj = new GameObject($"Segment_{s}");
                    segObj.transform.SetParent(uncutGo.transform, false);
                    segObj.transform.localPosition = new Vector3(0, 0.19f, 0);

                    MeshFilter mf = segObj.AddComponent<MeshFilter>();
                    mf.sharedMesh = meshes[$"Mesh_Watermelon_Seg{s}"];
                    MeshRenderer mr = segObj.AddComponent<MeshRenderer>();
                    mr.sharedMaterials = new Material[] { materials["Mat_WatermelonRind"], materials["Mat_WatermelonFlesh"] };

                    segFood.AddVisualSegment(segObj);
                }

                GameObject uncutPrefab = PrefabUtility.SaveAsPrefabAsset(uncutGo, uncutPath);
                GameObject.DestroyImmediate(uncutGo);
                dict[FoodType.Watermelon] = (uncutPrefab, slicePrefab);
            }

            // 3. Lemon / Citrus Prefab
            {
                string slicePath = $"{ROOT_PATH}/Prefabs/Food/Slice_Lemon.prefab";
                GameObject sliceGo = new GameObject("Slice_Lemon");
                MeshFilter sliceMf = sliceGo.AddComponent<MeshFilter>();
                sliceMf.sharedMesh = meshes["Mesh_Lemon_Seg1"];
                MeshRenderer sliceMr = sliceGo.AddComponent<MeshRenderer>();
                sliceMr.sharedMaterials = new Material[] { materials["Mat_LemonPeel"], materials["Mat_LemonPulp"] };
                BoxCollider sliceCol = sliceGo.AddComponent<BoxCollider>();
                sliceCol.size = new Vector3(0.28f, 0.28f, 0.12f);
                Rigidbody sliceRb = sliceGo.AddComponent<Rigidbody>();
                sliceRb.isKinematic = true;
                sliceGo.AddComponent<FoodSlicePiece>();
                GameObject slicePrefab = PrefabUtility.SaveAsPrefabAsset(sliceGo, slicePath);
                GameObject.DestroyImmediate(sliceGo);

                string uncutPath = $"{ROOT_PATH}/Prefabs/Food/Food_Lemon.prefab";
                GameObject uncutGo = new GameObject("Food_Lemon");
                BoxCollider mainCol = uncutGo.AddComponent<BoxCollider>();
                mainCol.size = new Vector3(0.28f, 0.28f, 0.40f);
                mainCol.center = new Vector3(0, 0.14f, 0);

                FoodItem foodItem = AttachFoodItem(uncutGo, FoodType.Lemon);
                SegmentedFood segFood = uncutGo.AddComponent<SegmentedFood>();
                FoodMover mover = uncutGo.AddComponent<FoodMover>();

                for (int s = 0; s < 4; s++)
                {
                    GameObject segObj = new GameObject($"Segment_{s}");
                    segObj.transform.SetParent(uncutGo.transform, false);
                    segObj.transform.localPosition = new Vector3(0, 0.14f, 0);

                    MeshFilter mf = segObj.AddComponent<MeshFilter>();
                    mf.sharedMesh = meshes[$"Mesh_Lemon_Seg{s}"];
                    MeshRenderer mr = segObj.AddComponent<MeshRenderer>();
                    mr.sharedMaterials = new Material[] { materials["Mat_LemonPeel"], materials["Mat_LemonPulp"] };

                    segFood.AddVisualSegment(segObj);
                }

                GameObject uncutPrefab = PrefabUtility.SaveAsPrefabAsset(uncutGo, uncutPath);
                GameObject.DestroyImmediate(uncutGo);
                dict[FoodType.Lemon] = (uncutPrefab, slicePrefab);
            }

            // 4. Apple Prefab
            {
                string slicePath = $"{ROOT_PATH}/Prefabs/Food/Slice_Apple.prefab";
                GameObject sliceGo = new GameObject("Slice_Apple");
                MeshFilter sliceMf = sliceGo.AddComponent<MeshFilter>();
                sliceMf.sharedMesh = meshes["Mesh_Apple_Seg1"];
                MeshRenderer sliceMr = sliceGo.AddComponent<MeshRenderer>();
                sliceMr.sharedMaterials = new Material[] { materials["Mat_AppleSkin"], materials["Mat_AppleFlesh"] };
                BoxCollider sliceCol = sliceGo.AddComponent<BoxCollider>();
                sliceCol.size = new Vector3(0.30f, 0.28f, 0.08f);
                Rigidbody sliceRb = sliceGo.AddComponent<Rigidbody>();
                sliceRb.isKinematic = true;
                sliceGo.AddComponent<FoodSlicePiece>();
                GameObject slicePrefab = PrefabUtility.SaveAsPrefabAsset(sliceGo, slicePath);
                GameObject.DestroyImmediate(sliceGo);

                string uncutPath = $"{ROOT_PATH}/Prefabs/Food/Food_Apple.prefab";
                GameObject uncutGo = new GameObject("Food_Apple");
                BoxCollider mainCol = uncutGo.AddComponent<BoxCollider>();
                mainCol.size = new Vector3(0.30f, 0.28f, 0.30f);
                mainCol.center = new Vector3(0, 0.14f, 0);

                FoodItem foodItem = AttachFoodItem(uncutGo, FoodType.Apple);
                SegmentedFood segFood = uncutGo.AddComponent<SegmentedFood>();
                FoodMover mover = uncutGo.AddComponent<FoodMover>();

                for (int s = 0; s < 4; s++)
                {
                    GameObject segObj = new GameObject($"Segment_{s}");
                    segObj.transform.SetParent(uncutGo.transform, false);
                    segObj.transform.localPosition = new Vector3(0, 0.14f, 0);

                    MeshFilter mf = segObj.AddComponent<MeshFilter>();
                    mf.sharedMesh = meshes[$"Mesh_Apple_Seg{s}"];
                    MeshRenderer mr = segObj.AddComponent<MeshRenderer>();
                    mr.sharedMaterials = new Material[] { materials["Mat_AppleSkin"], materials["Mat_AppleFlesh"] };

                    segFood.AddVisualSegment(segObj);
                }

                GameObject uncutPrefab = PrefabUtility.SaveAsPrefabAsset(uncutGo, uncutPath);
                GameObject.DestroyImmediate(uncutGo);
                dict[FoodType.Apple] = (uncutPrefab, slicePrefab);
            }

            // 5. Tomato Prefab
            {
                string slicePath = $"{ROOT_PATH}/Prefabs/Food/Slice_Tomato.prefab";
                GameObject sliceGo = new GameObject("Slice_Tomato");
                MeshFilter sliceMf = sliceGo.AddComponent<MeshFilter>();
                sliceMf.sharedMesh = meshes["Mesh_Tomato_Seg1"];
                MeshRenderer sliceMr = sliceGo.AddComponent<MeshRenderer>();
                sliceMr.sharedMaterials = new Material[] { materials["Mat_TomatoSkin"], materials["Mat_TomatoPulp"] };
                BoxCollider sliceCol = sliceGo.AddComponent<BoxCollider>();
                sliceCol.size = new Vector3(0.30f, 0.26f, 0.08f);
                Rigidbody sliceRb = sliceGo.AddComponent<Rigidbody>();
                sliceRb.isKinematic = true;
                sliceGo.AddComponent<FoodSlicePiece>();
                GameObject slicePrefab = PrefabUtility.SaveAsPrefabAsset(sliceGo, slicePath);
                GameObject.DestroyImmediate(sliceGo);

                string uncutPath = $"{ROOT_PATH}/Prefabs/Food/Food_Tomato.prefab";
                GameObject uncutGo = new GameObject("Food_Tomato");
                BoxCollider mainCol = uncutGo.AddComponent<BoxCollider>();
                mainCol.size = new Vector3(0.30f, 0.26f, 0.30f);
                mainCol.center = new Vector3(0, 0.13f, 0);

                FoodItem foodItem = AttachFoodItem(uncutGo, FoodType.Tomato);
                SegmentedFood segFood = uncutGo.AddComponent<SegmentedFood>();
                FoodMover mover = uncutGo.AddComponent<FoodMover>();

                for (int s = 0; s < 4; s++)
                {
                    GameObject segObj = new GameObject($"Segment_{s}");
                    segObj.transform.SetParent(uncutGo.transform, false);
                    segObj.transform.localPosition = new Vector3(0, 0.13f, 0);

                    MeshFilter mf = segObj.AddComponent<MeshFilter>();
                    mf.sharedMesh = meshes[$"Mesh_Tomato_Seg{s}"];
                    MeshRenderer mr = segObj.AddComponent<MeshRenderer>();
                    mr.sharedMaterials = new Material[] { materials["Mat_TomatoSkin"], materials["Mat_TomatoPulp"] };

                    segFood.AddVisualSegment(segObj);
                }

                GameObject uncutPrefab = PrefabUtility.SaveAsPrefabAsset(uncutGo, uncutPath);
                GameObject.DestroyImmediate(uncutGo);
                dict[FoodType.Tomato] = (uncutPrefab, slicePrefab);
            }

            // 6. Remaining Foods (Carrot, BellPepper, Potato, Cakes, Bread)
            var genericDefs = new (FoodType type, string meshBase, string meshSlice, string skinMat, string fleshMat, int segments, Vector3 colSize)[]
            {
                (FoodType.Carrot, "Mesh_Carrot", "Mesh_CarrotSlice", "Mat_Carrot", "Mat_Carrot", 5, new Vector3(0.18f, 0.18f, 0.65f)),
                (FoodType.BellPepper, "Mesh_BellPepper", "Mesh_BellPepperSlice", "Mat_BellPepper", "Mat_BellPepper", 4, new Vector3(0.28f, 0.32f, 0.28f)),
                (FoodType.Potato, "Mesh_Potato", "Mesh_PotatoSlice", "Mat_Potato", "Mat_Potato", 4, new Vector3(0.26f, 0.24f, 0.36f)),
                (FoodType.ChocolateCake, "Mesh_ChocCake", "Mesh_ChocCakeSlice", "Mat_ChocCake", "Mat_CreamWhite", 4, new Vector3(0.28f, 0.20f, 0.55f)),
                (FoodType.CreamCake, "Mesh_CreamCake", "Mesh_CreamCakeSlice", "Mat_CreamWhite", "Mat_Strawberry", 4, new Vector3(0.28f, 0.22f, 0.55f)),
                (FoodType.SwissRoll, "Mesh_SwissRoll", "Mesh_SwissRollSlice", "Mat_SwissRollCrust", "Mat_CreamWhite", 4, new Vector3(0.30f, 0.30f, 0.55f)),
                (FoodType.Pie, "Mesh_Pie", "Mesh_PieSlice", "Mat_PieCrust", "Mat_PieFilling", 4, new Vector3(0.48f, 0.20f, 0.48f)),
                (FoodType.Bread, "Mesh_Bread", "Mesh_BreadSlice", "Mat_BreadCrust", "Mat_BreadFlesh", 5, new Vector3(0.26f, 0.22f, 0.65f))
            };

            foreach (var def in genericDefs)
            {
                string slicePath = $"{ROOT_PATH}/Prefabs/Food/Slice_{def.type}.prefab";
                GameObject sliceGo = new GameObject($"Slice_{def.type}");
                MeshFilter sliceMf = sliceGo.AddComponent<MeshFilter>();
                sliceMf.sharedMesh = meshes[def.meshSlice];
                MeshRenderer sliceMr = sliceGo.AddComponent<MeshRenderer>();
                sliceMr.sharedMaterial = materials[def.fleshMat];

                BoxCollider sliceCol = sliceGo.AddComponent<BoxCollider>();
                sliceCol.size = new Vector3(def.colSize.x, def.colSize.y, def.colSize.z / def.segments);
                Rigidbody sliceRb = sliceGo.AddComponent<Rigidbody>();
                sliceRb.isKinematic = true;
                sliceGo.AddComponent<FoodSlicePiece>();
                GameObject slicePrefab = PrefabUtility.SaveAsPrefabAsset(sliceGo, slicePath);
                GameObject.DestroyImmediate(sliceGo);

                string uncutPath = $"{ROOT_PATH}/Prefabs/Food/Food_{def.type}.prefab";
                GameObject uncutGo = new GameObject($"Food_{def.type}");
                BoxCollider mainCol = uncutGo.AddComponent<BoxCollider>();
                mainCol.size = def.colSize;
                mainCol.center = new Vector3(0, def.colSize.y * 0.5f, 0);

                FoodItem foodItem = AttachFoodItem(uncutGo, def.type);
                SegmentedFood segFood = uncutGo.AddComponent<SegmentedFood>();
                FoodMover mover = uncutGo.AddComponent<FoodMover>();

                float totalLength = def.colSize.z * 0.85f;
                float segStep = totalLength / def.segments;
                float startZ = (totalLength * 0.5f) - (segStep * 0.5f);

                for (int s = 0; s < def.segments; s++)
                {
                    GameObject segObj = new GameObject($"Segment_{s}");
                    segObj.transform.SetParent(uncutGo.transform);
                    segObj.transform.localPosition = new Vector3(0, def.colSize.y * 0.5f, startZ - s * segStep);

                    MeshFilter mf = segObj.AddComponent<MeshFilter>();
                    mf.sharedMesh = meshes[def.meshSlice];
                    MeshRenderer mr = segObj.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = materials[def.skinMat];

                    segFood.AddVisualSegment(segObj);
                }

                GameObject uncutPrefab = PrefabUtility.SaveAsPrefabAsset(uncutGo, uncutPath);
                GameObject.DestroyImmediate(uncutGo);
                dict[def.type] = (uncutPrefab, slicePrefab);
            }

            return dict;
        }
        #endregion

        #region Knife Prefabs
        private static List<GameObject> CreateKnifePrefabs(Dictionary<string, Material> materials, Dictionary<string, Mesh> meshes)
        {
            var list = new List<GameObject>();

            var knifeConfigs = new (string name, string meshBlade, string meshHandle, string matBlade, string matHandle)[]
            {
                ("ChefKnife", "Mesh_Blade_Chef", "Mesh_Handle_Chef", "Mat_KnifeSteel", "Mat_KnifeWoodHandle"),
                ("Katana", "Mesh_Blade_Katana", "Mesh_Handle_Katana", "Mat_KnifeSteel", "Mat_KnifeRedGrip"),
                ("Cleaver", "Mesh_Blade_Cleaver", "Mesh_Handle_Cleaver", "Mat_KnifeSteel", "Mat_KnifeWoodHandle"),
                ("FantasyBlade", "Mesh_Blade_Fantasy", "Mesh_Handle_Fantasy", "Mat_KnifeFantasyCyan", "Mat_KnifeGold"),
                ("KitchenKnife", "Mesh_Blade_Santoku", "Mesh_Handle_Santoku", "Mat_KnifeSteel", "Mat_KnifeBlackHandle"),
                ("OrnamentalKnife", "Mesh_Blade_Ornamental", "Mesh_Handle_Ornamental", "Mat_KnifeGold", "Mat_KnifeRedGrip")
            };

            for (int i = 0; i < knifeConfigs.Length; i++)
            {
                var cfg = knifeConfigs[i];
                string path = $"{ROOT_PATH}/Prefabs/Knives/Knife_{cfg.name}.prefab";

                GameObject root = new GameObject($"Knife_{cfg.name}");
                KnifeController kc = root.AddComponent<KnifeController>();
                AudioSource aud = root.AddComponent<AudioSource>();
                aud.playOnAwake = false;

                // Visual hierarchy
                GameObject visual = new GameObject("KnifeVisual");
                visual.transform.SetParent(root.transform, false);

                // Blade: originates at (0,0,0) and extends toward negative X
                GameObject blade = new GameObject("Blade");
                blade.transform.SetParent(visual.transform, false);
                blade.transform.localPosition = Vector3.zero;
                MeshFilter bMf = blade.AddComponent<MeshFilter>();
                bMf.sharedMesh = meshes[cfg.meshBlade];
                MeshRenderer bMr = blade.AddComponent<MeshRenderer>();
                bMr.sharedMaterial = materials[cfg.matBlade];

                // Bolster collar
                GameObject bolster = new GameObject("Bolster");
                bolster.transform.SetParent(visual.transform, false);
                bolster.transform.localPosition = new Vector3(0.02f, 0, 0);
                bolster.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                MeshFilter bolMf = bolster.AddComponent<MeshFilter>();
                bolMf.sharedMesh = meshes["Mesh_Bolster_Chef"];
                MeshRenderer bolMr = bolster.AddComponent<MeshRenderer>();
                bolMr.sharedMaterial = materials["Mat_KnifeGold"];

                // Handle: extends from (0.05, 0, 0) toward positive X (lower-right)
                GameObject handle = new GameObject("Handle");
                handle.transform.SetParent(visual.transform, false);
                handle.transform.localPosition = new Vector3(0.05f, 0, 0);
                MeshFilter hMf = handle.AddComponent<MeshFilter>();
                hMf.sharedMesh = meshes[cfg.meshHandle];
                MeshRenderer hMr = handle.AddComponent<MeshRenderer>();
                hMr.sharedMaterial = materials[cfg.matHandle];

                // Blade Trail
                GameObject trailObj = new GameObject("Trail");
                trailObj.transform.SetParent(blade.transform, false);
                trailObj.transform.localPosition = new Vector3(-0.9f, -0.18f, 0);
                TrailRenderer trail = trailObj.AddComponent<TrailRenderer>();
                trail.time = 0.15f;
                trail.startWidth = 0.30f;
                trail.endWidth = 0.02f;
                trail.material = materials[cfg.matBlade];
                trail.emitting = false;

                // Slice Detector Box positioned along the cutting edge
                GameObject detector = new GameObject("SliceDetector");
                detector.transform.SetParent(root.transform, false);
                detector.transform.localPosition = new Vector3(-0.85f, -0.16f, 0);

                // Wire up serialized fields in KnifeController
                SerializedObject so = new SerializedObject(kc);
                so.FindProperty("bladeEdgeCenter").objectReferenceValue = detector.transform;
                so.FindProperty("sliceBoxSize").vector3Value = new Vector3(1.8f, 0.45f, 0.55f);
                so.FindProperty("idleLocalPosition").vector3Value = Vector3.zero;
                so.FindProperty("chopOffset").vector3Value = new Vector3(0f, -0.38f, 0f);
                so.FindProperty("idleLocalEuler").vector3Value = Vector3.zero;
                so.FindProperty("impactLocalEuler").vector3Value = new Vector3(-8f, 4f, 6f);
                so.FindProperty("bladeTrail").objectReferenceValue = trail;
                so.FindProperty("knifeAudioSource").objectReferenceValue = aud;
                so.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                GameObject.DestroyImmediate(root);
                list.Add(prefab);
            }

            return list;
        }
        #endregion

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0) font = fonts[0];
            }
            return font;
        }

        #region Floating Text Prefab
        private static GameObject CreateFloatingTextPrefab(Sprite circleSprite)
        {
            string path = $"{ROOT_PATH}/Prefabs/UI/FloatingTextItem.prefab";
            GameObject go = new GameObject("FloatingTextItem");
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250, 70);

            CanvasGroup cg = go.AddComponent<CanvasGroup>();

            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            RectTransform trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            Text text = textGo.AddComponent<Text>();
            text.text = "+10";
            text.font = GetDefaultFont();
            text.fontSize = 36;
            text.fontStyle = FontStyle.BoldAndItalic;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.95f, 0.1f);

            Outline outline = textGo.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.75f);
            outline.effectDistance = new Vector2(2, -2);

            FloatingScoreItem item = go.AddComponent<FloatingScoreItem>();

            SerializedObject so = new SerializedObject(item);
            so.FindProperty("scoreText").objectReferenceValue = text;
            so.FindProperty("canvasGroup").objectReferenceValue = cg;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }
        #endregion

        #region ScriptableObjects
        private static List<FoodData> CreateFoodDataAssets(
            Dictionary<FoodType, (GameObject uncut, GameObject slice)> foodPrefabs,
            (Sprite uiCircle, Sprite uiPillBg, Sprite uiPillFill, Sprite uiComboFrame, Sprite uiButton, Sprite uiCheckmark, Dictionary<FoodType, Sprite> foodIcons) sprites)
        {
            var list = new List<FoodData>();

            var dataDefs = new (FoodType type, string name, FoodCategory cat, int segments, int score, Color pColor)[]
            {
                (FoodType.Apple, "Apple", FoodCategory.Fruit, 4, 10, new Color(0.9f, 0.15f, 0.15f)),
                (FoodType.Banana, "Banana", FoodCategory.Fruit, 5, 10, new Color(1f, 0.85f, 0.1f)),
                (FoodType.Watermelon, "Watermelon", FoodCategory.Fruit, 4, 15, new Color(0.95f, 0.2f, 0.25f)),
                (FoodType.Lemon, "Lemon", FoodCategory.Fruit, 4, 12, new Color(1f, 0.92f, 0.1f)),
                (FoodType.Tomato, "Tomato", FoodCategory.Fruit, 4, 10, new Color(0.9f, 0.14f, 0.12f)),
                (FoodType.Carrot, "Carrot", FoodCategory.Vegetable, 5, 12, new Color(1f, 0.5f, 0.05f)),
                (FoodType.BellPepper, "Bell Pepper", FoodCategory.Vegetable, 4, 12, new Color(0.2f, 0.75f, 0.25f)),
                (FoodType.Potato, "Potato", FoodCategory.Vegetable, 4, 10, new Color(0.72f, 0.55f, 0.38f)),
                (FoodType.ChocolateCake, "Chocolate Cake", FoodCategory.Bakery, 4, 15, new Color(0.35f, 0.2f, 0.12f)),
                (FoodType.CreamCake, "Cream Cake", FoodCategory.Bakery, 4, 15, new Color(0.95f, 0.9f, 0.9f)),
                (FoodType.SwissRoll, "Swiss Roll", FoodCategory.Bakery, 4, 15, new Color(0.85f, 0.65f, 0.4f)),
                (FoodType.Pie, "Fruit Pie", FoodCategory.Bakery, 4, 15, new Color(0.85f, 0.65f, 0.35f)),
                (FoodType.Bread, "Loaf Bread", FoodCategory.Bakery, 5, 12, new Color(0.88f, 0.75f, 0.55f))
            };

            foreach (var def in dataDefs)
            {
                string path = $"{ROOT_PATH}/ScriptableObjects/Foods/FoodData_{def.type}.asset";
                FoodData data = AssetDatabase.LoadAssetAtPath<FoodData>(path);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<FoodData>();
                    AssetDatabase.CreateAsset(data, path);
                }

                data.foodType = def.type;
                data.foodName = def.name;
                data.category = def.cat;
                data.totalSegments = def.segments;
                data.scorePerSegment = def.score;
                data.particleColor = def.pColor;
                data.uncutPrefab = foodPrefabs[def.type].uncut;
                data.slicePiecePrefab = foodPrefabs[def.type].slice;
                data.icon = sprites.foodIcons.ContainsKey(def.type) ? sprites.foodIcons[def.type] : sprites.uiCircle;
                data.visualScale = Vector3.one;
                data.boardHeightOffset = 0f;

                if (data.uncutPrefab != null)
                {
                    FoodItem fi = data.uncutPrefab.GetComponent<FoodItem>();
                    if (fi != null)
                    {
                        SerializedObject fiSo = new SerializedObject(fi);
                        var fdp = fiSo.FindProperty("foodData");
                        if (fdp != null) fdp.objectReferenceValue = data;
                        var ftp = fiSo.FindProperty("foodType");
                        if (ftp != null) ftp.enumValueIndex = (int)def.type;
                        fiSo.ApplyModifiedPropertiesWithoutUndo();
                        PrefabUtility.SavePrefabAsset(data.uncutPrefab);
                    }
                }

                EditorUtility.SetDirty(data);
                list.Add(data);
            }

            return list;
        }

        private static List<KnifeData> CreateKnifeDataAssets(
            List<GameObject> knifePrefabs,
            (Sprite uiCircle, Sprite uiPillBg, Sprite uiPillFill, Sprite uiComboFrame, Sprite uiButton, Sprite uiCheckmark, Dictionary<FoodType, Sprite> foodIcons) sprites)
        {
            var list = new List<KnifeData>();

            var knifeDefs = new (string name, int price, bool unlocked)[]
            {
                ("Chef Knife", 0, true),
                ("Katana", 500, false),
                ("Meat Cleaver", 800, false),
                ("Fantasy Cyan Blade", 1200, false),
                ("Santoku Knife", 1500, false),
                ("Ornamental Gold Knife", 2500, false)
            };

            for (int i = 0; i < knifeDefs.Length; i++)
            {
                var def = knifeDefs[i];
                string path = $"{ROOT_PATH}/ScriptableObjects/Knives/KnifeData_{i}.asset";
                KnifeData kd = AssetDatabase.LoadAssetAtPath<KnifeData>(path);
                if (kd == null)
                {
                    kd = ScriptableObject.CreateInstance<KnifeData>();
                    AssetDatabase.CreateAsset(kd, path);
                }

                kd.knifeName = def.name;
                kd.knifeIndex = i;
                kd.price = def.price;
                kd.unlockedByDefault = def.unlocked;
                kd.knifePrefab = knifePrefabs[i];
                kd.icon = sprites.uiCircle;

                EditorUtility.SetDirty(kd);
                list.Add(kd);
            }

            return list;
        }

        private static List<LevelData> Create20LevelDataAssets(List<FoodData> foods)
        {
            var list = new List<LevelData>();

            FoodData GetFood(FoodType t) => foods.Find(f => f.foodType == t);

            // Level 1: Tailored to match Image 2 visual reference
            // Top Targets: Apple (2), Banana (3), Watermelon (2)
            // Food Sequence starts with Banana, Lemon, Watermelon, Apple so they are immediately visible in line!
            for (int lvl = 1; lvl <= 20; lvl++)
            {
                string path = $"{ROOT_PATH}/ScriptableObjects/Levels/LevelData_{lvl}.asset";
                LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<LevelData>();
                    AssetDatabase.CreateAsset(data, path);
                }

                data.levelNumber = lvl;
                data.rewardCoins = 50 + (lvl * 10);
                data.speedIncreasePerCut = 0.005f;
                data.maxConveyorSpeed = 4.2f;
                data.targetObjectives.Clear();
                data.foodSequence.Clear();

                if (lvl == 1)
                {
                    data.levelName = "Fruit Orchard 1";
                    data.conveyorSpeed = 1.3f;
                    data.spawnSpacing = 2.0f;
                    data.wrongFoodPenaltyMode = WrongFoodPenaltyMode.None;
                    data.boardHitMode = BoardHitMode.Penalty;

                    // 3 Target slots matching Image 2 (configured to 3 each per user specification)
                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Apple, requiredCount = 3 });
                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Banana, requiredCount = 3 });
                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Watermelon, requiredCount = 3 });

                    // Food sequence matching Image 2 line: Banana nearest, Lemon behind, Watermelon behind that, Apple behind that!
                    data.foodSequence.Add(GetFood(FoodType.Banana));
                    data.foodSequence.Add(GetFood(FoodType.Lemon));
                    data.foodSequence.Add(GetFood(FoodType.Watermelon));
                    data.foodSequence.Add(GetFood(FoodType.Apple));
                    data.foodSequence.Add(GetFood(FoodType.Banana));
                    data.foodSequence.Add(GetFood(FoodType.Apple));
                    data.foodSequence.Add(GetFood(FoodType.Watermelon));
                    data.foodSequence.Add(GetFood(FoodType.Banana));
                    data.foodSequence.Add(GetFood(FoodType.Lemon));
                    data.foodSequence.Add(GetFood(FoodType.Apple));
                    data.foodSequence.Add(GetFood(FoodType.Banana));
                    data.foodSequence.Add(GetFood(FoodType.Watermelon));
                }
                else if (lvl <= 3) // Levels 2-3
                {
                    data.levelName = $"Fruit Orchard {lvl}";
                    data.conveyorSpeed = 1.3f + (lvl * 0.15f);
                    data.spawnSpacing = 2.0f;
                    data.wrongFoodPenaltyMode = WrongFoodPenaltyMode.None;
                    data.boardHitMode = BoardHitMode.Penalty;

                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Apple, requiredCount = 8 });
                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Banana, requiredCount = 8 });
                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Watermelon, requiredCount = 6 });

                    for (int s = 0; s < 4; s++)
                    {
                        data.foodSequence.Add(GetFood(FoodType.Banana));
                        data.foodSequence.Add(GetFood(FoodType.Apple));
                        data.foodSequence.Add(GetFood(FoodType.Watermelon));
                        data.foodSequence.Add(GetFood(FoodType.Lemon));
                    }
                }
                else if (lvl <= 6) // Levels 4-6
                {
                    data.levelName = $"Garden Fresh {lvl}";
                    data.conveyorSpeed = 1.8f + ((lvl - 3) * 0.15f);
                    data.spawnSpacing = 1.9f;
                    data.wrongFoodPenaltyMode = WrongFoodPenaltyMode.LoseScore;
                    data.boardHitMode = BoardHitMode.Penalty;

                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Tomato, requiredCount = 8 });
                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Lemon, requiredCount = 8 });
                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Carrot, requiredCount = 8 });

                    for (int s = 0; s < 4; s++)
                    {
                        data.foodSequence.Add(GetFood(FoodType.Tomato));
                        data.foodSequence.Add(GetFood(FoodType.Lemon));
                        data.foodSequence.Add(GetFood(FoodType.Carrot));
                    }
                }
                else if (lvl <= 10) // Levels 7-10
                {
                    data.levelName = $"Veggie Harvest {lvl}";
                    data.conveyorSpeed = 2.3f + ((lvl - 6) * 0.15f);
                    data.spawnSpacing = 1.8f;
                    data.wrongFoodPenaltyMode = WrongFoodPenaltyMode.ResetCombo;
                    data.boardHitMode = BoardHitMode.ResetCombo;

                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.BellPepper, requiredCount = 8 });
                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Potato, requiredCount = 8 });
                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Carrot, requiredCount = 8 });

                    for (int s = 0; s < 4; s++)
                    {
                        data.foodSequence.Add(GetFood(FoodType.BellPepper));
                        data.foodSequence.Add(GetFood(FoodType.Potato));
                        data.foodSequence.Add(GetFood(FoodType.Carrot));
                        data.foodSequence.Add(GetFood(FoodType.Apple));
                    }
                }
                else if (lvl <= 14) // Levels 11-14
                {
                    data.levelName = $"Sweet Bakery {lvl}";
                    data.conveyorSpeed = 2.9f + ((lvl - 10) * 0.15f);
                    data.spawnSpacing = 1.7f;
                    data.wrongFoodPenaltyMode = WrongFoodPenaltyMode.LoseScore;
                    data.boardHitMode = BoardHitMode.Penalty;

                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.ChocolateCake, requiredCount = 8 });
                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.CreamCake, requiredCount = 8 });
                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.SwissRoll, requiredCount = 8 });

                    for (int s = 0; s < 4; s++)
                    {
                        data.foodSequence.Add(GetFood(FoodType.ChocolateCake));
                        data.foodSequence.Add(GetFood(FoodType.CreamCake));
                        data.foodSequence.Add(GetFood(FoodType.SwissRoll));
                        data.foodSequence.Add(GetFood(FoodType.Bread));
                    }
                }
                else // Levels 15-20
                {
                    data.levelName = $"Master Slicer {lvl}";
                    data.conveyorSpeed = 3.5f + ((lvl - 14) * 0.12f);
                    data.spawnSpacing = 1.6f;
                    data.wrongFoodPenaltyMode = WrongFoodPenaltyMode.ResetCombo;
                    data.boardHitMode = (lvl == 20) ? BoardHitMode.FailLevel : BoardHitMode.ResetCombo;

                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Bread, requiredCount = 10 });
                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Pie, requiredCount = 8 });
                    data.targetObjectives.Add(new TargetFoodObjective { foodType = FoodType.Watermelon, requiredCount = 8 });

                    for (int s = 0; s < 5; s++)
                    {
                        data.foodSequence.Add(GetFood(FoodType.Bread));
                        data.foodSequence.Add(GetFood(FoodType.Pie));
                        data.foodSequence.Add(GetFood(FoodType.Watermelon));
                        data.foodSequence.Add(GetFood(FoodType.Lemon));
                    }
                }

                EditorUtility.SetDirty(data);
                list.Add(data);
            }

            return list;
        }
        #endregion

        #region VFX Prefabs
        private static (GameObject sliceVfx, GameObject boardHitVfx, GameObject winVfx) CreateVFXPrefabs(Material particleMaterial)
        {
            string pathSlice = $"{ROOT_PATH}/Prefabs/VFX/VFX_SliceParticles.prefab";
            GameObject goSlice = new GameObject("VFX_SliceParticles");
            ParticleSystem psSlice = goSlice.AddComponent<ParticleSystem>();
            var mainSlice = psSlice.main;
            mainSlice.startLifetime = 0.45f;
            mainSlice.startSpeed = 3.0f;
            mainSlice.startSize = 0.09f;
            mainSlice.gravityModifier = 1.8f;
            mainSlice.loop = false;
            mainSlice.playOnAwake = false;

            var emissionSlice = psSlice.emission;
            emissionSlice.rateOverTime = 0;
            emissionSlice.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

            var shapeSlice = psSlice.shape;
            shapeSlice.shapeType = ParticleSystemShapeType.Sphere;
            shapeSlice.radius = 0.12f;

            var psrSlice = goSlice.GetComponent<ParticleSystemRenderer>();
            if (psrSlice != null) psrSlice.sharedMaterial = particleMaterial;

            GameObject prefabSlice = PrefabUtility.SaveAsPrefabAsset(goSlice, pathSlice);
            GameObject.DestroyImmediate(goSlice);

            string pathHit = $"{ROOT_PATH}/Prefabs/VFX/VFX_BoardHitSparks.prefab";
            GameObject goHit = new GameObject("VFX_BoardHitSparks");
            ParticleSystem psHit = goHit.AddComponent<ParticleSystem>();
            var mainHit = psHit.main;
            mainHit.startLifetime = 0.25f;
            mainHit.startSpeed = 2.5f;
            mainHit.startSize = 0.04f;
            mainHit.startColor = new Color(0.75f, 0.95f, 1.0f);
            mainHit.gravityModifier = 1.5f;
            mainHit.loop = false;
            mainHit.playOnAwake = false;

            var emissionHit = psHit.emission;
            emissionHit.rateOverTime = 0;
            emissionHit.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });

            var psrHit = goHit.GetComponent<ParticleSystemRenderer>();
            if (psrHit != null) psrHit.sharedMaterial = particleMaterial;

            GameObject prefabHit = PrefabUtility.SaveAsPrefabAsset(goHit, pathHit);
            GameObject.DestroyImmediate(goHit);

            string pathWin = $"{ROOT_PATH}/Prefabs/VFX/VFX_LevelWinConfetti.prefab";
            GameObject goWin = new GameObject("VFX_LevelWinConfetti");
            ParticleSystem psWin = goWin.AddComponent<ParticleSystem>();
            var mainWin = psWin.main;
            mainWin.startLifetime = 1.8f;
            mainWin.startSpeed = 5.0f;
            mainWin.startSize = 0.16f;
            mainWin.gravityModifier = 0.6f;
            mainWin.loop = false;
            mainWin.playOnAwake = false;

            var emissionWin = psWin.emission;
            emissionWin.rateOverTime = 0;
            emissionWin.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });

            var psrWin = goWin.GetComponent<ParticleSystemRenderer>();
            if (psrWin != null) psrWin.sharedMaterial = particleMaterial;

            GameObject prefabWin = PrefabUtility.SaveAsPrefabAsset(goWin, pathWin);
            GameObject.DestroyImmediate(goWin);

            return (prefabSlice, prefabHit, prefabWin);
        }
        #endregion

        #region Scene Construction: Gameplay Scene (Matching Image 2)
        private static void BuildGameplayScene(
            List<KnifeData> knifeDataList,
            List<LevelData> levelDataList,
            (GameObject sliceVfx, GameObject boardHitVfx, GameObject winVfx) vfxPrefabs,
            GameObject floatingTextPrefab,
            Dictionary<string, Material> materials,
            Dictionary<string, Mesh> meshes,
            (Sprite uiCircle, Sprite uiPillBg, Sprite uiPillFill, Sprite uiComboFrame, Sprite uiButton, Sprite uiCheckmark, Dictionary<FoodType, Sprite> foodIcons) sprites)
        {
            string scenePath = $"{ROOT_PATH}/Scenes/Gameplay.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Systems & Managers Root
            GameObject systemsRoot = new GameObject("--- SYSTEMS ---");
            GameManager gm = systemsRoot.AddComponent<GameManager>();
            GameStateManager gsm = systemsRoot.AddComponent<GameStateManager>();
            SaveManager sm = systemsRoot.AddComponent<SaveManager>();
            LevelManager lm = systemsRoot.AddComponent<LevelManager>();
            ScoreManager scm = systemsRoot.AddComponent<ScoreManager>();
            ComboManager cm = systemsRoot.AddComponent<ComboManager>();
            PoolManager pm = systemsRoot.AddComponent<PoolManager>();
            AudioManager am = systemsRoot.AddComponent<AudioManager>();
            VFXManager vfxMgr = systemsRoot.AddComponent<VFXManager>();
            HapticManager hm = systemsRoot.AddComponent<HapticManager>();
            HitStopManager hsm = systemsRoot.AddComponent<HitStopManager>();
            FloatingTextManager ftm = systemsRoot.AddComponent<FloatingTextManager>();
            KnifeInputController kic = systemsRoot.AddComponent<KnifeInputController>();
            TargetManager tm = systemsRoot.AddComponent<TargetManager>();

            // 2. Camera Rig (Lower, Closer, Perspective Depth matching Image 2)
            GameObject camRig = new GameObject("CameraRig");
            CameraFeedback camFeedback = camRig.AddComponent<CameraFeedback>();
            camRig.transform.position = new Vector3(0.30f, 2.58f, -2.50f);
            camRig.transform.rotation = Quaternion.Euler(26.8f, -5.2f, 3.5f);

            GameObject camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(camRig.transform, false);
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(1.0f, 0.88f, 0.42f);
            cam.fieldOfView = 52f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            camGo.AddComponent<AudioListener>();

            // Configure vibrant hypercasual ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.92f, 0.94f, 0.98f);
            RenderSettings.ambientSkyColor = new Color(0.92f, 0.94f, 0.98f);
            RenderSettings.ambientEquatorColor = new Color(0.92f, 0.94f, 0.98f);
            RenderSettings.ambientGroundColor = new Color(0.92f, 0.94f, 0.98f);

            // 3. Environment & Lighting
            GameObject env = new GameObject("Environment");

            // Main Directional Key Light (shines from front-left down onto knife and board)
            GameObject lightGo = new GameObject("Directional Light");
            lightGo.transform.SetParent(env.transform);
            lightGo.transform.rotation = Quaternion.Euler(42f, -22f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.98f, 0.95f);
            light.intensity = 1.45f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.22f;

            // Secondary Soft Fill Light
            GameObject fillLightGo = new GameObject("Fill Light");
            fillLightGo.transform.SetParent(env.transform);
            fillLightGo.transform.rotation = Quaternion.Euler(25f, 45f, 0f);
            Light fillLight = fillLightGo.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.90f, 0.95f, 1.0f);
            fillLight.intensity = 0.95f;
            fillLight.shadows = LightShadows.None;

            // Cutting Board (Glossy pure white runway, surface at Y = 0)
            GameObject board = new GameObject("CuttingBoard");
            board.transform.SetParent(env.transform);
            board.transform.position = new Vector3(0, -0.075f, 70f);
            MeshFilter bMf = board.AddComponent<MeshFilter>();
            bMf.sharedMesh = meshes["Mesh_CuttingBoard"];
            MeshRenderer bMr = board.AddComponent<MeshRenderer>();
            bMr.sharedMaterial = materials["Mat_CuttingBoard"];
            bMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bMr.receiveShadows = true;

            // Glowing Neon Cyan Rails on Left & Right Edges matching Image 2
            GameObject railL = new GameObject("LeftEdge");
            railL.transform.SetParent(board.transform, false);
            railL.transform.localPosition = new Vector3(-0.48f, 0.06f, 0);
            MeshFilter rLmf = railL.AddComponent<MeshFilter>();
            rLmf.sharedMesh = meshes["Mesh_NeonRail"];
            MeshRenderer rLmr = railL.AddComponent<MeshRenderer>();
            rLmr.sharedMaterial = materials["Mat_NeonCyanEdge"];
            rLmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject railR = new GameObject("RightEdge");
            railR.transform.SetParent(board.transform, false);
            railR.transform.localPosition = new Vector3(0.48f, 0.06f, 0);
            MeshFilter rRmf = railR.AddComponent<MeshFilter>();
            rRmf.sharedMesh = meshes["Mesh_NeonRail"];
            MeshRenderer rRmr = railR.AddComponent<MeshRenderer>();
            rRmr.sharedMaterial = materials["Mat_NeonCyanEdge"];
            rRmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Level Complete Presentation Rig & Checkerboard Pillow
            GameObject lcpGo = new GameObject("LevelCompletePresentation");
            lcpGo.transform.SetParent(env.transform, false);
            LevelCompletePresentation lcp = lcpGo.AddComponent<LevelCompletePresentation>();

            GameObject rigGo = new GameObject("CompletionRig");
            rigGo.transform.SetParent(lcpGo.transform, false);

            // Pillow placed directly on cutting board surface at world (0, 0.045f, 0.65f)
            GameObject pillowGo = new GameObject("Pillow");
            pillowGo.transform.SetParent(rigGo.transform, false);
            pillowGo.transform.position = new Vector3(0f, 0.045f, 0.65f);
            MeshFilter pillowMf = pillowGo.AddComponent<MeshFilter>();
            pillowMf.sharedMesh = meshes["Mesh_Pillow"];
            MeshRenderer pillowMr = pillowGo.AddComponent<MeshRenderer>();
            pillowMr.sharedMaterial = materials["Mat_Pillow"];

            // Gold perimeter piping
            GameObject pipingGo = new GameObject("Piping");
            pipingGo.transform.SetParent(pillowGo.transform, false);
            pipingGo.transform.localPosition = Vector3.zero;
            MeshFilter pipingMf = pipingGo.AddComponent<MeshFilter>();
            pipingMf.sharedMesh = meshes["Mesh_PillowPiping"];
            MeshRenderer pipingMr = pipingGo.AddComponent<MeshRenderer>();
            pipingMr.sharedMaterial = materials["Mat_PillowGoldTrim"];

            // 4 Corner Gold Tassels
            float tasselX = 0.20f;
            float tasselZ = 0.20f;
            Vector3[] tasselPos = new Vector3[]
            {
                new Vector3(-tasselX, 0.01f, -tasselZ),
                new Vector3( tasselX, 0.01f, -tasselZ),
                new Vector3( tasselX, 0.01f,  tasselZ),
                new Vector3(-tasselX, 0.01f,  tasselZ)
            };
            for (int ti = 0; ti < 4; ti++)
            {
                GameObject tGo = new GameObject($"Tassel_{ti}");
                tGo.transform.SetParent(pillowGo.transform, false);
                tGo.transform.localPosition = tasselPos[ti];
                MeshFilter tMf = tGo.AddComponent<MeshFilter>();
                tMf.sharedMesh = meshes["Mesh_CornerTassel"];
                MeshRenderer tMr = tGo.AddComponent<MeshRenderer>();
                tMr.sharedMaterial = materials["Mat_PillowGoldTrim"];
            }

            // KnifeRestPoint: diagonal rest pose on pillow
            // Blade points upper-left (~10 o'clock), handle points lower-right (~4 o'clock)
            GameObject restPointGo = new GameObject("KnifeRestPoint");
            restPointGo.transform.SetParent(pillowGo.transform, false);
            restPointGo.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            restPointGo.transform.localRotation = Quaternion.Euler(14f, 42f, -18f);

            // FirstFlipApex: above and slightly right of pillow
            GameObject firstApexGo = new GameObject("FirstFlipApex");
            firstApexGo.transform.SetParent(rigGo.transform, false);
            firstApexGo.transform.position = new Vector3(0.14f, 0.64f, 0.69f);

            // SecondFlipApex: lower and slightly closer (70-80% height of first flip)
            GameObject secondApexGo = new GameObject("SecondFlipApex");
            secondApexGo.transform.SetParent(rigGo.transform, false);
            secondApexGo.transform.position = new Vector3(0.09f, 0.48f, 0.68f);

            // Confetti Left & Right and Celebration Streaks
            ParticleSystem confLeft = CreateCelebrationConfetti("ConfettiLeft", rigGo.transform, new Vector3(-0.35f, 0.05f, 0.70f), materials["Mat_Particle_Unlit"]);
            ParticleSystem confRight = CreateCelebrationConfetti("ConfettiRight", rigGo.transform, new Vector3(0.35f, 0.05f, 0.70f), materials["Mat_Particle_Unlit"]);
            ParticleSystem celebVfx = CreateCelebrationStreaks("CelebrationParticles", rigGo.transform, new Vector3(0f, 0.08f, 0.75f), materials["Mat_Particle_Unlit"]);

            pillowGo.SetActive(false);

            // Unlit Vertical Gradient Background Plane parented to Camera
            GameObject bgPlane = new GameObject("GradientBackground");
            bgPlane.transform.SetParent(camGo.transform, false);
            bgPlane.transform.localPosition = new Vector3(0, 0, 22.0f);
            bgPlane.transform.localRotation = Quaternion.identity;
            MeshFilter bgMf = bgPlane.AddComponent<MeshFilter>();
            bgMf.sharedMesh = meshes["Mesh_BackgroundPlane"];
            MeshRenderer bgMr = bgPlane.AddComponent<MeshRenderer>();
            bgMr.sharedMaterial = materials["Mat_GradientBG"];
            bgMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bgMr.receiveShadows = false;

            // 4. Gameplay Spawn Anchors
            GameObject gameplayRoot = new GameObject("Gameplay");

            // Knife Spawn Anchor (Lower-Right Foreground matching Image 2)
            GameObject knifeSpawn = new GameObject("KnifeSpawn");
            knifeSpawn.transform.SetParent(gameplayRoot.transform);
            knifeSpawn.transform.position = new Vector3(0.40f, 0.42f, 0.18f);
            knifeSpawn.transform.rotation = Quaternion.Euler(8f, 22f, -10f);

            GameObject spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(env.transform);
            spawnPoint.transform.position = new Vector3(0, 0.05f, 22f);

            GameObject foodContainer = new GameObject("FoodContainer");
            foodContainer.transform.SetParent(gameplayRoot.transform);

            FoodSpawner spawner = systemsRoot.AddComponent<FoodSpawner>();
            SerializedObject spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("spawnPoint").objectReferenceValue = spawnPoint.transform;
            spawnerSo.FindProperty("foodContainer").objectReferenceValue = foodContainer.transform;
            spawnerSo.FindProperty("defaultSpawnDistanceZ").floatValue = 22f;
            spawnerSo.FindProperty("defaultSpacing").floatValue = 3.5f;
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();

            // 5. UI Hierarchy (Portrait 1080 x 1920)
            GameObject canvasGo = new GameObject("UI_Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Safe Area Container
            GameObject safeAreaGo = new GameObject("SafeArea");
            safeAreaGo.transform.SetParent(canvasGo.transform, false);
            RectTransform saRt = safeAreaGo.AddComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero;
            saRt.anchorMax = Vector2.one;
            saRt.sizeDelta = Vector2.zero;
            safeAreaGo.AddComponent<SafeAreaController>();

            // HUD
            GameObject hud = new GameObject("HUD");
            hud.transform.SetParent(safeAreaGo.transform, false);
            RectTransform hudRt = hud.AddComponent<RectTransform>();
            hudRt.anchorMin = Vector2.zero;
            hudRt.anchorMax = Vector2.one;
            hudRt.sizeDelta = Vector2.zero;

            // Upper-Left Score & Multiplier Block matching Image 2
            GameObject scoreBlock = new GameObject("ScoreBlock");
            scoreBlock.transform.SetParent(hud.transform, false);
            RectTransform sbRt = scoreBlock.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0, 1);
            sbRt.anchorMax = new Vector2(0, 1);
            sbRt.pivot = new Vector2(0, 1);
            sbRt.anchoredPosition = new Vector2(48, -125);
            sbRt.sizeDelta = new Vector2(300, 440);

            // Score Text (Large bold italic white matching Image 2: "1528")
            GameObject scoreTextGo = new GameObject("ScoreText");
            scoreTextGo.transform.SetParent(scoreBlock.transform, false);
            RectTransform stRt = scoreTextGo.AddComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0, 1);
            stRt.anchorMax = new Vector2(0, 1);
            stRt.pivot = new Vector2(0, 1);
            stRt.anchoredPosition = new Vector2(0, 0);
            stRt.sizeDelta = new Vector2(280, 85);
            Text scoreText = scoreTextGo.AddComponent<Text>();
            scoreText.text = "1528";
            scoreText.font = GetDefaultFont();
            scoreText.fontSize = 76;
            scoreText.fontStyle = FontStyle.BoldAndItalic;
            scoreText.color = Color.white;
            scoreText.alignment = TextAnchor.MiddleLeft;

            // Multiplier Text (Large bold italic lemon-yellow matching Image 2: "x2")
            GameObject multTextGo = new GameObject("MultiplierText");
            multTextGo.transform.SetParent(scoreBlock.transform, false);
            RectTransform mtRt = multTextGo.AddComponent<RectTransform>();
            mtRt.anchorMin = new Vector2(0, 1);
            mtRt.anchorMax = new Vector2(0, 1);
            mtRt.pivot = new Vector2(0, 1);
            mtRt.anchoredPosition = new Vector2(0, -82);
            mtRt.sizeDelta = new Vector2(250, 75);
            Text multText = multTextGo.AddComponent<Text>();
            multText.text = "x2";
            multText.font = GetDefaultFont();
            multText.fontSize = 66;
            multText.fontStyle = FontStyle.BoldAndItalic;
            multText.color = new Color(1.0f, 0.94f, 0.05f);
            multText.alignment = TextAnchor.MiddleLeft;

            // Vertical Tapered Combo Meter matching Image 2
            GameObject comboMeterGo = new GameObject("ComboMeter");
            comboMeterGo.transform.SetParent(scoreBlock.transform, false);
            RectTransform cmRt = comboMeterGo.AddComponent<RectTransform>();
            cmRt.anchorMin = new Vector2(0, 1);
            cmRt.anchorMax = new Vector2(0, 1);
            cmRt.pivot = new Vector2(0, 1);
            cmRt.anchoredPosition = new Vector2(8, -172);
            cmRt.sizeDelta = new Vector2(95, 230);
            Image cmBg = comboMeterGo.AddComponent<Image>();
            cmBg.sprite = sprites.uiComboFrame;
            ComboMeterUI comboMeterUi = comboMeterGo.AddComponent<ComboMeterUI>();

            List<Image> comboSegments = new List<Image>();
            for (int seg = 0; seg < 5; seg++)
            {
                GameObject segGo = new GameObject($"Seg_{seg}");
                segGo.transform.SetParent(comboMeterGo.transform, false);
                RectTransform segRt = segGo.AddComponent<RectTransform>();
                segRt.anchorMin = new Vector2(0.5f, 0);
                segRt.anchorMax = new Vector2(0.5f, 0);
                segRt.pivot = new Vector2(0.5f, 0);
                segRt.anchoredPosition = new Vector2(0, seg * 43 + 6);
                float segW = Mathf.Lerp(45f, 85f, (float)seg / 4f);
                segRt.sizeDelta = new Vector2(segW, 38);
                Image img = segGo.AddComponent<Image>();
                img.sprite = sprites.uiPillFill;
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Vertical;
                // Bottom segment active bright green like Image 2
                img.color = seg == 0 ? new Color(0.1f, 1.0f, 0.3f) : new Color(1.0f, 1.0f, 1.0f, 0.12f);
                comboSegments.Add(img);
            }

            SerializedObject cmSo = new SerializedObject(comboMeterUi);
            SerializedProperty segListProp = cmSo.FindProperty("segmentFills");
            segListProp.ClearArray();
            for (int s = 0; s < comboSegments.Count; s++)
            {
                segListProp.InsertArrayElementAtIndex(s);
                segListProp.GetArrayElementAtIndex(s).objectReferenceValue = comboSegments[s];
            }
            cmSo.ApplyModifiedPropertiesWithoutUndo();

            // Top Target Panel (3 Circular Target Slots matching Image 2)
            GameObject targetPanelGo = new GameObject("TargetPanel");
            targetPanelGo.transform.SetParent(hud.transform, false);
            RectTransform tpRt = targetPanelGo.AddComponent<RectTransform>();
            tpRt.anchorMin = new Vector2(0.5f, 1);
            tpRt.anchorMax = new Vector2(0.5f, 1);
            tpRt.pivot = new Vector2(0.5f, 1);
            tpRt.anchoredPosition = new Vector2(0, -45);
            tpRt.sizeDelta = new Vector2(660, 180);

            List<TargetSlotUI> targetSlotUiList = new List<TargetSlotUI>();
            float[] slotOffsetsX = new float[] { -210, 0, 210 };
            FoodType[] initialIcons = new FoodType[] { FoodType.Apple, FoodType.Banana, FoodType.Watermelon };
            Color[] slotFillColors = new Color[] {
                new Color(1.0f, 0.18f, 0.18f), // Red
                new Color(1.0f, 0.72f, 0.05f), // Yellow/Amber
                new Color(0.0f, 0.88f, 0.35f)  // Green
            };

            for (int s = 0; s < 3; s++)
            {
                GameObject slotGo = new GameObject($"TargetSlot_{s + 1}");
                slotGo.transform.SetParent(targetPanelGo.transform, false);
                RectTransform sRt = slotGo.AddComponent<RectTransform>();
                sRt.anchoredPosition = new Vector2(slotOffsetsX[s], 0);
                sRt.sizeDelta = new Vector2(165, 175);

                // Circular Card Backing
                GameObject circleGo = new GameObject("CircleBacking");
                circleGo.transform.SetParent(slotGo.transform, false);
                RectTransform cRt = circleGo.AddComponent<RectTransform>();
                cRt.anchoredPosition = new Vector2(0, 15);
                cRt.sizeDelta = new Vector2(142, 142);
                Image cImg = circleGo.AddComponent<Image>();
                cImg.sprite = sprites.uiCircle;

                // Food Icon
                GameObject iconGo = new GameObject("FoodIcon");
                iconGo.transform.SetParent(circleGo.transform, false);
                RectTransform iRt = iconGo.AddComponent<RectTransform>();
                iRt.sizeDelta = new Vector2(102, 102);
                Image iImg = iconGo.AddComponent<Image>();
                iImg.sprite = sprites.foodIcons[initialIcons[s]];

                // Pill Progress Bar Backing
                GameObject barBgGo = new GameObject("BarBacking");
                barBgGo.transform.SetParent(slotGo.transform, false);
                RectTransform bbgRt = barBgGo.AddComponent<RectTransform>();
                bbgRt.anchoredPosition = new Vector2(0, -62);
                bbgRt.sizeDelta = new Vector2(120, 18);
                Image bbgImg = barBgGo.AddComponent<Image>();
                bbgImg.sprite = sprites.uiPillBg;

                // Pill Progress Bar Fill
                GameObject barFillGo = new GameObject("BarFill");
                barFillGo.transform.SetParent(barBgGo.transform, false);
                RectTransform bfRt = barFillGo.AddComponent<RectTransform>();
                bfRt.anchorMin = Vector2.zero;
                bfRt.anchorMax = Vector2.one;
                bfRt.sizeDelta = Vector2.zero;
                Image bfImg = barFillGo.AddComponent<Image>();
                bfImg.sprite = sprites.uiPillFill;
                bfImg.type = Image.Type.Filled;
                bfImg.fillMethod = Image.FillMethod.Horizontal;
                bfImg.fillAmount = 0f;
                bfImg.color = slotFillColors[s];

                // Checkmark Badge
                GameObject checkGo = new GameObject("Checkmark");
                checkGo.transform.SetParent(circleGo.transform, false);
                RectTransform chkRt = checkGo.AddComponent<RectTransform>();
                chkRt.anchorMin = new Vector2(1, 1);
                chkRt.anchorMax = new Vector2(1, 1);
                chkRt.pivot = new Vector2(1, 1);
                chkRt.anchoredPosition = new Vector2(6, 6);
                chkRt.sizeDelta = new Vector2(48, 48);
                Image chkImg = checkGo.AddComponent<Image>();
                chkImg.sprite = sprites.uiCheckmark;
                checkGo.SetActive(false);

                TargetSlotUI slotUi = slotGo.AddComponent<TargetSlotUI>();
                SerializedObject tsSo = new SerializedObject(slotUi);
                tsSo.FindProperty("iconImage").objectReferenceValue = iImg;
                tsSo.FindProperty("progressFillImage").objectReferenceValue = bfImg;
                tsSo.FindProperty("checkmarkObject").objectReferenceValue = checkGo;
                tsSo.FindProperty("normalColor").colorValue = slotFillColors[s];
                tsSo.FindProperty("completedColor").colorValue = new Color(0.18f, 0.85f, 0.35f);
                tsSo.ApplyModifiedPropertiesWithoutUndo();

                targetSlotUiList.Add(slotUi);
            }

            // Top-Right Pause Button (Hidden by default to match clean reference HUD)
            GameObject pauseBtnGo = new GameObject("PauseButton");
            pauseBtnGo.transform.SetParent(hud.transform, false);
            RectTransform pbRt = pauseBtnGo.AddComponent<RectTransform>();
            pbRt.anchorMin = new Vector2(1, 1);
            pbRt.anchorMax = new Vector2(1, 1);
            pbRt.pivot = new Vector2(1, 1);
            pbRt.anchoredPosition = new Vector2(-45, -50);
            pbRt.sizeDelta = new Vector2(75, 75);
            Image pbImg = pauseBtnGo.AddComponent<Image>();
            pbImg.sprite = sprites.uiCircle;
            Button pauseBtn = pauseBtnGo.AddComponent<Button>();

            // Pause Icon Text "||"
            GameObject pbTextGo = new GameObject("Icon");
            pbTextGo.transform.SetParent(pauseBtnGo.transform, false);
            RectTransform pbtRt = pbTextGo.AddComponent<RectTransform>();
            pbtRt.anchorMin = Vector2.zero;
            pbtRt.anchorMax = Vector2.one;
            pbtRt.sizeDelta = Vector2.zero;
            Text pbText = pbTextGo.AddComponent<Text>();
            pbText.text = "II";
            pbText.font = GetDefaultFont();
            pbText.fontSize = 32;
            pbText.fontStyle = FontStyle.Bold;
            pbText.color = new Color(0.2f, 0.4f, 0.6f);
            pbText.alignment = TextAnchor.MiddleCenter;
            pauseBtnGo.SetActive(false);

            // 6. Level Complete Panel (Card, Title, Score, Coins, Next, Replay, Home)
            GameObject completePanelGo = new GameObject("LevelCompletePanel");
            completePanelGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform cpRt = completePanelGo.AddComponent<RectTransform>();
            cpRt.anchorMin = Vector2.zero;
            cpRt.anchorMax = Vector2.one;
            cpRt.sizeDelta = Vector2.zero;
            Image cpBg = completePanelGo.AddComponent<Image>();
            cpBg.color = new Color(0, 0, 0, 0.65f);

            GameObject completeCardGo = new GameObject("Card");
            completeCardGo.transform.SetParent(completePanelGo.transform, false);
            RectTransform cardRt = completeCardGo.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(840, 980);
            Image cardImg = completeCardGo.AddComponent<Image>();
            cardImg.color = new Color(1.0f, 1.0f, 1.0f, 0.96f);

            GameObject winTitleGo = new GameObject("Title");
            winTitleGo.transform.SetParent(completeCardGo.transform, false);
            RectTransform wtRt = winTitleGo.AddComponent<RectTransform>();
            wtRt.anchorMin = new Vector2(0.5f, 1);
            wtRt.anchorMax = new Vector2(0.5f, 1);
            wtRt.pivot = new Vector2(0.5f, 1);
            wtRt.anchoredPosition = new Vector2(0, -60);
            wtRt.sizeDelta = new Vector2(760, 110);
            Text winTitleTxt = winTitleGo.AddComponent<Text>();
            winTitleTxt.text = "LEVEL COMPLETE!";
            winTitleTxt.font = GetDefaultFont();
            winTitleTxt.fontSize = 62;
            winTitleTxt.fontStyle = FontStyle.BoldAndItalic;
            winTitleTxt.alignment = TextAnchor.MiddleCenter;
            winTitleTxt.color = new Color(0.96f, 0.10f, 0.58f);

            GameObject winScoreGo = new GameObject("ScoreText");
            winScoreGo.transform.SetParent(completeCardGo.transform, false);
            RectTransform wsRt = winScoreGo.AddComponent<RectTransform>();
            wsRt.anchorMin = new Vector2(0.5f, 1);
            wsRt.anchorMax = new Vector2(0.5f, 1);
            wsRt.pivot = new Vector2(0.5f, 1);
            wsRt.anchoredPosition = new Vector2(0, -220);
            wsRt.sizeDelta = new Vector2(700, 80);
            Text winScoreTxt = winScoreGo.AddComponent<Text>();
            winScoreTxt.text = "SCORE: 0";
            winScoreTxt.font = GetDefaultFont();
            winScoreTxt.fontSize = 54;
            winScoreTxt.fontStyle = FontStyle.Bold;
            winScoreTxt.alignment = TextAnchor.MiddleCenter;
            winScoreTxt.color = new Color(0.2f, 0.25f, 0.3f);

            GameObject winCoinsGo = new GameObject("CoinsText");
            winCoinsGo.transform.SetParent(completeCardGo.transform, false);
            RectTransform wcRt = winCoinsGo.AddComponent<RectTransform>();
            wcRt.anchorMin = new Vector2(0.5f, 1);
            wcRt.anchorMax = new Vector2(0.5f, 1);
            wcRt.pivot = new Vector2(0.5f, 1);
            wcRt.anchoredPosition = new Vector2(0, -320);
            wcRt.sizeDelta = new Vector2(700, 80);
            Text winCoinsTxt = winCoinsGo.AddComponent<Text>();
            winCoinsTxt.text = "+50 COINS";
            winCoinsTxt.font = GetDefaultFont();
            winCoinsTxt.fontSize = 48;
            winCoinsTxt.fontStyle = FontStyle.Bold;
            winCoinsTxt.alignment = TextAnchor.MiddleCenter;
            winCoinsTxt.color = new Color(1.0f, 0.75f, 0.05f);

            // Next Level Button
            GameObject nextBtnGo = new GameObject("NextLevelButton");
            nextBtnGo.transform.SetParent(completeCardGo.transform, false);
            RectTransform nbRt = nextBtnGo.AddComponent<RectTransform>();
            nbRt.anchorMin = new Vector2(0.5f, 0);
            nbRt.anchorMax = new Vector2(0.5f, 0);
            nbRt.pivot = new Vector2(0.5f, 0);
            nbRt.anchoredPosition = new Vector2(0, 310);
            nbRt.sizeDelta = new Vector2(600, 115);
            Image nbImg = nextBtnGo.AddComponent<Image>();
            nbImg.color = new Color(0.12f, 0.85f, 0.38f);
            Button nextBtn = nextBtnGo.AddComponent<Button>();

            GameObject nbtGo = new GameObject("Text");
            nbtGo.transform.SetParent(nextBtnGo.transform, false);
            RectTransform nbtRt = nbtGo.AddComponent<RectTransform>();
            nbtRt.anchorMin = Vector2.zero;
            nbtRt.anchorMax = Vector2.one;
            nbtRt.sizeDelta = Vector2.zero;
            Text nbTxt = nbtGo.AddComponent<Text>();
            nbTxt.text = "NEXT LEVEL";
            nbTxt.font = GetDefaultFont();
            nbTxt.fontSize = 46;
            nbTxt.fontStyle = FontStyle.Bold;
            nbTxt.alignment = TextAnchor.MiddleCenter;
            nbTxt.color = Color.white;

            // Replay Button
            GameObject replayBtnGo = new GameObject("ReplayButton");
            replayBtnGo.transform.SetParent(completeCardGo.transform, false);
            RectTransform rbRt = replayBtnGo.AddComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(0.5f, 0);
            rbRt.anchorMax = new Vector2(0.5f, 0);
            rbRt.pivot = new Vector2(0.5f, 0);
            rbRt.anchoredPosition = new Vector2(-160, 160);
            rbRt.sizeDelta = new Vector2(270, 95);
            Image rbImg = replayBtnGo.AddComponent<Image>();
            rbImg.color = new Color(0.35f, 0.65f, 0.95f);
            Button replayBtn = replayBtnGo.AddComponent<Button>();

            GameObject rbtGo = new GameObject("Text");
            rbtGo.transform.SetParent(replayBtnGo.transform, false);
            RectTransform rbtRt = rbtGo.AddComponent<RectTransform>();
            rbtRt.anchorMin = Vector2.zero;
            rbtRt.anchorMax = Vector2.one;
            rbtRt.sizeDelta = Vector2.zero;
            Text rbTxt = rbtGo.AddComponent<Text>();
            rbTxt.text = "REPLAY";
            rbTxt.font = GetDefaultFont();
            rbTxt.fontSize = 38;
            rbTxt.fontStyle = FontStyle.Bold;
            rbTxt.alignment = TextAnchor.MiddleCenter;
            rbTxt.color = Color.white;

            // Home Button
            GameObject homeBtnGo = new GameObject("HomeButton");
            homeBtnGo.transform.SetParent(completeCardGo.transform, false);
            RectTransform hbRt = homeBtnGo.AddComponent<RectTransform>();
            hbRt.anchorMin = new Vector2(0.5f, 0);
            hbRt.anchorMax = new Vector2(0.5f, 0);
            hbRt.pivot = new Vector2(0.5f, 0);
            hbRt.anchoredPosition = new Vector2(160, 160);
            hbRt.sizeDelta = new Vector2(270, 95);
            Image hbImg = homeBtnGo.AddComponent<Image>();
            hbImg.color = new Color(0.92f, 0.40f, 0.35f);
            Button homeBtn = homeBtnGo.AddComponent<Button>();

            GameObject hbtGo = new GameObject("Text");
            hbtGo.transform.SetParent(homeBtnGo.transform, false);
            RectTransform hbtRt = hbtGo.AddComponent<RectTransform>();
            hbtRt.anchorMin = Vector2.zero;
            hbtRt.anchorMax = Vector2.one;
            hbtRt.sizeDelta = Vector2.zero;
            Text hbTxt = hbtGo.AddComponent<Text>();
            hbTxt.text = "MAIN MENU";
            hbTxt.font = GetDefaultFont();
            hbTxt.fontSize = 34;
            hbTxt.fontStyle = FontStyle.Bold;
            hbTxt.alignment = TextAnchor.MiddleCenter;
            hbTxt.color = Color.white;

            completePanelGo.SetActive(false);

            // 7. Level Failed Panel
            GameObject failPanelGo = new GameObject("LevelFailedPanel");
            failPanelGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform fpRt = failPanelGo.AddComponent<RectTransform>();
            fpRt.anchorMin = Vector2.zero;
            fpRt.anchorMax = Vector2.one;
            fpRt.sizeDelta = Vector2.zero;
            Image fpBg = failPanelGo.AddComponent<Image>();
            fpBg.color = new Color(0, 0, 0, 0.65f);

            GameObject failCardGo = new GameObject("Card");
            failCardGo.transform.SetParent(failPanelGo.transform, false);
            RectTransform fcRt = failCardGo.AddComponent<RectTransform>();
            fcRt.anchorMin = new Vector2(0.5f, 0.5f);
            fcRt.anchorMax = new Vector2(0.5f, 0.5f);
            fcRt.pivot = new Vector2(0.5f, 0.5f);
            fcRt.sizeDelta = new Vector2(800, 800);
            Image fcImg = failCardGo.AddComponent<Image>();
            fcImg.color = new Color(1f, 1f, 1f, 0.96f);

            GameObject failTitleGo = new GameObject("Title");
            failTitleGo.transform.SetParent(failCardGo.transform, false);
            RectTransform ftRt = failTitleGo.AddComponent<RectTransform>();
            ftRt.anchorMin = new Vector2(0.5f, 1);
            ftRt.anchorMax = new Vector2(0.5f, 1);
            ftRt.pivot = new Vector2(0.5f, 1);
            ftRt.anchoredPosition = new Vector2(0, -60);
            ftRt.sizeDelta = new Vector2(700, 100);
            Text failTitleTxt = failTitleGo.AddComponent<Text>();
            failTitleTxt.text = "LEVEL FAILED";
            failTitleTxt.font = GetDefaultFont();
            failTitleTxt.fontSize = 58;
            failTitleTxt.fontStyle = FontStyle.Bold;
            failTitleTxt.alignment = TextAnchor.MiddleCenter;
            failTitleTxt.color = new Color(0.92f, 0.15f, 0.15f);

            GameObject failReasonGo = new GameObject("ReasonText");
            failReasonGo.transform.SetParent(failCardGo.transform, false);
            RectTransform frRt = failReasonGo.AddComponent<RectTransform>();
            frRt.anchorMin = new Vector2(0.5f, 1);
            frRt.anchorMax = new Vector2(0.5f, 1);
            frRt.pivot = new Vector2(0.5f, 1);
            frRt.anchoredPosition = new Vector2(0, -200);
            frRt.sizeDelta = new Vector2(650, 80);
            Text failReasonTxt = failReasonGo.AddComponent<Text>();
            failReasonTxt.text = "Try again!";
            failReasonTxt.font = GetDefaultFont();
            failReasonTxt.fontSize = 42;
            failReasonTxt.alignment = TextAnchor.MiddleCenter;
            failReasonTxt.color = new Color(0.4f, 0.4f, 0.45f);

            GameObject retryBtnGo = new GameObject("RetryButton");
            retryBtnGo.transform.SetParent(failCardGo.transform, false);
            RectTransform rtyRt = retryBtnGo.AddComponent<RectTransform>();
            rtyRt.anchorMin = new Vector2(0.5f, 0);
            rtyRt.anchorMax = new Vector2(0.5f, 0);
            rtyRt.pivot = new Vector2(0.5f, 0);
            rtyRt.anchoredPosition = new Vector2(0, 240);
            rtyRt.sizeDelta = new Vector2(500, 105);
            Image rtyImg = retryBtnGo.AddComponent<Image>();
            rtyImg.color = new Color(0.95f, 0.45f, 0.15f);
            Button retryBtn = retryBtnGo.AddComponent<Button>();

            GameObject rtyTxtGo = new GameObject("Text");
            rtyTxtGo.transform.SetParent(retryBtnGo.transform, false);
            RectTransform rtytRt = rtyTxtGo.AddComponent<RectTransform>();
            rtytRt.anchorMin = Vector2.zero;
            rtytRt.anchorMax = Vector2.one;
            rtytRt.sizeDelta = Vector2.zero;
            Text rtyTxt = rtyTxtGo.AddComponent<Text>();
            rtyTxt.text = "TRY AGAIN";
            rtyTxt.font = GetDefaultFont();
            rtyTxt.fontSize = 44;
            rtyTxt.fontStyle = FontStyle.Bold;
            rtyTxt.alignment = TextAnchor.MiddleCenter;
            rtyTxt.color = Color.white;

            GameObject failHomeGo = new GameObject("FailHomeButton");
            failHomeGo.transform.SetParent(failCardGo.transform, false);
            RectTransform fhRt = failHomeGo.AddComponent<RectTransform>();
            fhRt.anchorMin = new Vector2(0.5f, 0);
            fhRt.anchorMax = new Vector2(0.5f, 0);
            fhRt.pivot = new Vector2(0.5f, 0);
            fhRt.anchoredPosition = new Vector2(0, 110);
            fhRt.sizeDelta = new Vector2(380, 85);
            Image fhImg = failHomeGo.AddComponent<Image>();
            fhImg.color = new Color(0.5f, 0.55f, 0.6f);
            Button failHomeBtn = failHomeGo.AddComponent<Button>();

            GameObject fhtGo = new GameObject("Text");
            fhtGo.transform.SetParent(failHomeGo.transform, false);
            RectTransform fhtRt = fhtGo.AddComponent<RectTransform>();
            fhtRt.anchorMin = Vector2.zero;
            fhtRt.anchorMax = Vector2.one;
            fhtRt.sizeDelta = Vector2.zero;
            Text fhTxt = fhtGo.AddComponent<Text>();
            fhTxt.text = "MAIN MENU";
            fhTxt.font = GetDefaultFont();
            fhTxt.fontSize = 34;
            fhTxt.fontStyle = FontStyle.Bold;
            fhTxt.alignment = TextAnchor.MiddleCenter;
            fhTxt.color = Color.white;

            failPanelGo.SetActive(false);

            // 8. Pause Panel
            GameObject pausePanelGo = new GameObject("PausePanel");
            pausePanelGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform ppRt = pausePanelGo.AddComponent<RectTransform>();
            ppRt.anchorMin = Vector2.zero;
            ppRt.anchorMax = Vector2.one;
            ppRt.sizeDelta = Vector2.zero;
            Image ppBg = pausePanelGo.AddComponent<Image>();
            ppBg.color = new Color(0, 0, 0, 0.65f);

            GameObject pauseCardGo = new GameObject("Card");
            pauseCardGo.transform.SetParent(pausePanelGo.transform, false);
            RectTransform pcRt = pauseCardGo.AddComponent<RectTransform>();
            pcRt.anchorMin = new Vector2(0.5f, 0.5f);
            pcRt.anchorMax = new Vector2(0.5f, 0.5f);
            pcRt.pivot = new Vector2(0.5f, 0.5f);
            pcRt.sizeDelta = new Vector2(760, 680);
            Image pcImg = pauseCardGo.AddComponent<Image>();
            pcImg.color = new Color(1f, 1f, 1f, 0.96f);

            GameObject pauseTitleGo = new GameObject("Title");
            pauseTitleGo.transform.SetParent(pauseCardGo.transform, false);
            RectTransform ptRt = pauseTitleGo.AddComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(0.5f, 1);
            ptRt.anchorMax = new Vector2(0.5f, 1);
            ptRt.pivot = new Vector2(0.5f, 1);
            ptRt.anchoredPosition = new Vector2(0, -60);
            ptRt.sizeDelta = new Vector2(650, 100);
            Text pauseTitleTxt = pauseTitleGo.AddComponent<Text>();
            pauseTitleTxt.text = "PAUSED";
            pauseTitleTxt.font = GetDefaultFont();
            pauseTitleTxt.fontSize = 58;
            pauseTitleTxt.fontStyle = FontStyle.Bold;
            pauseTitleTxt.alignment = TextAnchor.MiddleCenter;
            pauseTitleTxt.color = new Color(0.2f, 0.25f, 0.35f);

            GameObject resumeBtnGo = new GameObject("ResumeButton");
            resumeBtnGo.transform.SetParent(pauseCardGo.transform, false);
            RectTransform rsmRt = resumeBtnGo.AddComponent<RectTransform>();
            rsmRt.anchorMin = new Vector2(0.5f, 0);
            rsmRt.anchorMax = new Vector2(0.5f, 0);
            rsmRt.pivot = new Vector2(0.5f, 0);
            rsmRt.anchoredPosition = new Vector2(0, 320);
            rsmRt.sizeDelta = new Vector2(500, 100);
            Image rsmImg = resumeBtnGo.AddComponent<Image>();
            rsmImg.color = new Color(0.12f, 0.85f, 0.38f);
            Button resumeBtn = resumeBtnGo.AddComponent<Button>();

            GameObject rsmtGo = new GameObject("Text");
            rsmtGo.transform.SetParent(resumeBtnGo.transform, false);
            RectTransform rsmttRt = rsmtGo.AddComponent<RectTransform>();
            rsmttRt.anchorMin = Vector2.zero;
            rsmttRt.anchorMax = Vector2.one;
            rsmttRt.sizeDelta = Vector2.zero;
            Text rsmtTxt = rsmtGo.AddComponent<Text>();
            rsmtTxt.text = "RESUME";
            rsmtTxt.font = GetDefaultFont();
            rsmtTxt.fontSize = 44;
            rsmtTxt.fontStyle = FontStyle.Bold;
            rsmtTxt.alignment = TextAnchor.MiddleCenter;
            rsmtTxt.color = Color.white;

            GameObject restartBtnGo = new GameObject("RestartButton");
            restartBtnGo.transform.SetParent(pauseCardGo.transform, false);
            RectTransform rstRt = restartBtnGo.AddComponent<RectTransform>();
            rstRt.anchorMin = new Vector2(0.5f, 0);
            rstRt.anchorMax = new Vector2(0.5f, 0);
            rstRt.pivot = new Vector2(0.5f, 0);
            rstRt.anchoredPosition = new Vector2(0, 200);
            rstRt.sizeDelta = new Vector2(500, 95);
            Image rstImg = restartBtnGo.AddComponent<Image>();
            rstImg.color = new Color(0.35f, 0.65f, 0.95f);
            Button restartBtn = restartBtnGo.AddComponent<Button>();

            GameObject rsttGo = new GameObject("Text");
            rsttGo.transform.SetParent(restartBtnGo.transform, false);
            RectTransform rstttRt = rsttGo.AddComponent<RectTransform>();
            rstttRt.anchorMin = Vector2.zero;
            rstttRt.anchorMax = Vector2.one;
            rstttRt.sizeDelta = Vector2.zero;
            Text rsttTxt = rsttGo.AddComponent<Text>();
            rsttTxt.text = "RESTART";
            rsttTxt.font = GetDefaultFont();
            rsttTxt.fontSize = 40;
            rsttTxt.fontStyle = FontStyle.Bold;
            rsttTxt.alignment = TextAnchor.MiddleCenter;
            rsttTxt.color = Color.white;

            GameObject pauseHomeGo = new GameObject("PauseHomeButton");
            pauseHomeGo.transform.SetParent(pauseCardGo.transform, false);
            RectTransform pshRt = pauseHomeGo.AddComponent<RectTransform>();
            pshRt.anchorMin = new Vector2(0.5f, 0);
            pshRt.anchorMax = new Vector2(0.5f, 0);
            pshRt.pivot = new Vector2(0.5f, 0);
            pshRt.anchoredPosition = new Vector2(0, 85);
            pshRt.sizeDelta = new Vector2(500, 95);
            Image pshImg = pauseHomeGo.AddComponent<Image>();
            pshImg.color = new Color(0.5f, 0.55f, 0.6f);
            Button pauseHomeBtn = pauseHomeGo.AddComponent<Button>();

            GameObject pshtGo = new GameObject("Text");
            pshtGo.transform.SetParent(pauseHomeGo.transform, false);
            RectTransform pshttRt = pshtGo.AddComponent<RectTransform>();
            pshttRt.anchorMin = Vector2.zero;
            pshttRt.anchorMax = Vector2.one;
            pshttRt.sizeDelta = Vector2.zero;
            Text pshtTxt = pshtGo.AddComponent<Text>();
            pshtTxt.text = "MAIN MENU";
            pshtTxt.font = GetDefaultFont();
            pshtTxt.fontSize = 38;
            pshtTxt.fontStyle = FontStyle.Bold;
            pshtTxt.alignment = TextAnchor.MiddleCenter;
            pshtTxt.color = Color.white;

            pausePanelGo.SetActive(false);

            // Wire up UIManager
            UIManager uim = systemsRoot.AddComponent<UIManager>();
            SerializedObject uimSo = new SerializedObject(uim);
            uimSo.FindProperty("scoreText").objectReferenceValue = scoreText;
            uimSo.FindProperty("multiplierText").objectReferenceValue = multText;
            uimSo.FindProperty("comboMeter").objectReferenceValue = comboMeterUi;
            uimSo.FindProperty("pauseButton").objectReferenceValue = pauseBtn;

            uimSo.FindProperty("levelCompletePanel").objectReferenceValue = completePanelGo;
            uimSo.FindProperty("completeScoreText").objectReferenceValue = winScoreTxt;
            uimSo.FindProperty("completeCoinsText").objectReferenceValue = winCoinsTxt;
            uimSo.FindProperty("nextLevelButton").objectReferenceValue = nextBtn;
            uimSo.FindProperty("completeReplayButton").objectReferenceValue = replayBtn;
            uimSo.FindProperty("completeHomeButton").objectReferenceValue = homeBtn;

            uimSo.FindProperty("levelFailedPanel").objectReferenceValue = failPanelGo;
            uimSo.FindProperty("failReasonText").objectReferenceValue = failReasonTxt;
            uimSo.FindProperty("retryButton").objectReferenceValue = retryBtn;
            uimSo.FindProperty("failHomeButton").objectReferenceValue = failHomeBtn;

            uimSo.FindProperty("pausePanel").objectReferenceValue = pausePanelGo;
            uimSo.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
            uimSo.FindProperty("restartButton").objectReferenceValue = restartBtn;
            uimSo.FindProperty("pauseHomeButton").objectReferenceValue = pauseHomeBtn;

            SerializedProperty tsArray = uimSo.FindProperty("targetSlots");
            tsArray.ClearArray();
            for (int s = 0; s < targetSlotUiList.Count; s++)
            {
                tsArray.InsertArrayElementAtIndex(s);
                tsArray.GetArrayElementAtIndex(s).objectReferenceValue = targetSlotUiList[s];
            }
            uimSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire up LevelCompletePresentation
            SerializedObject lcpSo = new SerializedObject(lcp);
            lcpSo.FindProperty("completionRig").objectReferenceValue = rigGo;
            lcpSo.FindProperty("pillow").objectReferenceValue = pillowGo;
            lcpSo.FindProperty("knifeRestPoint").objectReferenceValue = restPointGo.transform;
            lcpSo.FindProperty("firstFlipApex").objectReferenceValue = firstApexGo.transform;
            lcpSo.FindProperty("secondFlipApex").objectReferenceValue = secondApexGo.transform;
            lcpSo.FindProperty("confettiLeft").objectReferenceValue = confLeft;
            lcpSo.FindProperty("confettiRight").objectReferenceValue = confRight;
            lcpSo.FindProperty("celebrationVFX").objectReferenceValue = celebVfx;
            lcpSo.FindProperty("levelCompletePanel").objectReferenceValue = completePanelGo;

            lcpSo.FindProperty("pillowScale").vector3Value = new Vector3(1.35f, 1.15f, 1.35f);
            lcpSo.FindProperty("pillowWidthScale").floatValue = 1.0f;
            lcpSo.FindProperty("pillowHeightScale").floatValue = 1.0f;
            lcpSo.FindProperty("pillowDepthScale").floatValue = 1.0f;
            lcpSo.FindProperty("gameplayKnifeScale").vector3Value = Vector3.one;
            lcpSo.FindProperty("completionKnifeScale").vector3Value = new Vector3(0.32f, 0.32f, 0.32f);
            lcpSo.FindProperty("knifeCompletionScaleMultiplier").floatValue = 1.0f;

            lcpSo.FindProperty("pillowAppearDuration").floatValue = 0.30f;
            lcpSo.FindProperty("initialRestDuration").floatValue = 0.35f;
            lcpSo.FindProperty("firstFlipDuration").floatValue = 0.60f;
            lcpSo.FindProperty("firstFlipHeight").floatValue = 0.54f;
            lcpSo.FindProperty("firstFlipHorizontalOffset").floatValue = 0.14f;
            lcpSo.FindProperty("firstFlipSpinDegrees").floatValue = 360f;
            lcpSo.FindProperty("firstRestDuration").floatValue = 0.25f;
            lcpSo.FindProperty("firstLandingDuration").floatValue = 0.10f;
            lcpSo.FindProperty("secondFlipDuration").floatValue = 0.45f;
            lcpSo.FindProperty("secondFlipHeight").floatValue = 0.38f;
            lcpSo.FindProperty("secondFlipHorizontalOffset").floatValue = 0.09f;
            lcpSo.FindProperty("secondFlipSpinDegrees").floatValue = 360f;
            lcpSo.FindProperty("finalSettleDuration").floatValue = 0.25f;
            lcpSo.FindProperty("confettiStartDelay").floatValue = 0.00f;
            lcpSo.FindProperty("confettiDuration").floatValue = 0.80f;
            lcpSo.FindProperty("levelCompletePanelDelay").floatValue = 0.15f;
            lcpSo.FindProperty("levelCompletePanelFadeDuration").floatValue = 0.35f;
            lcpSo.FindProperty("levelCompletePanelScaleDuration").floatValue = 0.35f;
            lcpSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire up LevelManager
            SerializedObject lmSo = new SerializedObject(lm);
            lmSo.FindProperty("knifeRestPillow").objectReferenceValue = pillowGo;
            lmSo.FindProperty("pillowRestPoint").objectReferenceValue = restPointGo.transform;
            SerializedProperty lvlsProp = lmSo.FindProperty("levels");
            if (lvlsProp != null)
            {
                lvlsProp.ClearArray();
                for (int l = 0; l < levelDataList.Count; l++)
                {
                    lvlsProp.InsertArrayElementAtIndex(l);
                    lvlsProp.GetArrayElementAtIndex(l).objectReferenceValue = levelDataList[l];
                }
            }
            lmSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire up GameManager
            SerializedObject gmSo = new SerializedObject(gm);
            gmSo.FindProperty("knifeSpawnPoint").objectReferenceValue = knifeSpawn.transform;
            SerializedProperty kdListProp = gmSo.FindProperty("knifeList");
            if (kdListProp != null)
            {
                kdListProp.ClearArray();
                for (int k = 0; k < knifeDataList.Count; k++)
                {
                    kdListProp.InsertArrayElementAtIndex(k);
                    kdListProp.GetArrayElementAtIndex(k).objectReferenceValue = knifeDataList[k];
                }
            }
            gmSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire up VFXManager
            SerializedObject vfxSo = new SerializedObject(vfxMgr);
            vfxSo.FindProperty("sliceParticlePrefab").objectReferenceValue = vfxPrefabs.sliceVfx;
            vfxSo.FindProperty("boardHitParticlePrefab").objectReferenceValue = vfxPrefabs.boardHitVfx;
            vfxSo.FindProperty("levelWinParticlePrefab").objectReferenceValue = vfxPrefabs.winVfx;
            vfxSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire up FloatingTextManager
            SerializedObject ftmSo = new SerializedObject(ftm);
            ftmSo.FindProperty("floatingTextPrefab").objectReferenceValue = floatingTextPrefab;
            ftmSo.FindProperty("floatingCanvas").objectReferenceValue = canvas;
            ftmSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"<color=green>[FoodSlicer] Gameplay Scene Rebuilt and Saved at {scenePath}</color>");
        }

        private static ParticleSystem CreateCelebrationConfetti(string name, Transform parent, Vector3 localPos, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.80f;
            main.startLifetime = 0.85f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.2f, 5.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.gravityModifier = 0.30f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.88f, 0.2f), 0.0f),
                    new GradientColorKey(new Color(0.2f, 0.95f, 1f), 0.35f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.1f), 0.7f),
                    new GradientColorKey(new Color(0.3f, 0.9f, 0.4f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14f;
            shape.radius = 0.08f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
            if (psr != null && mat != null)
            {
                psr.sharedMaterial = mat;
            }

            return ps;
        }

        private static ParticleSystem CreateCelebrationStreaks(string name, Transform parent, Vector3 localPos, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.80f;
            main.startLifetime = 0.80f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(4.2f, 6.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.gravityModifier = 0.10f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.55f, 0.05f, 0.2f);
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.2f, 0.9f, 1f), 0.0f),
                    new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0.5f),
                    new GradientColorKey(new Color(0.2f, 0.85f, 0.4f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.85f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
            if (psr != null && mat != null)
            {
                psr.sharedMaterial = mat;
                psr.renderMode = ParticleSystemRenderMode.Stretch;
                psr.velocityScale = 0.08f;
                psr.lengthScale = 1.4f;
            }

            return ps;
        }
        #endregion

        #region Scene Construction: Main Menu Scene
        private static void BuildMainMenuScene(
            List<KnifeData> knifeDataList,
            Dictionary<string, Material> materials,
            Dictionary<string, Mesh> meshes,
            (Sprite uiCircle, Sprite uiPillBg, Sprite uiPillFill, Sprite uiComboFrame, Sprite uiButton, Sprite uiCheckmark, Dictionary<FoodType, Sprite> foodIcons) sprites)
        {
            string scenePath = $"{ROOT_PATH}/Scenes/MainMenu.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject camRig = new GameObject("CameraRig");
            camRig.transform.position = new Vector3(0, 2.35f, -2.55f);
            camRig.transform.rotation = Quaternion.Euler(19.5f, 0f, 0f);

            GameObject camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(camRig.transform, false);
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 52f;
            camGo.AddComponent<AudioListener>();

            // Directional Light
            GameObject lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.98f, 0.96f);
            light.intensity = 1.35f;

            // Background Plane
            GameObject bgPlane = new GameObject("GradientBackground");
            bgPlane.transform.position = new Vector3(0, 12f, 32f);
            bgPlane.transform.rotation = Quaternion.Euler(19.5f, 0f, 0f);
            MeshFilter bgMf = bgPlane.AddComponent<MeshFilter>();
            bgMf.sharedMesh = meshes["Mesh_BackgroundPlane"];
            MeshRenderer bgMr = bgPlane.AddComponent<MeshRenderer>();
            bgMr.sharedMaterial = materials["Mat_GradientBG"];

            // Cutting Board
            GameObject board = new GameObject("CuttingBoard");
            board.transform.position = new Vector3(0, -0.075f, 16f);
            MeshFilter bMf = board.AddComponent<MeshFilter>();
            bMf.sharedMesh = meshes["Mesh_CuttingBoard"];
            MeshRenderer bMr = board.AddComponent<MeshRenderer>();
            bMr.sharedMaterial = materials["Mat_CuttingBoard"];

            // UI Canvas
            GameObject canvasGo = new GameObject("UI_Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Title Text
            GameObject titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(canvasGo.transform, false);
            RectTransform ttRt = titleGo.AddComponent<RectTransform>();
            ttRt.anchorMin = new Vector2(0.5f, 0.8f);
            ttRt.anchorMax = new Vector2(0.5f, 0.8f);
            ttRt.sizeDelta = new Vector2(800, 160);
            Text titleText = titleGo.AddComponent<Text>();
            titleText.text = "FOOD SLICER";
            titleText.font = GetDefaultFont();
            titleText.fontSize = 82;
            titleText.fontStyle = FontStyle.BoldAndItalic;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            Outline to = titleGo.AddComponent<Outline>();
            to.effectColor = new Color(0.9f, 0.1f, 0.5f, 0.8f);
            to.effectDistance = new Vector2(3, -3);

            // Play Button
            GameObject playBtnGo = new GameObject("PlayButton");
            playBtnGo.transform.SetParent(canvasGo.transform, false);
            RectTransform pbRt = playBtnGo.AddComponent<RectTransform>();
            pbRt.anchorMin = new Vector2(0.5f, 0.22f);
            pbRt.anchorMax = new Vector2(0.5f, 0.22f);
            pbRt.sizeDelta = new Vector2(420, 120);
            Image pbImg = playBtnGo.AddComponent<Image>();
            pbImg.color = new Color(0.1f, 0.88f, 0.35f);
            Button playBtn = playBtnGo.AddComponent<Button>();

            GameObject pbtGo = new GameObject("Text");
            pbtGo.transform.SetParent(playBtnGo.transform, false);
            RectTransform pbtRt = pbtGo.AddComponent<RectTransform>();
            pbtRt.anchorMin = Vector2.zero;
            pbtRt.anchorMax = Vector2.one;
            pbtRt.sizeDelta = Vector2.zero;
            Text pbText = pbtGo.AddComponent<Text>();
            pbText.text = "PLAY";
            pbText.font = GetDefaultFont();
            pbText.fontSize = 52;
            pbText.fontStyle = FontStyle.BoldAndItalic;
            pbText.alignment = TextAnchor.MiddleCenter;
            pbText.color = Color.white;

            // Wire up MainMenuManager & Systems
            GameObject systemsRoot = new GameObject("--- SYSTEMS ---");
            systemsRoot.AddComponent<SaveManager>();
            systemsRoot.AddComponent<AudioManager>();
            KnifeShopManager ksm = systemsRoot.AddComponent<KnifeShopManager>();
            SerializedObject ksmSo = new SerializedObject(ksm);
            SerializedProperty kdProp = ksmSo.FindProperty("knifeDatabase");
            kdProp.ClearArray();
            for (int i = 0; i < knifeDataList.Count; i++)
            {
                kdProp.InsertArrayElementAtIndex(i);
                kdProp.GetArrayElementAtIndex(i).objectReferenceValue = knifeDataList[i];
            }
            ksmSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject menuMgrGo = new GameObject("MainMenuManager");
            MainMenuManager mmm = menuMgrGo.AddComponent<MainMenuManager>();
            SerializedObject mmmSo = new SerializedObject(mmm);
            mmmSo.FindProperty("playButton").objectReferenceValue = playBtn;
            mmmSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"<color=green>[FoodSlicer] Main Menu Scene Rebuilt and Saved at {scenePath}</color>");
        }
        #endregion

        private static void UpdateBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene($"{ROOT_PATH}/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ROOT_PATH}/Scenes/Gameplay.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
        }
    }
}
