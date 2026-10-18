using System;
using UnityEngine;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System.Reflection;
using System.Text;
#endif
namespace Labyrinth
{
    // Recupera partidas guardadas con versiones anteriores.
    public static class LegacySaveMigration
    {
        static bool attempted;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            attempted = false;
        }

        // Busca un guardado antiguo para conservar el progreso.
        public static void TryCopy(string key)
        {
            if (attempted || PlayerPrefs.HasKey(key))
            {
                attempted = true;
                return;
            }
            attempted = true;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // Busca partidas antiguas solo en las rutas conocidas.
#if UNITY_EDITOR
            string prefix = @"Software\Unity\UnityEditor\";
#else
            string prefix = @"Software\";
#endif
            foreach (string product in new[]
            {
                @"Fabrizio\La venganza del laberinto", @"Fabrizio\HEXENKNALL"
            })
            {
                try
                {
                    var registryType = Type.GetType("Microsoft.Win32.Registry, Microsoft.Win32.Registry");
                    if (registryType == null)registryType = Type.GetType("Microsoft.Win32.Registry, mscorlib");
                    object user = null;
                    if (registryType != null)
                    {
                        var field = registryType.GetField("CurrentUser", BindingFlags.Public|BindingFlags.Static);
                        if (field != null)user = field.GetValue(null);
                    }
                    if (user == null)continue;
                    object keyObject = null;
                    var openMethod = user.GetType().GetMethod("OpenSubKey", new[]
                    {
                        typeof(string), typeof(bool)
                    });
                    if (openMethod != null)keyObject = openMethod.Invoke(user, new object[]
                    {
                        prefix+product, false
                    });
                    using (var registry = keyObject as IDisposable)
                    {
                        if (registry == null)continue;
                        var type = keyObject.GetType();
                        string[] names = null;
                        var namesMethod = type.GetMethod("GetValueNames", Type.EmptyTypes);
                        if (namesMethod != null)names = namesMethod.Invoke(keyObject, null) as string[];
                        if (names == null)continue;
                        foreach (string valueName in names)
                        {
                            if (valueName != key && !valueName.StartsWith(key+"_h", StringComparison.Ordinal))continue;
                            object raw = null;
                            var valueMethod = type.GetMethod("GetValue", new[]
                            {
                                typeof(string)
                            });
                            if (valueMethod != null)raw = valueMethod.Invoke(keyObject, new object[]
                            {
                                valueName
                            });
                            string json = raw as string;
                            if (raw is byte[] bytes)json = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
                            if (string.IsNullOrEmpty(json))continue;
                            var candidate = JsonUtility.FromJson<RunData>(json);
                            if (candidate == null || candidate.version != 2)continue;
                            PlayerPrefs.SetString(key, json);
                            PlayerPrefs.Save();
                            Debug.Log("HEXENKNALL: se recupero el progreso de la version anterior.");
                            return;
                        }
                    }
                }
                catch (Exception)
                {
                    /* Leave the original save untouched if the location cannot be read. */
                }
            }
#endif
        }
    }
}
