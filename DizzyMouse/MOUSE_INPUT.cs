﻿using System;

namespace DizzyMouse
{
    internal struct MOUSE_INPUT
    {
        public int TYPE;

        public int dx;

        public int dy;

        public int mouseData;

        public int dwFlags;

        public int time;

        public IntPtr dwExtraInfo;
    }
}