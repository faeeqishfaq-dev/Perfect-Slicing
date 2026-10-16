using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FoodSlicer.EditorTools
{
    public static class FoodSlicerScreenCapture
    {
        public static void CaptureGameplayView()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_FoodSlicer/Scenes/Gameplay.unity", OpenSceneMode.Single);
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindObjectOfType<Camera>();
            if (cam == null)
            {
                Debug.LogError("No camera found!");
                return;
            }

            // Clean up any existing objects in FoodContainer
            GameObject foodContainer = GameObject.Find("FoodContainer");
            if (foodContainer != null)
            {
                for (int i = foodContainer.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(foodContainer.transform.GetChild(i).gameObject);
                }
            }
            Transform parent = foodContainer != null ? foodContainer.transform : null;

            // Load assets
            var matPeel = AssetDatabase.LoadAssetAtPath<Material>("Assets/_FoodSlicer/Art/Materials/Mat_BananaPeel.mat");
            var matFlesh = AssetDatabase.LoadAssetAtPath<Material>("Assets/_FoodSlicer/Art/Materials/Mat_BananaFlesh.mat");
            var tipMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_FoodSlicer/Art/Food/Mesh_Banana_TipSlice.asset");
            var coinMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_FoodSlicer/Art/Food/Mesh_Banana_Coin.asset");

            var bananaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_FoodSlicer/Prefabs/Food/Food_Banana.prefab");
            var lemonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_FoodSlicer/Prefabs/Food/Food_Lemon.prefab");
            var wmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_FoodSlicer/Prefabs/Food/Food_Watermelon.prefab");
            var applePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_FoodSlicer/Prefabs/Food/Food_Apple.prefab");
            var potatoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_FoodSlicer/Prefabs/Food/Food_Potato.prefab");

            // 1. Three sliced banana pieces in foreground matching Image 2
            // Slice 0 (Tip): front-most curved slice showing cut face and dark tip
            GameObject slice0 = new GameObject("Slice_0_Tip");
            slice0.transform.SetParent(parent);
            slice0.transform.position = new Vector3(-0.14f, 0.05f, -0.66f);
            slice0.transform.rotation = Quaternion.Euler(-56f, 22f, -12f);
            var mf0 = slice0.AddComponent<MeshFilter>();
            mf0.sharedMesh = tipMesh != null ? tipMesh : AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_FoodSlicer/Art/Food/Mesh_Banana_Seg0.asset");
            var mr0 = slice0.AddComponent<MeshRenderer>();
            mr0.sharedMaterials = new Material[] { matPeel, matFlesh };
            mr0.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            // Slice 1 (Middle coin): thin round slice with cut face tilted toward camera
            GameObject slice1 = new GameObject("Slice_1_Coin");
            slice1.transform.SetParent(parent);
            slice1.transform.position = new Vector3(-0.06f, 0.05f, -0.28f);
            slice1.transform.rotation = Quaternion.Euler(-60f, 18f, -8f);
            var mf1 = slice1.AddComponent<MeshFilter>();
            mf1.sharedMesh = coinMesh != null ? coinMesh : AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_FoodSlicer/Art/Food/Mesh_Banana_Seg1.asset");
            var mr1 = slice1.AddComponent<MeshRenderer>();
            mr1.sharedMaterials = new Material[] { matPeel, matFlesh };
            mr1.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            // Slice 2 (Third coin): cut face poised right in front of knife blade
            GameObject slice2 = new GameObject("Slice_2_Coin");
            slice2.transform.SetParent(parent);
            slice2.transform.position = new Vector3(0.01f, 0.05f, 0.08f);
            slice2.transform.rotation = Quaternion.Euler(-64f, 16f, -6f);
            var mf2 = slice2.AddComponent<MeshFilter>();
            mf2.sharedMesh = coinMesh != null ? coinMesh : AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_FoodSlicer/Art/Food/Mesh_Banana_Seg2.asset");
            var mr2 = slice2.AddComponent<MeshRenderer>();
            mr2.sharedMaterials = new Material[] { matPeel, matFlesh };
            mr2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            // 2. Uncut arched banana body immediately behind knife
            if (bananaPrefab != null)
            {
                GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(bananaPrefab, parent);
                b.transform.position = new Vector3(0.04f, 0.05f, 0.36f);
                b.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
                // Turn off only segments 0 and 1 since they are sliced in front, keeping segments 2, 3, 4 for a full arched body!
                var seg0 = b.transform.Find("Segment_0"); if (seg0 != null) seg0.gameObject.SetActive(false);
                var seg1 = b.transform.Find("Segment_1"); if (seg1 != null) seg1.gameObject.SetActive(false);
                foreach (var mr in b.GetComponentsInChildren<MeshRenderer>()) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            // 3. Golden Orange behind banana
            if (lemonPrefab != null)
            {
                GameObject lem = (GameObject)PrefabUtility.InstantiatePrefab(lemonPrefab, parent);
                lem.transform.position = new Vector3(0.08f, 0.05f, 1.90f);
                lem.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
                lem.transform.localScale = new Vector3(0.72f, 0.68f, 0.72f);
                foreach (var mr in lem.GetComponentsInChildren<MeshRenderer>()) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            // 4. Striped Watermelon behind orange (standing upright with vertical stripes matching Image 2)
            var wmWholeMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_FoodSlicer/Art/Food/Mesh_Watermelon_Whole.asset");
            var matWmRind = AssetDatabase.LoadAssetAtPath<Material>("Assets/_FoodSlicer/Art/Materials/Mat_WatermelonRind.mat");
            if (wmWholeMesh != null && matWmRind != null)
            {
                GameObject wm = new GameObject("Food_Watermelon_Preview");
                wm.transform.SetParent(parent);
                wm.transform.position = new Vector3(0.12f, 0.38f, 3.35f);
                wm.transform.rotation = Quaternion.Euler(6f, 15f, 0f);
                wm.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                var mf = wm.AddComponent<MeshFilter>();
                mf.sharedMesh = wmWholeMesh;
                var mr = wm.AddComponent<MeshRenderer>();
                mr.sharedMaterial = matWmRind;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            else if (wmPrefab != null)
            {
                GameObject wm = (GameObject)PrefabUtility.InstantiatePrefab(wmPrefab, parent);
                wm.transform.position = new Vector3(0.12f, 0.05f, 3.35f);
                wm.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
                wm.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                foreach (var mr in wm.GetComponentsInChildren<MeshRenderer>()) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            // 5. Red Apple behind watermelon
            if (applePrefab != null)
            {
                GameObject ap = (GameObject)PrefabUtility.InstantiatePrefab(applePrefab, parent);
                ap.transform.position = new Vector3(0.15f, 0.05f, 4.65f);
                ap.transform.localScale = new Vector3(0.68f, 0.64f, 0.68f);
                foreach (var mr in ap.GetComponentsInChildren<MeshRenderer>()) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            // 6. Dark Plum / Kiwi behind apple
            if (potatoPrefab != null)
            {
                GameObject darkFruit = (GameObject)PrefabUtility.InstantiatePrefab(potatoPrefab, parent);
                darkFruit.transform.position = new Vector3(0.18f, 0.05f, 5.75f);
                darkFruit.transform.localScale = new Vector3(0.55f, 0.50f, 0.55f);
                var matChoc = AssetDatabase.LoadAssetAtPath<Material>("Assets/_FoodSlicer/Art/Materials/Mat_ChocCake.mat");
                if (matChoc != null)
                {
                    foreach (var mr in darkFruit.GetComponentsInChildren<MeshRenderer>())
                    {
                        mr.sharedMaterial = matChoc;
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    }
                }
            }

            // Render camera to texture (1080 x 1920 portrait)
            int width = 1080;
            int height = 1920;
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);

            // Render 3D scene
            cam.Render();

            // Render Canvas overlay UI into the screenshot
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1.0f;
                cam.Render();
            }

            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            byte[] bytes = screenShot.EncodeToPNG();
            string artifactDir = "C:/Users/faeeqishfaq/.gemini/antigravity/brain/81d97ff8-c584-4892-8ed9-df0f77d21662";
            string artifactPath = $"{artifactDir}/gameplay_rebuilt_view.png";
            File.WriteAllBytes(artifactPath, bytes);

            string localDir = "D:/Perfect Slicing/Screenshots";
            if (!Directory.Exists(localDir)) Directory.CreateDirectory(localDir);
            File.WriteAllBytes($"{localDir}/Gameplay_Rebuilt.png", bytes);

            Debug.Log($"<color=green>[ScreenCapture] Successfully rendered gameplay screenshot to {artifactPath} and local folder!</color>");
        }
    }
}
