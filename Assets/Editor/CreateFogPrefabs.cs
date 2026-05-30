using UnityEngine;
using UnityEditor;
using System.IO;

namespace MaouSamaTD.EditorTools
{
    public static class CreateFogPrefabs
    {
        [MenuItem("Maou-TD/Tools/Create Fog and Background Prefabs")]
        public static void GeneratePrefabs()
        {
            string folderPath = "Assets/_Game/Prefabs/Environment";
            string absFolderPath = Path.Combine(Application.dataPath, "_Game/Prefabs/Environment");
            
            if (!Directory.Exists(absFolderPath))
            {
                Directory.CreateDirectory(absFolderPath);
            }

            // --- 1. Generate Seamless Noise Texture ---
            int size = 512; // Increased size to 512 for higher definition
            Texture2D noiseTex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            noiseTex.name = "SeamlessFogNoise";
            
            Color[] noisePixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;

                    float GetNoise(float px, float py, float scale, float seedOffset)
                    {
                        float x1 = (px / size) * scale + seedOffset;
                        float y1 = (py / size) * scale + seedOffset;
                        return Mathf.PerlinNoise(x1, y1);
                    }

                    float SampleToroidal(float px, float py, float scale, float seedOffset)
                    {
                        float n00 = GetNoise(px, py, scale, seedOffset);
                        float n10 = GetNoise(px - size, py, scale, seedOffset);
                        float n01 = GetNoise(px, py - size, scale, seedOffset);
                        float n11 = GetNoise(px - size, py - size, scale, seedOffset);

                        float n0 = Mathf.Lerp(n00, n10, u);
                        float n1 = Mathf.Lerp(n01, n11, u);
                        return Mathf.Lerp(n0, n1, v);
                    }

                    // Sample three independent, seamless noise layers
                    float rNoise = SampleToroidal(x, y, 3f, 0f);
                    float gNoise = SampleToroidal(x, y, 5f, 120f);
                    float bNoise = SampleToroidal(x, y, 7f, 240f);

                    float finalAlpha = (rNoise * 0.6f + gNoise * 0.4f);

                    noisePixels[y * size + x] = new Color(rNoise, gNoise, bNoise, finalAlpha);
                }
            }
            noiseTex.SetPixels(noisePixels);
            noiseTex.Apply();

            byte[] noisePngBytes = noiseTex.EncodeToPNG();
            string relativeNoiseTexPath = $"{folderPath}/SeamlessFogNoise.png";
            string absoluteNoiseTexPath = Path.Combine(absFolderPath, "SeamlessFogNoise.png");
            File.WriteAllBytes(absoluteNoiseTexPath, noisePngBytes);
            Object.DestroyImmediate(noiseTex);

            AssetDatabase.ImportAsset(relativeNoiseTexPath);
            
            // Set import settings for the shader noise (Repeat wraps)
            TextureImporter noiseImporter = AssetImporter.GetAtPath(relativeNoiseTexPath) as TextureImporter;
            if (noiseImporter != null)
            {
                noiseImporter.textureType = TextureImporterType.Default;
                noiseImporter.wrapMode = TextureWrapMode.Repeat;
                noiseImporter.filterMode = FilterMode.Bilinear;
                noiseImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                noiseImporter.alphaIsTransparency = true;
                noiseImporter.SaveAndReimport();
            }


            // --- 2. Generate Particle Cloud Puff Texture (Soft radial fade to zero) ---
            int puffSize = 256;
            Texture2D puffTex = new Texture2D(puffSize, puffSize, TextureFormat.RGBA32, true);
            puffTex.name = "ParticleCloudPuff";
            
            Color[] puffPixels = new Color[puffSize * puffSize];
            for (int y = 0; y < puffSize; y++)
            {
                for (int x = 0; x < puffSize; x++)
                {
                    // Compute normalized distance from center [-1.0 to 1.0]
                    float nx = (x - (puffSize / 2f)) / (puffSize / 2f);
                    float ny = (y - (puffSize / 2f)) / (puffSize / 2f);
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);

                    // Radial fade (1.0 at center, 0.0 at outer circle)
                    float radialFade = Mathf.Clamp01(1.0f - dist);
                    
                    // High-quality smoothstep for organic puffy falloff
                    radialFade = Mathf.SmoothStep(0f, 1f, radialFade);
                    
                    // Sample fine-grain Perlin noise for cloud-like details
                    float u = (float)x / puffSize;
                    float v = (float)y / puffSize;
                    float detailNoise1 = Mathf.PerlinNoise(u * 5f, v * 5f);
                    float detailNoise2 = Mathf.PerlinNoise((u + 0.37f) * 9f, (v - 0.22f) * 9f);
                    float detail = (detailNoise1 * 0.65f + detailNoise2 * 0.35f);

                    // Blend radial mask with noise details
                    float finalAlpha = radialFade * Mathf.Lerp(0.2f, 1.0f, detail);

                    // Clean puffy cloud: pure white color, alpha determines opacity
                    puffPixels[y * puffSize + x] = new Color(1.0f, 1.0f, 1.0f, finalAlpha);
                }
            }
            puffTex.SetPixels(puffPixels);
            puffTex.Apply();

            byte[] puffPngBytes = puffTex.EncodeToPNG();
            string relativePuffTexPath = $"{folderPath}/ParticleCloudPuff.png";
            string absolutePuffTexPath = Path.Combine(absFolderPath, "ParticleCloudPuff.png");
            File.WriteAllBytes(absolutePuffTexPath, puffPngBytes);
            Object.DestroyImmediate(puffTex);

            AssetDatabase.ImportAsset(relativePuffTexPath);
            
            // Set import settings for the particle puff (Clamp wraps, transparency)
            TextureImporter puffImporter = AssetImporter.GetAtPath(relativePuffTexPath) as TextureImporter;
            if (puffImporter != null)
            {
                puffImporter.textureType = TextureImporterType.Default;
                puffImporter.wrapMode = TextureWrapMode.Clamp;
                puffImporter.filterMode = FilterMode.Bilinear;
                puffImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                puffImporter.alphaIsTransparency = true;
                puffImporter.SaveAndReimport();
            }

            // Sync References
            Texture2D savedNoiseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(relativeNoiseTexPath);
            Texture2D savedPuffTex = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePuffTexPath);


            // --- 3. Shaders and Materials ---
            Shader customShader = Shader.Find("Custom/SeamlessFog");
            bool isCustomShader = customShader != null;
            
            if (customShader == null)
            {
                Debug.LogWarning("Custom/SeamlessFog shader not found. Making a fallback.");
                customShader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (customShader == null)
            {
                customShader = Shader.Find("Unlit/Color");
            }

            // Tile/Shader Fog Material
            Material fogMaterial = new Material(customShader);
            fogMaterial.name = "TileFogMaterial";
            
            if (isCustomShader)
            {
                // Rich, soft, luxurious deep violet palette that is highly transparent
                fogMaterial.SetColor("_BaseColor", new Color(0.35f, 0.15f, 0.55f, 1.0f)); 
                fogMaterial.SetColor("_CloudColor2", new Color(0.15f, 0.05f, 0.25f, 1.0f)); 
                fogMaterial.SetFloat("_GlobalScale", 0.02f); // Large sweeping noise patterns
                fogMaterial.SetVector("_ScrollSpeed1", new Vector4(0.012f, 0.006f, 0f, 0f));
                fogMaterial.SetVector("_ScrollSpeed2", new Vector4(-0.008f, 0.01f, 0f, 0f));
                fogMaterial.SetVector("_DistortionSpeed", new Vector4(0.005f, -0.005f, 0f, 0f));
                fogMaterial.SetFloat("_DistortionStrength", 0.15f);
                fogMaterial.SetFloat("_ParallaxStrength", 0.25f);
                fogMaterial.SetFloat("_LayerHeight2", 0.12f);
                fogMaterial.SetFloat("_LayerHeight3", 0.25f);
                fogMaterial.SetFloat("_Thickness", 0.15f); // Low cutoff for wispy edges
                fogMaterial.SetFloat("_Softness", 0.45f); // High softness for silky transitions
                fogMaterial.SetFloat("_MaxAlpha", 0.5f); // Prevent blocking grid vision!
                if (savedNoiseTex != null) fogMaterial.SetTexture("_BaseMap", savedNoiseTex);
            }
            else
            {
                // Fallback transparency configuration
                fogMaterial.SetFloat("_Surface", 1);
                fogMaterial.SetFloat("_Blend", 0);
                fogMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                fogMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                fogMaterial.SetInt("_ZWrite", 0);
                fogMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                fogMaterial.SetColor("_BaseColor", new Color(0.35f, 0.15f, 0.55f, 0.4f));
                if (savedNoiseTex != null) fogMaterial.SetTexture("_BaseMap", savedNoiseTex);
            }
            AssetDatabase.CreateAsset(fogMaterial, $"{folderPath}/TileFogMaterial.mat");

            // Particle Material Setup (URP Particles Unlit)
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null)
            {
                particleShader = Shader.Find("Particles/Standard Unlit");
            }
            if (particleShader == null)
            {
                particleShader = customShader;
            }
            
            Material partMat = new Material(particleShader);
            partMat.name = "VoidParticleMaterial";
            partMat.color = new Color(1f, 1f, 1f, 1f); 
            
            partMat.SetFloat("_Surface", 1f); // Transparent
            partMat.SetFloat("_Blend", 0f); // Alpha blend
            partMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            partMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            partMat.SetFloat("_ZWrite", 0f);
            partMat.DisableKeyword("_ALPHATEST_ON");
            partMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            partMat.EnableKeyword("_BLENDMODE_ALPHA");
            partMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            
            if (savedPuffTex != null)
            {
                partMat.mainTexture = savedPuffTex;
                if (particleShader.name.Contains("Universal Render Pipeline"))
                {
                    partMat.SetTexture("_BaseMap", savedPuffTex);
                }
            }
            AssetDatabase.CreateAsset(partMat, $"{folderPath}/VoidParticleMaterial.mat");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Material loadedTileFogMat = AssetDatabase.LoadAssetAtPath<Material>($"{folderPath}/TileFogMaterial.mat");
            Material loadedPartMat = AssetDatabase.LoadAssetAtPath<Material>($"{folderPath}/VoidParticleMaterial.mat");


            // --- 4. Tile Fog Prefab (Flat Horizontal Quad for None coordinates) ---
            GameObject tileFogGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tileFogGo.name = "TileFogPrefab";
            tileFogGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            Collider col = tileFogGo.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            MeshRenderer mr = tileFogGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = loadedTileFogMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            string tileFogPrefabPath = $"{folderPath}/TileFogPrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(tileFogGo, tileFogPrefabPath);
            Object.DestroyImmediate(tileFogGo);


            // --- 5. Global Background Prefab - SHADER VERSION ---
            GameObject globalBgShaderGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            globalBgShaderGo.name = "GlobalBackgroundPrefab_Shader";
            globalBgShaderGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            globalBgShaderGo.transform.localScale = new Vector3(180f, 180f, 1f); // Huge XZ quad coverage

            Collider shaderCol = globalBgShaderGo.GetComponent<Collider>();
            if (shaderCol != null) Object.DestroyImmediate(shaderCol);

            MeshRenderer shaderMr = globalBgShaderGo.GetComponent<MeshRenderer>();
            if (shaderMr != null)
            {
                shaderMr.sharedMaterial = loadedTileFogMat;
                shaderMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                shaderMr.receiveShadows = false;
            }

            string globalBgShaderPath = $"{folderPath}/GlobalBackgroundPrefab_Shader.prefab";
            PrefabUtility.SaveAsPrefabAsset(globalBgShaderGo, globalBgShaderPath);
            Object.DestroyImmediate(globalBgShaderGo);


            // --- 6. Global Background Prefab - PARTICLE VERSION ---
            GameObject globalBgPartGo = new GameObject("GlobalBackgroundPrefab_Particle");
            ParticleSystem ps = globalBgPartGo.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 10f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f); // Drifts slowly
            main.startSize = new ParticleSystem.MinMaxCurve(30f, 50f); // Massive soft overlapping cloud sizes
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.15f, 0.55f, 0.22f)); // Soft violet cloud
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI); // Random initial rotations
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 30f; // Dense output

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(160f, 1f, 160f); // Cover playable and backdrop areas

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0.02f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.7f);
            sizeCurve.AddKey(0.2f, 1.0f);
            sizeCurve.AddKey(0.8f, 1.0f);
            sizeCurve.AddKey(1.0f, 0.7f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f); // Very slow drift rotation

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.4f, 0.7f); // Organic swirling fluid motion
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.08f;
            noise.quality = ParticleSystemNoiseQuality.High;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.32f, 0.12f, 0.50f), 0.0f), 
                    new GradientColorKey(new Color(0.42f, 0.20f, 0.65f), 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.0f, 0.0f), 
                    new GradientAlphaKey(1.0f, 0.15f), 
                    new GradientAlphaKey(1.0f, 0.85f), 
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            ParticleSystemRenderer psr = globalBgPartGo.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.HorizontalBillboard; // LIE FLAT on XZ Plane! No "paper card" visual.
            psr.sharedMaterial = loadedPartMat;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;

            string globalBgPartPath = $"{folderPath}/GlobalBackgroundPrefab_Particle.prefab";
            PrefabUtility.SaveAsPrefabAsset(globalBgPartGo, globalBgPartPath);
            Object.DestroyImmediate(globalBgPartGo);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Successfully created all assets:\n" +
                      $"1. {tileFogPrefabPath}\n" +
                      $"2. {globalBgShaderPath}\n" +
                      $"3. {globalBgPartPath}");
        }
    }
}
