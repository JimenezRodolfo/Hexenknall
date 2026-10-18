using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Labyrinth
{
    // Mueve la cámara alrededor del tablero.
    public sealed class OrbitCamera : MonoBehaviour
    {
        public float angle = 35, elevation = 54, distance = 19, smoothing = 2;
        Vector3 center;
        float shock;
        DepthOfField blur;
        ColorAdjustments brightness;
        VolumeProfile profile;
        Camera cam;

        // Prepara la cámara al entrar al nivel.
        void Start()
        {
            cam = GetComponent<Camera>();
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;
            blur = profile.Add<DepthOfField>(true);
            blur.mode.Override(DepthOfFieldMode.Gaussian);
            blur.gaussianStart.Override(0);
            blur.gaussianEnd.Override(2);
            blur.gaussianMaxRadius.Override(1.5f);
            blur.active = false;
            brightness = profile.Add<ColorAdjustments>(true);
            brightness.postExposure.Override(0);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1);
            bloom.intensity.Override(.35f);
            bloom.scatter.Override(.65f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(.16f);
            center = BoardCenter();
            transform.position = Target();
            transform.LookAt(center);
        }

        // Activa el movimiento de cámara del evento.
        public void Pulse(float amount)
        {
            shock = Mathf.Max(shock, amount);
        }
        Vector3 BoardCenter()
        {
            var b = GameDirector.I.board;
            return new Vector3((b.width-1)*.5f, 0, (b.height-1)*.5f);
        }
        Vector3 Target()
        {
            var b = GameDirector.I.board;
            // Mantiene las esquinas del mapa dentro de la pantalla durante el giro.
            var rotation = Quaternion.Euler(elevation, angle, 0);
            var inverse = Quaternion.Inverse(rotation);
            float tanV = Mathf.Tan(cam.fieldOfView*Mathf.Deg2Rad*.5f);
            float tanH = tanV*Mathf.Max(.4f, cam.aspect), fitDistance = distance;
            foreach (float x in new[]
            {
                -.5f, b.width-.5f
            }
            )foreach(float z in new[]
            {
                -.5f, b.height-.5f
            })
            {
                Vector3 local = inverse*(new Vector3(x, 1.8f, z)-BoardCenter());
                fitDistance = Mathf.Max(fitDistance, Mathf.Abs(local.x)/tanH-local.z+2, Mathf.Abs(local.y)/tanV-local.z+3);
            }
            return center+rotation*new Vector3(0, 0, -fitDistance);
        }

        // Ajusta la posición y el giro en cada fotograma.
        void LateUpdate()
        {
            if (!GameDirector.I || !cam)return;
            float lerp = 1-Mathf.Exp(-smoothing*Time.deltaTime);
            center = Vector3.Lerp(center, BoardCenter(), lerp);
            transform.position = Vector3.Lerp(transform.position, Target(), lerp);
            transform.LookAt(center);
            shock = Mathf.MoveTowards(shock, 0, Time.deltaTime*1.4f);
            blur.active = GameDirector.I.dialogue;
            brightness.postExposure.value = PlayerPrefs.GetFloat("Brightness", 0)+shock*.25f;
        }
        void OnDestroy()
        {
            if (profile)
            {
                foreach (var component in profile.components)if(component)Destroy(component);
                Destroy(profile);
            }
        }
    }
}
