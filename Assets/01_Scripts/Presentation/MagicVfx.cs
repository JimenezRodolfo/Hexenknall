using UnityEngine;
namespace Labyrinth
{
    // Dibuja los efectos visuales de las habilidades.
    public enum MagicKind
    {
        Normal, Ray, Sky, Warning, Portal
    }
    public static class MagicVfx
    {
        static Material material;
        public static Color ColorFor(MagicKind kind)=>kind == MagicKind.Warning?new Color(1, .025f, .075f):kind == MagicKind.Ray?new Color(.15f, .85f, 1):kind == MagicKind.Sky?new Color(.95f, .45f, 1):new Color(.65f, .15f, 1);
        static Material Glow
        {
            get
            {
                if (material)return material;
                var c = PresentationCatalog.Current;
                Shader shader = c && c.magicShader?c.magicShader:Shader.Find("HEXENKNALL/MagicGlow");
                material = new Material(shader);
                return material;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            if (material)Object.Destroy(material);
            material = null;
        }

        // Dibuja un círculo mágico.
        public static GameObject Circle(Vector3 position, MagicKind kind, float radius = .42f, float life = -1)
        {
            var root = new GameObject("Circulo magico "+kind);
            root.transform.position = position;
            var c = PresentationCatalog.Current;
            GameObject prefab = c?(kind == MagicKind.Warning?c.redWarningCircle:kind == MagicKind.Ray?c.rayCircle:kind == MagicKind.Sky?c.skyCircle:kind == MagicKind.Portal?c.portal:c.normalCircle):null;
            if (prefab)Object.Instantiate(prefab, root.transform, false);
            else
            {
                Color color = ColorFor(kind);
                Ring(root.transform, radius, color, .025f);
                Ring(root.transform, radius*.76f, color, .012f);
                var star = new Vector3[6];
                for (int i = 0;i<6;i++)
                {
                    float a = i*Mathf.PI*4/5;
                    star[i] = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))*radius*.67f;
                }
                Line(root.transform, star, color, .013f, false);
                for (int i = 0;i<8;i++)
                {
                    float a = i*Mathf.PI/4;
                    Vector3 p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))*radius*.88f;
                    Vector3 tangent = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))*.025f;
                    Line(root.transform, new[]
                    {
                        p-tangent, p+Vector3.up*.001f+p.normalized*.035f, p+tangent
                    }
                    , color, .012f, false);
                }
            }
            var animate = root.AddComponent<MagicGlyph>();
            animate.kind = kind;
            animate.life = life;
            return root;
        }

        // Dibuja el anillo de una habilidad.
        static void Ring(Transform root, float radius, Color color, float width)
        {
            var points = new Vector3[64];
            for (int i = 0;i<64;i++)
            {
                float a = i*Mathf.PI*2/64;
                points[i] = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))*radius;
            }
            Line(root, points, color, width, true);
        }

        // Crea un rayo entre dos puntos.
        public static LineRenderer Line(Transform root, Vector3[] points, Color color, float width, bool loop)
        {
            var line = new GameObject("Luz").AddComponent<LineRenderer>();
            line.transform.SetParent(root, false);
            line.sharedMaterial = Glow;
            line.useWorldSpace = false;
            line.loop = loop;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color*1.8f;
            line.numCapVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }
        public static void SetColor(GameObject root, Color color)
        {
            if (!root)return;
            foreach (var line in root.GetComponentsInChildren<LineRenderer>())line.startColor = line.endColor = color*1.8f;
        }

        // Muestra el impacto de magia.
        public static void Burst(Vector3 p, MagicKind kind, float duration)
        {
            Circle(new Vector3(p.x, .055f, p.z), kind, .4f, duration);
            if (kind == MagicKind.Sky)Beam(new Vector3(p.x, 8, p.z), new Vector3(p.x, .04f, p.z), kind, duration);
            else if(kind == MagicKind.Normal)
            {
                Beam(p+new Vector3(-.3f, -p.y, 0), p+new Vector3(-.3f, 2.3f, 0), kind, duration);
                Beam(p+new Vector3(.3f, -p.y, 0), p+new Vector3(.3f, 2.3f, 0), kind, duration);
            }
        }

        // Genera el haz de luz.
        public static GameObject Beam(Vector3 from, Vector3 to, MagicKind kind, float duration)
        {
            var root = new GameObject("Rayo de luz "+kind);
            root.transform.position = from;
            var c = PresentationCatalog.Current;
            var prefab = c?(kind == MagicKind.Sky?c.skyBeam:c.rayBeam):null;
            if (prefab)
            {
                var fx = Object.Instantiate(prefab, root.transform, false);
                fx.transform.rotation = Quaternion.LookRotation((to-from).normalized);
                fx.transform.localScale = new Vector3(1, 1, Vector3.Distance(from, to));
            }
            else
            {
                Vector3 end = to-from;
                Color color = ColorFor(kind);
                Line(root.transform, new[]
                {
                    Vector3.zero, end
                }
                , color, .18f, false);
                Line(root.transform, new[]
                {
                    Vector3.zero, end
                }
                , Color.white, .045f, false);
                Line(root.transform, new[]
                {
                    Vector3.zero, end
                }
                , new Color(color.r, color.g, color.b, .16f), .42f, false);
            }
            var glyph = root.AddComponent<MagicGlyph>();
            glyph.life = duration;
            glyph.spin = false;
            return root;
        }
    }
}
