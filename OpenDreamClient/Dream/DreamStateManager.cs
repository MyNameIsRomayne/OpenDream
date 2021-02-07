﻿using OpenDreamShared.Dream;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;

namespace OpenDreamClient.Dream {
    class DreamStateManager {
        public void HandlePacketFullGameState(PacketFullGameState pFullGameState) {
            DreamFullState fullState = pFullGameState.FullState;

            Program.OpenDream.IconAppearances.Clear();
            foreach (IconAppearance iconAppearance in fullState.IconAppearances) {
                Program.OpenDream.IconAppearances.Add(iconAppearance);
            }

            Program.OpenDream.ATOMs.Clear();
            foreach (KeyValuePair<UInt32, DreamFullState.Atom> stateAtom in fullState.Atoms) {
                ATOM atom = new ATOM(stateAtom.Key, stateAtom.Value.Type, stateAtom.Value.IconAppearanceID);

                atom.Icon.Appearance = Program.OpenDream.IconAppearances[stateAtom.Value.IconAppearanceID];

                atom.ScreenLocation = stateAtom.Value.ScreenLocation;

                if (stateAtom.Value.LocationID != UInt32.MaxValue) {
                    if (Program.OpenDream.ATOMs.ContainsKey(stateAtom.Value.LocationID)) {
                        atom.Loc = Program.OpenDream.ATOMs[stateAtom.Value.LocationID];
                    } else {
                        Console.WriteLine("Full game state packet gave an atom an invalid location, which was ignored (ID " + stateAtom.Value.AtomID + ")(Location ID " + stateAtom.Value.LocationID + ")");
                    }
                } else {
                    atom.Loc = null;
                }
            }

            ATOM[,] turfs = new ATOM[fullState.Turfs.GetLength(0), fullState.Turfs.GetLength(1)];
            for (int x = 0; x < turfs.GetLength(0); x++) {
                for (int y = 0; y < turfs.GetLength(1); y++) {
                    UInt32 turfAtomID = fullState.Turfs[x, y];

                    if (Program.OpenDream.ATOMs.ContainsKey(turfAtomID)) {
                        ATOM turf = Program.OpenDream.ATOMs[turfAtomID];

                        turf.X = x;
                        turf.Y = y;
                        turfs[x, y] = turf;
                    } else {
                        Console.WriteLine("Full game state packet defines a turf as an atom that doesn't exist, and was ignored (ID " + turfAtomID + ")(Location " + x + ", " + y + ")");
                    }
                }
            }

            Program.OpenDream.Map = new Map(turfs);

            if (pFullGameState.EyeID != UInt32.MaxValue) {
                if (Program.OpenDream.ATOMs.ContainsKey(pFullGameState.EyeID)) {
                    Program.OpenDream.Eye = Program.OpenDream.ATOMs[pFullGameState.EyeID];
                } else {
                    Console.WriteLine("Full game state packet gives an invalid atom for the eye (ID " + pFullGameState.EyeID + ")");
                    Program.OpenDream.Eye = null;
                }
            } else {
                Program.OpenDream.Eye = null;
            }

            Program.OpenDream.ScreenObjects.Clear();
            foreach (UInt32 screenObjectAtomID in pFullGameState.ScreenObjects) {
                if (Program.OpenDream.ATOMs.ContainsKey(screenObjectAtomID)) {
                    Program.OpenDream.ScreenObjects.Add(Program.OpenDream.ATOMs[screenObjectAtomID]);
                } else {
                    Console.WriteLine("Full games state packet defines a screen object that doesn't exist, and was ignored (ID " + screenObjectAtomID + ")");
                }
            }
        }

        public void HandlePacketDeltaGameState(PacketDeltaGameState pDeltaGameState) {
            DreamDeltaState deltaState = pDeltaGameState.DeltaState;

            foreach (IconAppearance appearance in deltaState.NewIconAppearances) {
                Program.OpenDream.IconAppearances.Add(appearance);
            }

            foreach (KeyValuePair<UInt32, DreamDeltaState.AtomCreation> atomCreationPair in deltaState.AtomCreations) {
                UInt32 atomID = atomCreationPair.Key;
                DreamDeltaState.AtomCreation atomCreation = atomCreationPair.Value;

                if (!Program.OpenDream.ATOMs.ContainsKey(atomID)) {
                    ATOM atom = new ATOM(atomID, atomCreation.Type, atomCreation.IconAppearanceID);

                    atom.Icon.Appearance = Program.OpenDream.IconAppearances[atomCreation.IconAppearanceID];
                    atom.ScreenLocation = atomCreation.ScreenLocation;

                    if (atomCreation.LocationID != UInt32.MaxValue) {
                        if (Program.OpenDream.ATOMs.ContainsKey(atomCreation.LocationID)) {
                            atom.Loc = Program.OpenDream.ATOMs[atomCreation.LocationID];
                        } else {
                            Console.WriteLine("Delta state packet gave a new atom an invalid location, so it was not assigned one (ID " + atomID + ")(Location ID " + atomCreation.LocationID + ")");
                        }
                    } else {
                        atom.Loc = null;
                    }
                } else {
                    Console.WriteLine("Delta state packet created a new atom that already exists, and was ignored (ID " + atomID + ")");
                }
            }

            foreach (UInt32 atomID in deltaState.AtomDeletions) {
                if (Program.OpenDream.ATOMs.ContainsKey(atomID)) {
                    ATOM atom = Program.OpenDream.ATOMs[atomID];

                    atom.Loc = null;
                    Program.OpenDream.ATOMs.Remove(atomID);
                } else {
                    Console.WriteLine("Delta state packet gives an atom deletion for an invalid atom, and was ignored (ID " + atomID + ")");
                }
            }

            foreach (KeyValuePair<UInt32, DreamDeltaState.AtomDelta> atomDeltaPair in deltaState.AtomDeltas) {
                UInt32 atomID = atomDeltaPair.Key;
                DreamDeltaState.AtomDelta atomDelta = atomDeltaPair.Value;

                if (Program.OpenDream.ATOMs.ContainsKey(atomID)) {
                    ATOM atom = Program.OpenDream.ATOMs[atomID];

                    if (atomDelta.NewIconAppearanceID.HasValue) {
                        if (Program.OpenDream.IconAppearances.Count > atomDelta.NewIconAppearanceID.Value) {
                            atom.Icon.Appearance = Program.OpenDream.IconAppearances[atomDelta.NewIconAppearanceID.Value];
                        } else {
                            Console.WriteLine("Invalid appearance ID " + atomDelta.NewIconAppearanceID.Value);
                        }
                    }

                    if (atomDelta.ScreenLocation.HasValue) {
                        atom.ScreenLocation = atomDelta.ScreenLocation.Value;
                    }
                } else {
                    Console.WriteLine("Delta state packet contains delta values for an invalid ATOM, and was ignored (ID " + atomID + ")");
                }
            }

            foreach (DreamDeltaState.AtomLocationDelta atomLocationDelta in deltaState.AtomLocationDeltas) {
                if (Program.OpenDream.ATOMs.ContainsKey(atomLocationDelta.AtomID)) {
                    ATOM atom = Program.OpenDream.ATOMs[atomLocationDelta.AtomID];

                    if (atomLocationDelta.LocationID != UInt32.MaxValue) {
                        if (Program.OpenDream.ATOMs.ContainsKey(atomLocationDelta.LocationID)) {
                            atom.Loc = Program.OpenDream.ATOMs[atomLocationDelta.LocationID];
                        } else {
                            Console.WriteLine("Delta state packet gave an atom a new invalid location, so it was not changed (ID " + atomLocationDelta.AtomID + ")(Location ID " + atomLocationDelta.LocationID + ")");
                        }
                    } else {
                        atom.Loc = null;
                    }
                } else {
                    Console.WriteLine("Delta state packet contains a location delta for an invalid ATOM, and was ignored (ID " + atomLocationDelta.AtomID + ")");
                }
            }

            foreach (KeyValuePair<(int X, int Y), UInt32> turfDelta in deltaState.TurfDeltas) {
                int x = turfDelta.Key.X;
                int y = turfDelta.Key.Y;
                UInt32 turfAtomID = turfDelta.Value;

                if (Program.OpenDream.ATOMs.ContainsKey(turfAtomID)) {
                    ATOM turf = Program.OpenDream.ATOMs[turfAtomID];

                    turf.X = x;
                    turf.Y = y;
                    Program.OpenDream.Map.Turfs[x, y] = turf;
                } else {
                    Console.WriteLine("Delta state packet sets a turf to an invalid atom, and was ignored (ID " + turfAtomID + ")(Location " + x + ", " + y + ")");
                }
            }

            if (pDeltaGameState.ClientDelta != null) {
                ApplyClientDelta(pDeltaGameState.ClientDelta);
            }
        }

        private void ApplyClientDelta(DreamDeltaState.ClientDelta clientDelta) {
            if (clientDelta.NewEyeID.HasValue) {
                ATOM newEye = null;

                if (clientDelta.NewEyeID.Value != UInt32.MaxValue) {
                    if (Program.OpenDream.ATOMs.ContainsKey(clientDelta.NewEyeID.Value)) {
                        newEye = Program.OpenDream.ATOMs[clientDelta.NewEyeID.Value];
                    } else {
                        Console.WriteLine("Delta state packet gives a new eye with an invalid ATOM, and was ignored (ID " + clientDelta.NewEyeID.Value + ")");
                    }
                }

                Program.OpenDream.Eye = newEye;
            }

            if (clientDelta.ScreenObjectAdditions != null) {
                foreach (UInt32 screenObjectAtomID in clientDelta.ScreenObjectAdditions) {
                    if (Program.OpenDream.ATOMs.ContainsKey(screenObjectAtomID)) {
                        ATOM screenObject = Program.OpenDream.ATOMs[screenObjectAtomID];

                        if (!Program.OpenDream.ScreenObjects.Contains(screenObject)) {
                            Program.OpenDream.ScreenObjects.Add(screenObject);
                        } else {
                            Console.WriteLine("Delta state packet says to add a screen object that's already there, and was ignored (ID " + screenObjectAtomID + ")");
                        }
                    } else {
                        Console.WriteLine("Delta state packet says to add a screen object that doesn't exist, and was ignored (ID " + screenObjectAtomID + ")");
                    }
                }
            }

            if (clientDelta.ScreenObjectRemovals != null) {
                foreach (UInt32 screenObjectAtomID in clientDelta.ScreenObjectRemovals) {
                    if (Program.OpenDream.ATOMs.ContainsKey(screenObjectAtomID)) {
                        ATOM screenObject = Program.OpenDream.ATOMs[screenObjectAtomID];

                        if (Program.OpenDream.ScreenObjects.Contains(screenObject)) {
                            Program.OpenDream.ScreenObjects.Remove(screenObject);
                        } else {
                            Console.WriteLine("Delta state packet says to remove a screen object that's not there, and was ignored (ID " + screenObjectAtomID + ")");
                        }
                    } else {
                        Console.WriteLine("Delta state packet says to remove a screen object that doesn't exist, and was ignored (ID " + screenObjectAtomID + ")");
                    }
                }
            }
        }
    }
}
