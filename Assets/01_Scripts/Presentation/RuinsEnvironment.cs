using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Labyrinth
{
    // El decorado se adapta al tamaño del tablero sin bloquear casillas.
    // Construye el decorado alrededor del tablero.
    public sealed class RuinsEnvironment : MonoBehaviour
    {
        int width, height;
        GameObject decoration;
        class Batch
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<int> indices = new List<int>();
            public void Box(Vector3 center, Vector3 size, Quaternion rotation)
            {
                Vector3[] p =
                {
                    new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1), new Vector3(-1, -1, 1), new Vector3(1, -1, 1), new Vector3(1, 1, 1), new Vector3(-1, 1, 1)
                };
                int[] quads =
                {
                    0, 3, 2, 1, 4, 5, 6, 7, 0, 4, 7, 3, 1, 2, 6, 5, 3, 7, 6, 2, 0, 1, 5, 4
                };
                for (int f = 0;f<6;f++)
                {
                    int start = vertices.Count;
                    for (int j = 0;j<4;j++)vertices.Add(center+rotation*Vector3.Scale(p[quads[f*4+j]], size*.5f));
                    indices.AddRange(new[]
                    {
                        start, start+1, start+2, start, start+2, start+3
                    });
                }
            }
            public void Cone(Vector3 center, float radius, float h, int sides, bool crystal = false)
            {
                for (int i = 0;i<sides;i++)
                {
                    float a = i*Mathf.PI*2/sides, b = (i+1)*Mathf.PI*2/sides;
                    int start = vertices.Count;
                    vertices.Add(center+new Vector3(Mathf.Cos(a)*radius, 0, Mathf.Sin(a)*radius));
                    vertices.Add(center+new Vector3(0, h, 0));
                    vertices.Add(center+new Vector3(Mathf.Cos(b)*radius, 0, Mathf.Sin(b)*radius));
                    indices.AddRange(new[]
                    {
                        start, start+1, start+2
                    });
                    if (crystal)
                    {
                        vertices.Add(center+Vector3.down*.18f);
                        indices.AddRange(new[]
                        {
                            start, start+2, start+3
                        });
                    }
                }
            }
            public void Finish(string name, Transform parent, Material material)
            {
                var obj = new GameObject(name);
                obj.transform.SetParent(parent, false);
                var mesh = new Mesh
                {
                    name = name, indexFormat = IndexFormat.UInt32
                };
                mesh.SetVertices(vertices);
                mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = obj.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
        }

        // Construye el entorno al cargar el nivel.
        void Start()
        {
            var art = ArtCatalog.Current;
            if (!art)return;
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if(r.name.StartsWith("Floor "))
            {
                r.sharedMaterial = art.floor;
                r.receiveShadows = true;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.035f, .085f, .12f);
            RenderSettings.fogDensity = .022f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.25f, .33f, .43f);
            RenderSettings.ambientEquatorColor = new Color(.16f, .19f, .29f);
            RenderSettings.ambientGroundColor = new Color(.07f, .055f, .12f);
            if (Camera.main)
            {
                Camera.main.backgroundColor = RenderSettings.fogColor;
                Camera.main.farClipPlane = 160;
            }
            var sun = new GameObject("Moonlight").AddComponent<Light>();
            sun.transform.SetParent(transform);
            sun.type = LightType.Directional;
            sun.color = new Color(.60f, .75f, 1);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48, -30, 0);
            Build();
        }

        // Revisa los cambios de tamaño del tablero.
        void Update()
        {
            if (ArtCatalog.Current && (width != GameDirector.I.board.width || height != GameDirector.I.board.height))Build();
        }

        // Crea las ruinas y los detalles decorativos.
        void Build()
        {
            var art = ArtCatalog.Current;
            var board = GameDirector.I.board;
            width = board.width;
            height = board.height;
            if (decoration)
            {
                foreach (var f in decoration.GetComponentsInChildren<MeshFilter>())Destroy(f.sharedMesh);
                Destroy(decoration);
            }
            decoration = new GameObject("Ruins and violet forest");
            decoration.transform.SetParent(transform, false);
            var stone = new Batch();
            var trim = new Batch();
            var leaves = new Batch();
            var crystals = new Batch();
            var runes = new Batch();
            var rng = new System.Random(4815+width*31+height);
            float R(float min, float max)=>(float)(min+rng.NextDouble()*(max-min));
            void Box(Batch batch, Vector3 p, Vector3 s, float yaw = 0)=>batch.Box(p, s, Quaternion.Euler(0, yaw, 0));
            Box(stone, new Vector3((width-1)*.5f, -1.85f, (height-1)*.5f), new Vector3(width+1.4f, 3.25f, height+1.4f));
            // Construye las piedras irregulares de la plataforma.
            for (int y = 0;y<3;y++)
            {
                for (int x = -1;x<=width;x+=2)
                {
                    Box(trim, new Vector3(x+(y%2)*.6f, -.7f-y*.8f, -.88f), new Vector3(1.9f, .72f, .32f));
                    Box(trim, new Vector3(x+(y%2)*.6f, -.7f-y*.8f, height-.12f), new Vector3(1.9f, .72f, .32f));
                }
                for (int z = 0;z<height;z+=2)
                {
                    Box(trim, new Vector3(-.88f, -.7f-y*.8f, z), new Vector3(.32f, .72f, 1.9f));
                    Box(trim, new Vector3(width-.12f, -.7f-y*.8f, z), new Vector3(.32f, .72f, 1.9f));
                }
            }
            // Coloca los bordes decorativos fuera de las casillas.
            for (int x = -1;x<=width;x++)foreach(float z in new[]
            {
                -1.1f, height+.1f
            }
            )Box(trim, new Vector3(x, -.02f, z), new Vector3(.95f, .28f, .8f));
            for (int z = 0;z<height;z++)foreach(float x in new[]
            {
                -1.1f, width+.1f
            }
            )Box(trim, new Vector3(x, -.02f, z), new Vector3(.8f, .28f, .95f));
            // Agrega grietas y esquinas dañadas al suelo.
            for (int y = 1;y<height-1;y++)for(int x = 1;x<width-1;x++)
            {
                if (rng.NextDouble()>.42)continue;
                Box(trim, new Vector3(x+R(-.32f, .32f), -.019f, y+R(-.25f, .25f)), new Vector3(R(.1f, .28f), .006f, .017f), R(-50, 50));
            }

            // Construye una columna decorativa.
            void Pillar(float x, float z, float h)
            {
                Box(stone, new Vector3(x, h*.5f, z), new Vector3(.65f, h, .65f));
                Box(trim, new Vector3(x, .15f, z), new Vector3(1.05f, .3f, 1.05f));
                Box(trim, new Vector3(x, h-.12f, z), new Vector3(.95f, .25f, .95f));
                for (float y = .65f;y<h-.3f;y+=.7f)Box(trim, new Vector3(x, y, z), new Vector3(.72f, .08f, .72f));
            }
            foreach (float x in new[]
            {
                -3f, width+2f
            }
            )foreach(float z in new[]
            {
                -2.6f, height+1.6f
            }
            )Pillar(x, z, R(3, 5));
            // Coloca el arco roto detrás del tablero.
            float gateX = width*.68f, gateZ = height+3.2f;
            Pillar(gateX-2, gateZ, 3);
            Pillar(gateX+2, gateZ, 3);
            for (int i = 0;i<=10;i++)
            {
                if (i == 3)continue;
                float a = i*Mathf.PI/10;
                stone.Box(new Vector3(gateX+Mathf.Cos(a)*2, 3+Mathf.Sin(a)*2, gateZ), new Vector3(.7f, .65f, .75f), Quaternion.Euler(0, 0, a*Mathf.Rad2Deg));
            }
            // Agrega escaleras fuera del terreno jugable.
            for (int i = 0;i<9;i++)Box(stone, new Vector3(width*.35f, -.15f-i*.26f, -1.4f-i*.55f), new Vector3(3.3f, .35f, .6f));
            for (int i = 0;i<60;i++)
            {
                bool horizontal = i%2 == 0;
                float x = horizontal?R(-5, width+4):(i%4 == 1?-R(2.2f, 7):width+R(1.2f, 6));
                float z = horizontal?(i%4 == 0?-R(2, 7):height+R(1, 6)):R(-4, height+3);
                float ground = R(-2.2f, -.6f);
                float h = R(3.2f, 8);
                Box(stone, new Vector3(x, ground+h*.35f, z), new Vector3(.18f, h*.7f, .18f), R(0, 180));
                for (int tier = 0;tier<4;tier++)leaves.Cone(new Vector3(x, ground+tier*h*.16f, z), h*.22f*(1-tier*.18f), h*.52f, 7);
            }
            for (int i = 0;i<65;i++)
            {
                float x, z;
                if (i%2 == 0)
                {
                    x = R(-1.6f, width+.6f);
                    z = i%4 == 0?-1.65f:height+.65f;
                }
                else
                {
                    x = i%4 == 1?-1.65f:width+.65f;
                    z = R(-1.4f, height+.4f);
                }
                float h = R(.25f, .8f);
                for (int j = 0;j<3;j++)crystals.Cone(new Vector3(x+R(-.3f, .3f), -.05f, z+R(-.3f, .3f)), R(.07f, .15f), h*R(.6f, 1.4f), 5, true);
                for (int j = 0;j<4;j++)leaves.Cone(new Vector3(x+R(-.5f, .5f), -.08f, z+R(-.5f, .5f)), .22f, R(.2f, .5f), 4);
            }
            for (int x = 1;x<width-1;x+=2)foreach(float z in new[]
            {
                -.6f, height-.4f
            })
            {
                Box(runes, new Vector3(x, .03f, z), new Vector3(.19f, .012f, .045f), 45);
                Box(runes, new Vector3(x, .03f, z), new Vector3(.19f, .012f, .045f), -45);
            }
            stone.Finish("Ruined masonry", decoration.transform, art.stone);
            trim.Finish("Stone details", decoration.transform, art.trim);
            leaves.Finish("Forest and plants", decoration.transform, art.foliage);
            crystals.Finish("Crystal clusters", decoration.transform, art.crystal);
            runes.Finish("Border runes", decoration.transform, art.crystal);
            foreach (var p in new[]
            {
                new Vector3(-1, 1, -1), new Vector3(width, 1, -1), new Vector3(-1, 1, height), new Vector3(width, 1, height)
            })
            {
                var light = new GameObject("Violet glow").AddComponent<Light>();
                light.transform.SetParent(decoration.transform, false);
                light.transform.localPosition = p;
                light.type = LightType.Point;
                light.color = new Color(.6f, .15f, 1);
                light.range = 6;
                light.intensity = 2;
                light.shadows = LightShadows.None;
            }
        }
        void OnDestroy()
        {
            if (decoration)foreach(var f in decoration.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh)Destroy(f.sharedMesh);
        }
    }
}
