using UnityEngine;
namespace Labyrinth
{
    // Anima los símbolos de magia.
    public sealed class MagicGlyph : MonoBehaviour
    {
        public MagicKind kind;
        public float life = -1;
        public bool spin = true;
        float age;

        // Mantiene el símbolo animado mientras esté activo.
        void Update()
        {
            age+=Time.deltaTime;
            if (spin)transform.Rotate(Vector3.up, (kind == MagicKind.Warning?-25:45)*Time.deltaTime, Space.Self);
            if (life>0 && age>=life)Destroy(gameObject);
        }
    }
}
