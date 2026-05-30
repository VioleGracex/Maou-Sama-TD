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

                    float SampleToroidalWavy(float px, float py, float scaleX, float scaleY, float seedOffset, float waveFreq, float waveAmp)
                    {
                        // Add a seamless wave distortion based on Y coordinate (py / size)
                        float wave = Mathf.Sin((py / size) * 2f * Mathf.PI * waveFreq) * waveAmp;
                        
                        // Distort the X coordinate using the wave
                        float distortedPx = px + wave * size;

                        float GetNoise(float xVal, float yVal)
                        {
                            float x1 = (xVal / size) * scaleX + seedOffset;
                            float y1 = (yVal / size) * scaleY + seedOffset;
                            return Mathf.PerlinNoise(x1, y1);
                        }

                        float n00 = GetNoise(distortedPx, py);
                        float n10 = GetNoise(distortedPx - size, py);
                        float n01 = GetNoise(distortedPx, py - size);
                        float n11 = GetNoise(distortedPx - size, py - size);

                        float n0 = Mathf.Lerp(n00, n10, u);
                        float n1 = Mathf.Lerp(n01, n11, u);
                        return Mathf.Lerp(n0, n1, v);
                    }

                    // Sample three independent, seamless noise layers with beautiful wavy patterns.
                    // Elongating vertically (smaller scaleY than scaleX) creates vertical/diagonal wavefronts.
                    float rNoise = SampleToroidalWavy(x, y, 4.0f, 1.5f, 0f, 2f, 0.08f);
                    float gNoise = SampleToroidalWavy(x, y, 6.0f, 2.0f, 120f, 3f, 0.06f);
                    float bNoise = SampleToroidalWavy(x, y, 8.0f, 2.5f, 240f, 4f, 0.04f);

                    float finalAlpha = Mathf.Clamp01(rNoise * 0.52f + gNoise * 0.33f + bNoise * 0.15f);

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
            string tileFogMatPath = $"{folderPath}/TileFogMaterial.mat";
            Material fogMaterial = AssetDatabase.LoadAssetAtPath<Material>(tileFogMatPath);
            if (fogMaterial == null)
            {
                fogMaterial = new Material(customShader);
                fogMaterial.name = "TileFogMaterial";
                AssetDatabase.CreateAsset(fogMaterial, tileFogMatPath);
            }
            else
            {
                fogMaterial.shader = customShader;
            }
            
            if (isCustomShader)
            {
                // Rich, soft, luxurious deep violet palette that is highly transparent
                fogMaterial.SetColor("_BaseColor", new Color(0.35f, 0.15f, 0.55f, 1.0f)); 
                fogMaterial.SetColor("_CloudColor2", new Color(0.15f, 0.05f, 0.25f, 1.0f)); 
                fogMaterial.SetFloat("_GlobalScale", 0.02f); // Large sweeping noise patterns
                fogMaterial.SetVector("_ScrollSpeed1", new Vector4(-0.03f, -0.005f, 0f, 0f));
                fogMaterial.SetVector("_ScrollSpeed2", new Vector4(-0.02f, 0.005f, 0f, 0f));
                fogMaterial.SetVector("_DistortionSpeed", new Vector4(-0.015f, 0.01f, 0f, 0f));
                fogMaterial.SetFloat("_DistortionStrength", 0.18f);
                fogMaterial.SetFloat("_ParallaxStrength", 0.25f);
                fogMaterial.SetFloat("_LayerHeight2", 0.12f);
                fogMaterial.SetFloat("_LayerHeight3", 0.25f);
                fogMaterial.SetFloat("_Thickness", 0.1f); // Low cutoff for thicker clouds
                fogMaterial.SetFloat("_Softness", 0.4f); // Silky transitions
                fogMaterial.SetFloat("_MaxAlpha", 0.85f); // Richer violet fog presence!
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
                fogMaterial.SetColor("_BaseColor", new Color(0.35f, 0.15f, 0.55f, 0.85f));
                if (savedNoiseTex != null) fogMaterial.SetTexture("_BaseMap", savedNoiseTex);
            }
            EditorUtility.SetDirty(fogMaterial);

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
            
            string partMatPath = $"{folderPath}/VoidParticleMaterial.mat";
            Material partMat = AssetDatabase.LoadAssetAtPath<Material>(partMatPath);
            if (partMat == null)
            {
                partMat = new Material(particleShader);
                partMat.name = "VoidParticleMaterial";
                AssetDatabase.CreateAsset(partMat, partMatPath);
            }
            else
            {
                partMat.shader = particleShader;
            }
            
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
            EditorUtility.SetDirty(partMat);

            AssetDatabase.SaveAssets();

            Material loadedTileFogMat = AssetDatabase.LoadAssetAtPath<Material>(tileFogMatPath);
            Material loadedPartMat = AssetDatabase.LoadAssetAtPath<Material>(partMatPath);


            // --- 4. Tile Fog Prefab (Particle System for None coordinates) ---
            GameObject tileFogGo = new GameObject("TileFogPrefab");
            tileFogGo.hideFlags = HideFlags.HideInHierarchy;
            ParticleSystem tilePs = tileFogGo.AddComponent<ParticleSystem>();
            
            var tileMain = tilePs.main;
            tileMain.duration = 10f;
            tileMain.loop = true;
            tileMain.prewarm = true;
            tileMain.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f);
            tileMain.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.04f); // Slow and peaceful
            tileMain.startSize = new ParticleSystem.MinMaxCurve(2.0f, 3.5f); // Larger size for premium overlapping clouds
            tileMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.15f, 0.55f, 0.22f), new Color(0.35f, 0.15f, 0.55f, 0.38f)); // Rich yet soft alpha variance
            tileMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            tileMain.maxParticles = 16; // Increased to ensure a lush volume of fog
            tileMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var tileEmission = tilePs.emission;
            tileEmission.rateOverTime = 1.2f; // Increased emission for consistent cloud cover

            var tileShape = tilePs.shape;
            tileShape.shapeType = ParticleSystemShapeType.Box;
            tileShape.scale = new Vector3(1.2f, 0f, 1.2f); // Slightly wider spawn area for natural scattering and overlap

            var tileVelocity = tilePs.velocityOverLifetime;
            tileVelocity.enabled = true;
            tileVelocity.x = new ParticleSystem.MinMaxCurve(-0.09f, -0.04f); // Slow drifting right-to-left
            tileVelocity.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
            tileVelocity.y = new ParticleSystem.MinMaxCurve(0f, 0.01f);

            var tileSizeOverLifetime = tilePs.sizeOverLifetime;
            tileSizeOverLifetime.enabled = true;
            AnimationCurve tileSizeCurve = new AnimationCurve();
            tileSizeCurve.AddKey(0f, 0.6f);
            tileSizeCurve.AddKey(0.2f, 1.0f);
            tileSizeCurve.AddKey(0.8f, 1.0f);
            tileSizeCurve.AddKey(1.0f, 0.6f);
            tileSizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, tileSizeCurve);

            var tileRotation = tilePs.rotationOverLifetime;
            tileRotation.enabled = true;
            tileRotation.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

            var tileNoise = tilePs.noise;
            tileNoise.enabled = true;
            tileNoise.strength = new ParticleSystem.MinMaxCurve(0.15f, 0.35f); // Richer drifting waves
            tileNoise.frequency = 0.2f;
            tileNoise.scrollSpeed = 0.03f;
            tileNoise.quality = ParticleSystemNoiseQuality.High;

            var tileColorOverLifetime = tilePs.colorOverLifetime;
            tileColorOverLifetime.enabled = true;
            Gradient tileGrad = new Gradient();
            tileGrad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.35f, 0.15f, 0.55f), 0.0f), 
                    new GradientColorKey(new Color(0.40f, 0.18f, 0.60f), 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.0f, 0.0f), 
                    new GradientAlphaKey(1.0f, 0.20f), 
                    new GradientAlphaKey(1.0f, 0.80f), 
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            tileColorOverLifetime.color = new ParticleSystem.MinMaxGradient(tileGrad);

            ParticleSystemRenderer tilePsr = tileFogGo.GetComponent<ParticleSystemRenderer>();
            tilePsr.renderMode = ParticleSystemRenderMode.HorizontalBillboard; // Lie flat on XZ plane
            tilePsr.sharedMaterial = loadedPartMat != null ? loadedPartMat : partMat;
            tilePsr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tilePsr.receiveShadows = false;

            string tileFogPrefabPath = $"{folderPath}/TileFogPrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(tileFogGo, tileFogPrefabPath);
            Object.DestroyImmediate(tileFogGo);


            // --- 5. Global Background Prefab - SHADER VERSION ---
            GameObject globalBgShaderGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            globalBgShaderGo.hideFlags = HideFlags.HideInHierarchy;
            globalBgShaderGo.name = "GlobalBackgroundPrefab_Shader";
            globalBgShaderGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            globalBgShaderGo.transform.localScale = new Vector3(180f, 180f, 1f); // Huge XZ quad coverage

            Collider shaderCol = globalBgShaderGo.GetComponent<Collider>();
            if (shaderCol != null) Object.DestroyImmediate(shaderCol);

            MeshRenderer shaderMr = globalBgShaderGo.GetComponent<MeshRenderer>();
            if (shaderMr != null)
            {
                shaderMr.sharedMaterial = loadedTileFogMat != null ? loadedTileFogMat : fogMaterial;
                shaderMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                shaderMr.receiveShadows = false;
            }

            string globalBgShaderPath = $"{folderPath}/GlobalBackgroundPrefab_Shader.prefab";
            PrefabUtility.SaveAsPrefabAsset(globalBgShaderGo, globalBgShaderPath);
            Object.DestroyImmediate(globalBgShaderGo);


            // --- 6. Global Background Prefab - PARTICLE VERSION ---
            GameObject globalBgPartGo = new GameObject("GlobalBackgroundPrefab_Particle");
            globalBgPartGo.hideFlags = HideFlags.HideInHierarchy;
            ParticleSystem ps = globalBgPartGo.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 10f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f); // Drifts slowly
            main.startSize = new ParticleSystem.MinMaxCurve(30f, 50f); // Massive soft overlapping cloud sizes
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.15f, 0.55f, 0.35f)); // Richer violet cloud
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
            velocity.x = new ParticleSystem.MinMaxCurve(-0.6f, -0.2f); // Drift consistently right-to-left
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
            psr.sharedMaterial = loadedPartMat != null ? loadedPartMat : partMat;
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
