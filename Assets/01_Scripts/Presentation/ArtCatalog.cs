using UnityEngine;
namespace Labyrinth
{
    // Asigna modelos visuales a personajes y bloques.
    public sealed class ArtCatalog : ScriptableObject
    {
        public GameObject player, melee, bomber, boss, wall, crate, oldFriend;
        public Material floor, stone, trim, crystal, foliage, metal;
        static ArtCatalog current;
        public static ArtCatalog Current=>current?current:(current = Resources.Load<ArtCatalog>("LabyrinthArt"));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            current = null;
        }

        // Coloca el modelo visual del personaje.
        public void DressActor(GridActor actor)
        {
            var prototype = actor.GetComponent<Renderer>();
            if (prototype)prototype.enabled = false;
            actor.transform.localScale = Vector3.one;
            var attached = actor.GetComponentInChildren<CharacterVisual>();
            if (attached)
            {
                attached.Bind(actor);
                actor.visual = attached;
                return;
            }
            var enemy = actor as EnemyController;
            var prefab = actor.player?player:actor.boss?boss:enemy && enemy.bomber?bomber:melee;
            if (!prefab)return;
            var old = actor.GetComponent<Renderer>();
            if (old)old.enabled = false;
            actor.transform.localScale = Vector3.one;
            var obj = Instantiate(prefab, actor.transform);
            obj.name = "Character Visual";
            obj.transform.localPosition = new Vector3(0, -.65f, 0);
            obj.transform.localRotation = Quaternion.identity;
            var visual = obj.GetComponent<CharacterVisual>();
            visual.Bind(actor);
            actor.visual = visual;
        }

        // Coloca el modelo de un bloque.
        public void DressBlock(Block block)
        {
            var prototype = block.GetComponent<Renderer>();
            if (prototype)prototype.enabled = false;
            block.transform.localScale = Vector3.one;
            if (block.transform.Find("Block Visual"))return;
            var prefab = block.destructible?crate:wall;
            if (!prefab)return;
            var old = block.GetComponent<Renderer>();
            if (old)old.enabled = false;
            block.transform.localScale = Vector3.one;
            var obj = Instantiate(prefab, block.transform);
            obj.name = "Block Visual";
            obj.transform.localPosition = new Vector3(0, block.destructible?-.5f:-.6f, 0);
            obj.transform.localRotation = Quaternion.identity;
        }
    }
}
