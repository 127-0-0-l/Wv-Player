using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Wv_Player
{
    [Serializable]
    class Settings
    {
        public List<string> folders = new List<string>();
        public string activeFolder = "";
        public string activeCanvas = "";
        public string activeFile = "";
        public bool repeat = false;
        public bool mix = false;
        public int theme = 0;

        public void Save(Settings obj)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream("Settings.dat", FileMode.OpenOrCreate))
                formatter.Serialize(fs, obj);
        }

        public Settings Load()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream("Settings.dat", FileMode.OpenOrCreate))
                return (Settings)formatter.Deserialize(fs);
        }
    }
}
