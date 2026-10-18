using UnityEngine;
namespace Labyrinth
{
    // Anima los fragmentos cuando se rompe un bloque.
    public sealed class BreakFragments : MonoBehaviour
    {
        readonly Transform[] pieces = new Transform[8];
        readonly Vector3[] velocity = new Vector3[8];
        float age;

        // Prepara las piezas del bloque destruido.
        public void Begin(Vector3 origin)
        {
            for (int i = 0;i<pieces.Length;i++)
            {
                var obj = GameDirector.I.Visual(PrimitiveType.Cube, origin+Random.insideUnitSphere*.25f, Vector3.one*Random.Range(.09f, .19f), new Color(.55f, .25f, .8f));
                obj.transform.SetParent(transform, true);
                pieces[i] = obj.transform;
                velocity[i] = Random.insideUnitSphere*1.4f+Vector3.up*2;
            }
        }

        // Mueve las piezas hasta que desaparezcan.
        void Update()
        {
            age+=Time.deltaTime;
            for (int i = 0;i<pieces.Length;i++)if(pieces[i])
            {
                velocity[i]+=Vector3.down*5*Time.deltaTime;
                pieces[i].position+=velocity[i]*Time.deltaTime;
                pieces[i].Rotate(80*Time.deltaTime, 120*Time.deltaTime, 40*Time.deltaTime);
                pieces[i].localScale*=1-Time.deltaTime;
            }
            if (age>1)Destroy(gameObject);
        }
    }
}
