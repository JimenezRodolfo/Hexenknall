using System;
using UnityEngine;
namespace Labyrinth
{
    // Carga los diálogos desde el archivo JSON.
    public static class DialogueLibrary
    {
        [Serializable]
        public class Line
        {
            public string speaker;
            [TextArea]
            public string text;
        }
        [Serializable]
        public class Entry
        {
            public string id;
            public Line[] lines;
        }
        [Serializable]
        public class Book
        {
            public Entry[] entries;
        }
        static Book book;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            book = null;
        }

        // Busca el diálogo por su identificador.
        public static string[] Get(string id, string[] fallback = null)
        {
            if (book == null)
            {
                var source = Resources.Load<TextAsset>("HexenknallDialogues");
                try
                {
                    book = source?JsonUtility.FromJson<Book>(source.text):new Book();
                }
                catch (Exception e)
                {
                    Debug.LogError("Revisa HexenknallDialogues.json: "+e.Message);
                    book = new Book();
                }
            }
            if (book != null && book.entries != null)foreach(var entry in book.entries) if(entry != null && entry.id == id && entry.lines != null)
            {
                var lines = new string[entry.lines.Length];
                for (int i = 0;i<lines.Length;i++)
                {
                    var item = entry.lines[i];
                    string speaker = "";
                    string text = "";
                    if (item != null)
                    {
                        if (item.speaker != null)speaker = item.speaker;
                        if (item.text != null)text = item.text;
                    }
                    lines[i] = speaker+"|"+text;
                }
                return lines;
            }
            if (fallback != null)return fallback;
            return Array.Empty<string>();
        }
    }
}
