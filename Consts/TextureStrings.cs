using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Heal_On_Wall.Consts
{
    public class TextureStrings
    {
        #region Misc
        public const string HOWKey = "WallHeal";
        private const string HOWFile = "Heal_On_Wall.Resources.HOW.png";
        #endregion Misc

        private readonly Dictionary<string, Sprite> _dict;

        public TextureStrings()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            _dict = new Dictionary<string, Sprite>();
            Dictionary<string, string> tmpTextures = new Dictionary<string, string>();
            tmpTextures.Add(HOWKey, HOWFile);
            foreach (var t in tmpTextures)
            {
                using (Stream s = asm.GetManifestResourceStream(t.Value))
                {
                    if (s == null) continue;

                    byte[] buffer = new byte[s.Length];
                    s.Read(buffer, 0, buffer.Length);
                    s.Dispose();

                    //Create texture from bytes
                    var tex = new Texture2D(2, 2);

                    tex.LoadImage(buffer, true);

                    // Create sprite from texture
                    // Split is to cut off the TestOfTeamwork.Resources. and the .png
                    _dict.Add(t.Key, Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));
                }
            }
        }

        public Sprite Get(string key)
        {
            return _dict[key];
        }
    }
}