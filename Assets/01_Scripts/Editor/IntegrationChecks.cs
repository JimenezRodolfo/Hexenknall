using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Labyrinth.Editor
{
    // Valida el proyecto antes de generar la compilación.
    public static class IntegrationChecks
    {
        [MenuItem("HEXENKNALL/Validate integrated art and boss")]

        // Revisa las referencias entre recursos.
        public static void Validate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/10_Data/Resources/LabyrinthArt.asset");
            if (!catalog)throw new Exception("Falta LabyrinthArt: ejecuta Install models and animations.");
            foreach (var obj in new[]
            {
                catalog.player, catalog.melee, catalog.bomber, catalog.boss
            })
            {
                if (!obj)throw new Exception("Falta un prefab de personaje en el catalogo.");
                AnimationBindingChecks.ValidateCharacter(obj);
                foreach (var r in obj.GetComponentsInChildren<Renderer>())foreach(var material in r.sharedMaterials) if(!material || !material.shader || !material.shader.isSupported)throw new Exception("Material no valido en "+obj.name);
            }
            var skin = catalog.boss.GetComponent<SkinnedMeshRenderer>();
            if (!skin || !skin.sharedMesh || skin.bones.Length<10 || skin.sharedMesh.boneWeights.Length != skin.sharedMesh.vertexCount)throw new Exception("Rig del jefe incompleto.");
            foreach (var obj in new[]
            {
                catalog.wall, catalog.crate
            })
            {
                if (!obj)throw new Exception("Falta modelo de bloque.");
                int triangles = obj.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);
                if (triangles>10000)throw new Exception("Bloque sin optimizar: "+obj.name);
            }
            if (BossCombat.SecondPhase(4) || BossCombat.SecondPhase(3) || !BossCombat.SecondPhase(2) || !BossCombat.SecondPhase(1) || BossCombat.SecondPhase(0))throw new Exception("Umbral de fase incorrecto.");
            if (Campaign.AllScenes().Length != 14)throw new Exception("Campana incompleta.");
            Debug.Log("Arte, animaciones y umbral del jefe: comprobaciones correctas. Ejecuta Play Mode para verificar movimiento, importacion visual y colisiones.");
        }
    }
    public sealed class IntegratedBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder=>0;

        // Comprueba la configuración antes de compilar.
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/10_Data/Resources/LabyrinthArt.asset"))ArtInstaller.Build();
            try
            {
                IntegrationChecks.Validate();
            }
            catch (Exception e)
            {
                throw new BuildFailedException("Preparacion del arte incompleta: "+e.Message);
            }
        }
    }
}
