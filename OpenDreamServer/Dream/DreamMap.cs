﻿using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Resources;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Compiler.DMM;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamServer.Dream {
    class DreamMap {
        public UInt16[,] Turfs { get; private set; }
        public int Width { get => Turfs.GetLength(0); }
        public int Height { get => Turfs.GetLength(1); }

        public void LoadMap(DreamResource mapResource) {
            string dmmSource = mapResource.ReadAsString();
            DMMParser dmmParser = new DMMParser(new DMLexer(dmmSource));
            DMMParser.Map map = dmmParser.ParseMap();

            Turfs = new UInt16[map.MaxX - 1, map.MaxY - 1];
            foreach (DMMParser.MapBlock mapBlock in map.Blocks) {
                foreach (KeyValuePair<(int X, int Y), string> cell in mapBlock.Cells) {
                    DMMParser.CellDefinition cellDefinition = map.CellDefinitions[cell.Value];
                    DreamObject turf = CreateMapObject(cellDefinition.Turf);
                    if (turf == null) turf = Program.DreamObjectTree.CreateObject(DreamPath.Turf);

                    SetTurf(mapBlock.X + cell.Key.X - 1, mapBlock.Y + cell.Key.Y - 1, turf);
                    foreach (DMMParser.MapObject mapObject in cellDefinition.Objects) {
                        CreateMapObject(mapObject, turf);
                    }
                }
            }
        }

        public void SetTurf(int x, int y, DreamObject turf) {
            if (!turf.IsSubtypeOf(DreamPath.Turf)) {
                throw new Exception("Turf was not a sub-type of " + DreamPath.Turf);
            }

            turf.SetVariable("x", new DreamValue(x));
            turf.SetVariable("y", new DreamValue(y));
            turf.SetVariable("z", new DreamValue(1));
            SetTurfUnsafe(x, y, DreamMetaObjectAtom.AtomIDs[turf]);
        }

        public Point GetTurfLocation(DreamObject turf) {
            if (!turf.IsSubtypeOf(DreamPath.Turf)) {
                throw new Exception("Turf is not a sub-type of " + DreamPath.Turf);
            }

            UInt16 turfAtomID = DreamMetaObjectAtom.AtomIDs[turf];
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    if (Turfs[x, y] == turfAtomID) return new Point(x + 1, y + 1);
                }
            }

            return new Point(0, 0); //Not on the map
        }

        public DreamObject GetTurfAt(int x, int y) {
            return DreamMetaObjectAtom.AtomIDToAtom[Turfs[x - 1, y - 1]];
        }

        private void SetTurfUnsafe(int x, int y, UInt16 turfAtomID) {
            Turfs[x - 1, y - 1] = turfAtomID;

            Program.DreamStateManager.AddTurfDelta(x - 1, y - 1, turfAtomID);
        }

        private DreamObject CreateMapObject(DMMParser.MapObject mapObject, DreamObject loc = null) {
            if (!Program.DreamObjectTree.HasTreeEntry(mapObject.Type)) {
                Console.WriteLine("MAP LOAD: Skipping " + mapObject.Type);

                return null;
            }

            DreamObjectDefinition definition = Program.DreamObjectTree.GetObjectDefinitionFromPath(mapObject.Type);
            if (mapObject.VarOverrides.Count > 0) {
                definition = new DreamObjectDefinition(definition);

                foreach (KeyValuePair<string, DreamValue> varOverride in mapObject.VarOverrides) {
                    if (definition.HasVariable(varOverride.Key)) {
                        definition.Variables[varOverride.Key] = varOverride.Value;
                    }
                }
            }

            return new DreamObject(definition, new DreamProcArguments(new() { new DreamValue(loc) }));
        }
    }
}
