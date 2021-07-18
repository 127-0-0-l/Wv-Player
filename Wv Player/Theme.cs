using System;

namespace Wv_Player
{
    [Serializable]
    class Theme
    {
        //RGB
        public byte[] background = new byte[3];
        public byte[] elements = new byte[3];
        public byte[] font = new byte[3];
        public byte[] listElement = new byte[3];

        //фон для списка
        public string file;

        public Theme(byte[] b, byte[] e, byte[] fn, byte[] le, string fl)
        {
            background = b;
            elements = e;
            font = fn;
            listElement = le;
            file = fl;
        }
    }
}
