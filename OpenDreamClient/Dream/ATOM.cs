﻿using OpenDreamShared.Dream;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamClient.Dream {
    class ATOM {
        public UInt16 ID;
        public AtomType Type;
        public DreamIcon Icon { get; } = new DreamIcon();
        public List<ATOM> Contents = new List<ATOM>();
        public ScreenLocation ScreenLocation = new ScreenLocation();

        public ATOM Loc {
            get {
                if (Type == AtomType.Turf) {
                    return null;
                } else {
                    return _loc;
                }
            }
            set {
                if (_loc != null) {
                    _loc.Contents.Remove(this);
                }

                _loc = value;
                if (_loc != null) _loc.Contents.Add(this);
            }
        }

        public int X {
            get {
                if (Type == AtomType.Turf) {
                    return _x;
                } else {
                    return (Loc != null) ? Loc.X : 0;
                }
            }
            set {
                if (Type == AtomType.Turf) {
                    _x = value;
                }
            }
        }

        public int Y {
            get {
                if (Type == AtomType.Turf) {
                    return _y;
                } else {
                    return (Loc != null) ? Loc.Y : 0;
                }
            }
            set {
                if (Type == AtomType.Turf) {
                    _y = value;
                }
            }
        }

        private ATOM _loc = null;
        private int _x, _y; //Only used for turfs

        public ATOM(UInt16 id, AtomType type, int appearanceId) {
            ID = id;
            Type = type;
            Icon.Appearance = Program.OpenDream.IconAppearances[appearanceId];

            Program.OpenDream.AddATOM(this);
        }
    }
}
