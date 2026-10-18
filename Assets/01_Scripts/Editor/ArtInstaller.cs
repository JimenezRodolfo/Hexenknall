using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Labyrinth.Editor
{
    [InitializeOnLoad]
    // Instala modelos, materiales y animaciones.
    public static class ArtInstaller
    {
        const string Generated = "Assets/02_Prefabs/Generated", Materials = "Assets/06_Materials/Generated", Animations = "Assets/05_Animations/Generated", CatalogPath = "Assets/10_Data/Resources/LabyrinthArt.asset";
        static bool building;
        static ArtInstaller()
        {
            EditorApplication.delayCall+=Ensure;
        }

        // Revisa si hace falta instalar el arte.
        static void Ensure()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall+=Ensure;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode || building)return;
            if (!AssetDatabase.LoadAssetAtPath<ArtCatalog>(CatalogPath))Build();
        }
        [MenuItem("HEXENKNALL/Install models and animations")]

        // Crea los recursos visuales del proyecto.
        public static void Build()
        {
            if (building || EditorApplication.isPlayingOrWillChangePlaymode)return;
            building = true;
            try
            {
                Directory.CreateDirectory(Generated);
                Directory.CreateDirectory(Materials);
                Directory.CreateDirectory(Animations);
                Directory.CreateDirectory("Assets/04_Models/Generated");
                Directory.CreateDirectory("Assets/10_Data/Resources");
                AssetDatabase.Refresh();
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader)throw new InvalidOperationException("URP no esta importado. Espera a Package Manager y vuelve a instalar el arte.");
                var catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(CatalogPath);
                if (!catalog)catalog = ScriptableObject.CreateInstance<ArtCatalog>();
                catalog.floor = Plain("FloorStone", new Color(.18f, .23f, .28f), 0, .22f);
                catalog.stone = Plain("RuinsStone", new Color(.20f, .25f, .29f), 0, .15f);
                catalog.trim = Plain("CarvedStone", new Color(.32f, .38f, .43f), 0, .25f);
                catalog.crystal = Plain("VioletCrystal", new Color(.43f, .08f, .75f), .12f, .55f, new Color(1.1f, .08f, 2.1f));
                catalog.foliage = Plain("NightFoliage", new Color(.08f, .15f, .17f), 0, .1f);
                catalog.metal = Plain("SwordSteel", new Color(.55f, .62f, .72f), .85f, .65f);
                var roles = new[]
                {
                    "Player", "Melee", "Bomber", "Boss", "Wall", "Crate", "OldFriend"
                };
                var prefabs = new Dictionary<string, GameObject>();
                for (int i = 0;i<roles.Length;i++)
                {
                    string role = roles[i];
                    EditorUtility.DisplayProgressBar("HEXENKNALL", "Preparando "+role, (float)i/roles.Length);
                    prefabs[role] = BuildVisual(role, Textured(role));
                }
                catalog.player = prefabs["Player"];
                catalog.melee = prefabs["Melee"];
                catalog.bomber = prefabs["Bomber"];
                catalog.oldFriend = prefabs["OldFriend"];
                catalog.boss = prefabs["Boss"];
                catalog.wall = prefabs["Wall"];
                catalog.crate = prefabs["Crate"];
                if (!AssetDatabase.Contains(catalog))AssetDatabase.CreateAsset(catalog, CatalogPath);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                MediaInstaller.Configure();
                ArtSceneIntegration.Apply();
                Debug.Log("Arte instalado: modelos, materiales PBR y clips de habilidades listos. Abre MainMenu y pulsa Play.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                building = false;
                EditorUtility.ClearProgressBar();
            }
        }

        // Genera un material sencillo.
        static Material Plain(string name, Color color, float metallic, float smooth, Color emission = default)
        {
            string path = Materials+"/"+name+".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smooth);
            m.enableInstancing = true;
            if (emission.maxColorComponent>0)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        // Importa la textura necesaria.
        static Texture2D Texture(string role, string kind, bool normal = false)
        {
            string path = Directory.GetFiles("Assets/04_Models/"+role, kind+".*").FirstOrDefault(p=>!p.EndsWith(".meta"));
            if (path == null)return null;
            path = path.Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer)
            {
                bool color = kind == "BaseColor";
                var type = normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                if (importer.maxTextureSize != 1024 || importer.sRGBTexture != color || importer.textureType != type || !importer.isReadable)
                {
                    importer.maxTextureSize = 1024;
                    importer.sRGBTexture = color;
                    importer.textureType = type;
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Prepara los materiales del modelo.
        static Material Textured(string role)
        {
            var m = Plain(role+"PBR", Color.white, .05f, .28f);
            m.SetTexture("_BaseMap", Texture(role, "BaseColor"));
            var normal = Texture(role, "Normal", true);
            if (normal)
            {
                m.SetTexture("_BumpMap", normal);
                m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_BumpScale", .75f);
            }
            var metal = Texture(role, "Metallic");
            var rough = Texture(role, "Roughness");
            if (metal || rough)
            {
                const int size = 512;
                var packed = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
                var pixels = new Color[size*size];
                for (int y = 0;y<size;y++)for(int x = 0;x<size;x++)
                {
                    float u = (x+.5f)/size, v = (y+.5f)/size;
                    pixels[y*size+x] = new Color(metal?metal.GetPixelBilinear(u, v).r:0, 0, 0, rough?1-rough.GetPixelBilinear(u, v).r:.3f);
                }
                packed.SetPixels(pixels);
                packed.Apply();
                string path = Materials+"/"+role+"MetalSmooth.png";
                File.WriteAllBytes(path, packed.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(packed);
                AssetDatabase.ImportAsset(path);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.sRGBTexture = false;
                ti.SaveAndReimport();
                m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_Smoothness", 1);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        // Construye el prefab visual del personaje.
        static GameObject BuildVisual(string role, Material material)
        {
            bool actor = role != "Wall" && role != "Crate" && role != "OldFriend";
            string path = "Assets/04_Models/"+role+"/"+role+(role == "Boss" || role == "Wall" || role == "Crate"?".obj":".fbx");
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var animType = actor && role != "Boss"?ModelImporterAnimationType.Legacy:ModelImporterAnimationType.None;
            if (importer.animationType != animType || importer.importAnimation != actor || importer.importCameras || importer.importLights || !importer.isReadable || importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.animationType = animType;
                importer.importAnimation = actor;
                importer.importCameras = false;
                importer.importLights = false;
                importer.isReadable = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
            }
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!source)throw new Exception("No se pudo importar "+path);
            var root = new GameObject(role+"Visual");
            try
            {
                var model = UnityEngine.Object.Instantiate(source, root.transform);
                model.name = "Model";
                foreach (var c in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                foreach (var a in model.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);
                foreach (var a in model.GetComponentsInChildren<Animation>(true))UnityEngine.Object.DestroyImmediate(a);
                foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                {
                    var mf = r.GetComponent<MeshFilter>();
                    var mesh = r is SkinnedMeshRenderer sm?sm.sharedMesh:mf?mf.sharedMesh:null;
                    if (mesh && mesh.vertexCount<100)
                    {
                        r.enabled = false;
                        continue;
                    }
                    r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    r.receiveShadows = true;
                    if (r is SkinnedMeshRenderer skin)skin.updateWhenOffscreen = true;
                }
                Normalize(model, actor?(role == "Boss"?1.55f:1.25f):role == "OldFriend"?1.05f:role == "Wall"?1.05f:.86f, !actor && role != "OldFriend");
                if (actor)
                {
                    if (role == "Boss")MakeBossRig(root, model, material);
                    var animation = root.AddComponent<Animation>();
                    animation.playAutomatically = false;
                    animation.cullingType = AnimationCullingType.AlwaysAnimate;
                    var bones = FindBones(root);
                    foreach (string required in new[]
                    {
                        "hips", "spine", "leftupleg", "rightupleg", "leftleg", "rightleg"
                    }
                    ) if(!Bone(bones, required))throw new Exception(role+": falta hueso "+required+"; no se puede generar la marcha.");
                    RelaxArms(root, bones);
                    var clips = CreateClips(root, bones, role);
                    // Conserva la animación de carrera de los enemigos.
                    var original = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview"));
                    if (original && (role == "Melee" || role == "Bomber"))
                    {
                        var run = UnityEngine.Object.Instantiate(original);
                        run.name = "Run";
                        run.legacy = true;
                        run.wrapMode = WrapMode.Loop;
                        // El prefab agrega un objeto Model sobre el FBX.
                        var bindings = AnimationUtility.GetCurveBindings(run);
                        foreach (var binding in bindings)
                        {
                            var curve = AnimationUtility.GetEditorCurve(run, binding);
                            AnimationUtility.SetEditorCurve(run, binding, null);
                            if (binding.type == typeof(Transform) && (string.IsNullOrEmpty(binding.path) || binding.propertyName.Contains("LocalScale")))continue;
                            if (binding.path.ToLowerInvariant().EndsWith("hips") && (binding.propertyName == "m_LocalPosition.x" || binding.propertyName == "m_LocalPosition.z")) curve = AnimationCurve.Constant(0, run.length, curve.Evaluate(0));
                            var adjusted = binding;
                            adjusted.path = "Model"+(string.IsNullOrEmpty(binding.path)?"":"/"+binding.path);
                            AnimationUtility.SetEditorCurve(run, adjusted, curve);
                        }
                        run = SaveClip(run, role, "ImportedRun");
                        bool boundLeg = AnimationUtility.GetCurveBindings(run).Any(b=>b.type == typeof(Transform) && b.path.ToLowerInvariant().Contains("leg") && root.transform.Find(b.path) && AnimationUtility.GetEditorCurve(run, b).keys.Select(k=>k.value).Distinct().Count()>1);
                        if (boundLeg)
                        {
                            clips.RemoveAll(c=>c.state == "Run");
                            clips.Add(new CharacterVisual.ClipBinding("Run", run));
                        }
                        else Debug.LogWarning(role+": la carrera importada no enlaza piernas; se usa la carrera generada.");
                    }
                    else if(original)
                    {
                        // Conserva el clip original del jugador sin renombrarlo.
                        var retained = UnityEngine.Object.Instantiate(original);
                        retained.name = "SourceTake";
                        retained.legacy = true;
                        SaveClip(retained, role, "SourceTake");
                    }
                    var visual = root.AddComponent<CharacterVisual>();
                    visual.animationPlayer = animation;
                    visual.upperBody = Bone(bones, "spine");
                    visual.clips = clips.ToArray();
                    // Guarda las referencias de clips y recupera sus estados.
                    AnimationUtility.SetAnimationClips(animation, clips.Select(c=>c.clip).Distinct().ToArray());
                    animation.clip = clips.First(c=>c.state == "Idle").clip;
                    visual.RefreshClips();
                    if (!animation.GetClip("Idle") || !animation.GetClip("Run"))throw new Exception(role+": registro Idle/Run incompleto.");
                }
                return PrefabUtility.SaveAsPrefabAsset(root, Generated+"/"+role+"Visual.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
        static Bounds BoundsOf(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
            if (renderers.Length == 0)throw new Exception("Modelo sin malla visible: "+obj.name);
            var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
            return bounds;
        }

        // Ajusta la escala y la posición del modelo.
        static void Normalize(GameObject model, float height, bool cube)
        {
            var b = BoundsOf(model);
            float scale = height/Mathf.Max(.001f, b.size.y);
            if (cube)scale = Mathf.Min(scale, .94f/Mathf.Max(b.size.x, b.size.z));
            model.transform.localScale*=scale;
            b = BoundsOf(model);
            model.transform.position-=new Vector3(b.center.x, b.min.y, b.center.z);
        }
        static Dictionary<string, Transform> FindBones(GameObject root)
        {
            var bones = new Dictionary<string, Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                string n = t.name.Split(':').Last().ToLowerInvariant();
                if (n.StartsWith("mixamorig"))n = n.Substring("mixamorig".Length).TrimStart('_');
                if (!bones.ContainsKey(n))bones[n] = t;
            }
            return bones;
        }
        static Transform Bone(Dictionary<string, Transform> b, string name)=>b.TryGetValue(name, out var t)?t:null;
        static void RelaxArms(GameObject root, Dictionary<string, Transform> b)
        {
            foreach (string side in new[]
            {
                "left", "right"
            })
            {
                var arm = Bone(b, side+"arm");
                var fore = Bone(b, side+"forearm");
                if (!arm || !fore)continue;
                Vector3 direction = fore.position-arm.position;
                float sign = Mathf.Sign(arm.position.x-root.transform.position.x);
                var desired = new Vector3(sign*.25f, -1, .07f).normalized;
                arm.rotation = Quaternion.FromToRotation(direction, desired)*arm.rotation;
            }
        }

        // Crea la estructura de animación del jefe.
        static void MakeBossRig(GameObject root, GameObject model, Material material)
        {
            var filters = model.GetComponentsInChildren<MeshFilter>();
            var combined = new Mesh();
            combined.CombineMeshes(filters.Select(f=>new CombineInstance
            {
                mesh = f.sharedMesh, transform = root.transform.worldToLocalMatrix*f.transform.localToWorldMatrix
            }
            ).ToArray());
            var bones = new List<Transform>();
            var ends = new List<Vector3>();
            Transform Add(string name, Transform parent, Vector3 pos, Vector3 end)
            {
                var t = new GameObject(name).transform;
                t.SetParent(parent, false);
                t.position = root.transform.TransformPoint(pos);
                bones.Add(t);
                ends.Add(end);
                return t;
            }
            var hips = Add("Hips", root.transform, new Vector3(0, .75f, 0), new Vector3(0, .95f, 0));
            var spine = Add("Spine", hips, new Vector3(0, .95f, 0), new Vector3(0, 1.2f, 0));
            var head = Add("Head", spine, new Vector3(0, 1.3f, 0), new Vector3(0, 1.55f, 0));
            foreach (int side in new[]
            {
                -1, 1
            })
            {
                string prefix = side<0?"Left":"Right";
                var arm = Add(prefix+"Arm", spine, new Vector3(side*.19f, 1.12f, 0), new Vector3(side*.25f, .95f, 0));
                var fore = Add(prefix+"ForeArm", arm, new Vector3(side*.25f, .95f, 0), new Vector3(side*.29f, .79f, 0));
                Add(prefix+"Hand", fore, new Vector3(side*.29f, .79f, 0), new Vector3(side*.40f, .79f, 0));
                var thigh = Add(prefix+"UpLeg", hips, new Vector3(side*.10f, .70f, 0), new Vector3(side*.10f, .36f, 0));
                var leg = Add(prefix+"Leg", thigh, new Vector3(side*.10f, .36f, 0), new Vector3(side*.10f, .06f, 0));
                Add(prefix+"Foot", leg, new Vector3(side*.10f, .06f, 0), new Vector3(side*.10f, .04f, .12f));
            }
            var vertices = combined.vertices;
            var weights = new BoneWeight[vertices.Length];
            for (int i = 0;i<vertices.Length;i++)
            {
                var ranked = new List<KeyValuePair<int, float>>();
                for (int j = 0;j<bones.Count;j++)
                {
                    Vector3 a = root.transform.InverseTransformPoint(bones[j].position), delta = ends[j]-a;
                    Vector3 near = a+delta*Mathf.Clamp01(Vector3.Dot(vertices[i]-a, delta)/Mathf.Max(.0001f, delta.sqrMagnitude));
                    float w = 1/Mathf.Pow(.035f+(vertices[i]-near).magnitude, 5);
                    ranked.Add(new KeyValuePair<int, float>(j, w));
                }
                var top = ranked.OrderByDescending(x=>x.Value).Take(4).ToArray();
                float sum = top.Sum(x=>x.Value);
                weights[i] = new BoneWeight
                {
                    boneIndex0 = top[0].Key, boneIndex1 = top[1].Key, boneIndex2 = top[2].Key, boneIndex3 = top[3].Key, weight0 = top[0].Value/sum, weight1 = top[1].Value/sum, weight2 = top[2].Value/sum, weight3 = top[3].Value/sum
                };
            }
            combined.boneWeights = weights;
            combined.bindposes = bones.Select(t=>t.worldToLocalMatrix*root.transform.localToWorldMatrix).ToArray();
            combined.RecalculateBounds();
            combined.RecalculateNormals();
            combined.RecalculateTangents();
            string path = "Assets/04_Models/Generated/BossSkinnedMesh.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing)
            {
                EditorUtility.CopySerialized(combined, existing);
                UnityEngine.Object.DestroyImmediate(combined);
                combined = existing;
            }
            else AssetDatabase.CreateAsset(combined, path);
            UnityEngine.Object.DestroyImmediate(model);
            var skin = root.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh = combined;
            skin.bones = bones.ToArray();
            skin.rootBone = hips;
            skin.sharedMaterial = material;
            skin.updateWhenOffscreen = true;
            skin.localBounds = new Bounds(new Vector3(0, .8f, 0), new Vector3(3, 3, 3));
        }
        static AnimationClip SaveClip(AnimationClip clip, string role, string file)
        {
            string state = clip.name;
            var custom = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/05_Animations/Custom/"+role+"_"+state+".anim");
            if (custom)
            {
                if (!custom.legacy)
                {
                    custom.legacy = true;
                    EditorUtility.SetDirty(custom);
                }
                UnityEngine.Object.DestroyImmediate(clip);
                return custom;
            }
            string path = Animations+"/"+role+"_"+file+".anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing)
            {
                EditorUtility.CopySerialized(clip, existing);
                UnityEngine.Object.DestroyImmediate(clip);
                existing.name = state;
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(clip, path);
            clip.name = state;
            EditorUtility.SetDirty(clip);
            return clip;
        }

        // Prepara las animaciones disponibles.
        static List<CharacterVisual.ClipBinding> CreateClips(GameObject root, Dictionary<string, Transform> bones, string role)
        {
            var result = new List<CharacterVisual.ClipBinding>();
            foreach (string action in CharacterVisual.StateNames)
            {
                float length = action == "Idle"?2:action == "Run"?.7f:action == "Death"?1.35f:action == "PhaseChange"?1:action == "Aim"?1:.65f;
                var clip = new AnimationClip
                {
                    name = action, legacy = true, frameRate = 30, wrapMode = action == "Idle" || action == "Run" || action == "Aim"?WrapMode.Loop:WrapMode.Once
                };
                foreach (var pair in bones)
                {
                    string name = pair.Key;
                    var bone = pair.Value;
                    if (!(name.Contains("arm") || name.Contains("leg") || name == "spine" || name == "head" || name == "hips"))continue;
                    string path = AnimationUtility.CalculateTransformPath(bone, root.transform);
                    var x = new AnimationCurve();
                    var y = new AnimationCurve();
                    var z = new AnimationCurve();
                    var w = new AnimationCurve();
                    Quaternion rest = bone.localRotation;
                    Vector3 axisX = bone.InverseTransformDirection(root.transform.right), axisY = bone.InverseTransformDirection(root.transform.up), axisZ = bone.InverseTransformDirection(root.transform.forward);
                    for (int i = 0;i<=30;i++)
                    {
                        float f = i/30f;
                        Vector3 angles = Pose(role, action, name, f);
                        Quaternion q = rest*Quaternion.AngleAxis(angles.x, axisX)*Quaternion.AngleAxis(angles.y, axisY)*Quaternion.AngleAxis(angles.z, axisZ);
                        x.AddKey(f*length, q.x);
                        y.AddKey(f*length, q.y);
                        z.AddKey(f*length, q.z);
                        w.AddKey(f*length, q.w);
                    }
                    clip.SetCurve(path, typeof(Transform), "localRotation.x", x);
                    clip.SetCurve(path, typeof(Transform), "localRotation.y", y);
                    clip.SetCurve(path, typeof(Transform), "localRotation.z", z);
                    clip.SetCurve(path, typeof(Transform), "localRotation.w", w);
                }
                clip.EnsureQuaternionContinuity();
                clip = SaveClip(clip, role, action);
                result.Add(new CharacterVisual.ClipBinding(action, clip));
            }
            return result;
        }

        // Define la pose de una animación.
        static Vector3 Pose(string role, string action, string bone, float f)
        {
            bool left = bone.Contains("left"), arm = bone.EndsWith("arm") && !bone.Contains("fore"), fore = bone.Contains("forearm"), thigh = bone.Contains("upleg"), leg = bone.EndsWith("leg") && !thigh;
            float side = left?1:-1, swing = Mathf.Sin(f*Mathf.PI*2), pulse = Mathf.Sin(f*Mathf.PI), hold = Mathf.SmoothStep(0, 1, Mathf.Min(1, f*4));
            if (role == "Melee" && (action == "Idle" || action == "Slash"))
            {
                // Anima la guardia, el golpe y la recuperación del personaje.
                float punch = f<.3f?-Mathf.SmoothStep(0, .18f, f/.3f):f<.5f?Mathf.Lerp(-.18f, 1, Mathf.SmoothStep(0, 1, (f-.3f)/.2f)):1-Mathf.SmoothStep(0, 1, (f-.5f)/.5f);
                if (action == "Idle")punch = 0;
                bool right = !left;
                if (arm)return new Vector3(right?-38-48*punch:-34, 0, left?5:-5);
                if (fore)return new Vector3(right?-62+52*punch:-60, 0, 0);
                if (bone == "spine")return new Vector3(3+(action == "Idle"?swing:0), punch*10, 0);
                return Vector3.zero;
            }
            if (action == "Run")return new Vector3(thigh?side*swing*32:leg?Mathf.Max(0, -side*swing)*38:arm?-side*swing*25:fore?-25:bone == "spine"?8:0, 0, 0);
            if (action == "Idle")return new Vector3(bone == "spine"?swing*1.8f:0, 0, arm?side*swing*1.2f:0);
            if (action == "Aim")return new Vector3(arm?-65:fore?-30:0, 0, arm?side*12:0);
            if (action == "Hit")return new Vector3(bone == "spine"?-20*pulse:arm?15*pulse:0, 0, bone == "head"?pulse*10:0);
            if (action == "Death")return new Vector3(bone == "spine"?hold*24:leg?hold*45:0, 0, arm?side*hold*25:0);
            if (action == "PlaceBomb")return new Vector3(bone == "spine"?15*pulse:arm?-40*pulse:fore?-25*pulse:0, 0, 0);
            if (action == "PhaseChange")return new Vector3(bone == "spine"?-12*pulse:fore?-35*pulse:0, 0, arm?side*65*pulse:0);
            if (action == "Slash" || action == "DashStrike")return new Vector3(arm?-70*pulse:fore?-40*pulse:bone == "spine"?12*pulse:0, bone == "spine"?35*Mathf.Sin(f*Mathf.PI*2):0, arm?side*35*pulse:0);
            if (action == "DashCharge")return new Vector3(bone == "spine"?22*hold:arm?-55*hold:fore?-70*hold:thigh?-15*hold:0, 0, 0);
            if (action == "DashRecover")return new Vector3(bone == "spine"?22*(1-f):arm?-25*(1-f):0, 0, 0);
            return new Vector3(arm?-95*pulse:fore?-25*pulse:bone == "spine"?-6*pulse:0, 0, arm?side*18*pulse:0);
        }
    }
}
