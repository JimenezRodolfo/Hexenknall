using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    // Controla el tiempo de la bomba y su explosión.
    public sealed class Bomb : MonoBehaviour
    {
        public Vector2Int cell;
        public float fuse = 2;
        public int radius = 2, damage = 1;
        public GridActor owner;
        public bool special;
        bool detonated;
        float placedAt;
        GameObject ownership;
        readonly List<GameObject> forecast = new List<GameObject>();
        readonly HashSet<Vector2Int> forecastCells = new HashSet<Vector2Int>();
        float nextForecast;
        bool Friendly => owner && owner.player;
        public float Age=>Mathf.Max(0, Time.time-placedAt);
        public float Remaining=>Mathf.Max(0, fuse-Age);
        void Awake()
        {
            placedAt = Time.time;
        }

        // Inicia la cuenta atrás de la bomba.
        IEnumerator Start()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer)renderer.enabled = false;
            transform.localScale = Vector3.one;
            name = special?"Invocacion celeste":"Circulo arcano";
            var g = GameDirector.I;
            var glyph = MagicVfx.Circle(g.board.World(cell, .055f), special?MagicKind.Sky:MagicKind.Normal);
            glyph.transform.SetParent(transform, true);
            MagicVfx.SetColor(glyph, Friendly ? Color.cyan : new Color(1,.25f,.15f));
            ownership = g.Visual(Friendly ? PrimitiveType.Cube : PrimitiveType.Sphere,
                g.board.World(cell, .25f), Vector3.one*.16f, Friendly ? Color.cyan : Color.red);
            ownership.transform.SetParent(transform, true);
            if (g.sound)g.sound.Play(special?SoundCue.SkyCast:SoundCue.CircleCast);
            yield return new WaitForSeconds(Remaining);
            Detonate();
        }
        void Update()
        {
            var g = GameDirector.I;
            if (!g || !FeriaSession.Accessible || Time.time < nextForecast)return;
            nextForecast = Time.time+.12f;
            var cells = g.board.Blast(cell, radius);
            if (forecastCells.SetEquals(cells))return;
            forecastCells.Clear();
            forecastCells.UnionWith(cells);
            foreach (var obj in forecast)if (obj)Destroy(obj);
            forecast.Clear();
            foreach (var p in cells)
            {
                var obj = g.Visual(PrimitiveType.Cube, g.board.World(p, .035f),
                    new Vector3(.55f, .012f, .08f), Friendly ? new Color(.1f,.65f,.8f) : new Color(.95f,.25f,.15f));
                obj.transform.SetParent(transform, true);
                forecast.Add(obj);
            }
        }
        void OnGUI()
        {
            if (!Camera.main || !GameDirector.I || GameDirector.I.paused || GameDirector.I.dialogue || GameDirector.I.finished)return;
            var point = Camera.main.WorldToScreenPoint(transform.position+Vector3.up*.5f);
            if (point.z<=0)return;
            float scale = Mathf.Max(.7f, Screen.height/720f);
            var rect = new Rect(point.x-32*scale, Screen.height-point.y-24*scale, 64*scale, 26*scale);
            var style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(14*scale), alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Friendly ? Color.cyan : new Color(1,.5f,.3f);
            GUI.Box(rect, (Friendly ? "T " : "R ")+Remaining.ToString("0.0")+(Remaining<=.5f ? " !" : " s"), style);
        }

        // Calcula el alcance y aplica la explosión.
        public void Detonate(HashSet<GridActor> hitActors = null)
        {
            if (detonated)return;
            var g = GameDirector.I;
            if (!g || !g.board)return;
            detonated = true;
            var board = g.board;
            // Guarda los obstáculos antes de romper bloques o activar otra bomba.
            var cells = board.Blast(cell, radius);
            board.bombs.Remove(cell);
            if (hitActors == null)hitActors = new HashSet<GridActor>();
            foreach (var p in cells)
            {
                if (board.blocks.TryGetValue(p, out var block))block.Break(special);
                g.Flash(p, special?Color.cyan:new Color(1, .48f, .13f), 0.38f, damage, true, hitActors, special?MagicKind.Sky:MagicKind.Normal,
                    Friendly ? "Explosión de una bomba propia (puede haber reacción en cadena)." : "Explosión de una bomba rival (puede haber reacción en cadena).");
            }
            foreach (var p in cells)if(board.bombs.TryGetValue(p, out var other))other.Detonate(hitActors);
            if (g.sound)g.sound.Play(special?SoundCue.SkyImpact:SoundCue.CircleRelease);
            Destroy(gameObject);
        }
    }
}
