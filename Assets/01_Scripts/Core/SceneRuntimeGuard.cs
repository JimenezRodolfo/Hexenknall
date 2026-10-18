using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Labyrinth
{
    // Cada nivel funciona como escena independiente.
    // Evita que se ejecuten varias partidas a la vez.
    public static class SceneRuntimeGuard
    {
        public static IEnumerable<T> InScene<T>(Scene scene) where T:Component
        {
            foreach (var root in scene.GetRootGameObjects()) foreach(var component in root.GetComponentsInChildren<T>())yield return component;
        }
        static bool HasDirector(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)return false;
            foreach (var g in InScene<GameDirector>(scene))if(g)return true;
            return false;
        }

        // Comprueba que esta escena tenga un solo director.
        public static bool Claim(GameDirector director)
        {
            var own = director.gameObject.scene;
            var active = SceneManager.GetActiveScene();
            var chosen = HasDirector(active)?active:own;
            if (own != chosen)
            {
                DisableScene(own);
                return false;
            }
            if (GameDirector.I && GameDirector.I != director && GameDirector.I.gameObject.scene == own)
            {
                if (GameDirector.I.gameObject == director.gameObject)director.enabled = false;
                else director.gameObject.SetActive(false);
                Debug.LogWarning("Se desactivo un GameDirector duplicado en "+own.name+".");
                return false;
            }
            int ignored = 0;
            for (int i = 0;i<SceneManager.sceneCount;i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != own && scene.isLoaded && Campaign.TryStage(scene.name, out _))
                {
                    DisableScene(scene);
                    ignored++;
                }
            }
            if (ignored>0)Debug.LogWarning("Habia varias escenas de campana abiertas. Solo se ejecutara "+own.name+"; las demas no modificaran la partida.");
            return true;
        }

        // Desactiva objetos de una escena duplicada.
        static void DisableScene(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)foreach(var root in scene.GetRootGameObjects())root.SetActive(false);
        }

        // Evita sonidos duplicados por varias cámaras.
        public static void KeepOneAudioListener(Scene scene)
        {
            AudioListener selected = null;
            foreach (var listener in InScene<AudioListener>(scene)) if(listener.enabled && (!selected || listener.GetComponent<Camera>()))selected = listener;
            if (!selected)return;
            foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) if(listener != selected && listener.enabled)listener.enabled = false;
        }
    }
}
