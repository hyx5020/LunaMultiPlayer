﻿using LunaClient.Base;
using LunaClient.Systems.Asteroid;
using LunaClient.Systems.Chat;
using LunaClient.Systems.VesselRemoveSys;
using System;
using System.Collections.Generic;
using System.IO;
using UniLinq;
using UnityEngine;

namespace LunaClient.Systems.VesselProtoSys
{
    public class VesselLoader : SubSystem<VesselProtoSystem>
    {
        /// <summary>
        /// Load all the received vessels from the server into the game
        /// </summary>
        public void LoadVesselsIntoGame()
        {
            LunaLog.Log("[LMP]: Loading vessels in subspace 0 into game");
            var numberOfLoads = 0;

            foreach (var vessel in System.AllPlayerVessels)
            {
                if (vessel.Value.ProtoVessel != null && vessel.Value.ProtoVessel.vesselID == vessel.Key)
                {
                    RegisterServerAsteriodIfVesselIsAsteroid(vessel.Value.ProtoVessel);
                    HighLogic.CurrentGame.flightState.protoVessels.Add(vessel.Value.ProtoVessel);
                    numberOfLoads++;
                }
                else
                {
                    LunaLog.LogWarning($"[LMP]: Protovessel {vessel.Key} is DAMAGED!. Skipping load.");
                    SystemsContainer.Get<ChatSystem>().PmMessageServer($"WARNING: Protovessel {vessel.Key} is DAMAGED!. Skipping load.");
                }
                vessel.Value.Loaded = true;
            }

            LunaLog.Log($"[LMP]: {numberOfLoads} Vessels loaded into game");
        }

        /// <summary>
        /// Load a vessel into the game
        /// </summary>
        /// 
        public void LoadVessel(VesselProtoUpdate vesselProto)
        {
            try
            {
                LoadVesselImpl(vesselProto);
            }
            catch (Exception e)
            {
                LunaLog.LogError($"[LMP]: Error loading vessel: {e}");
            }
        }

        /// <summary>
        /// Performs the operation of actually loading the vessel into the game.  Does not handle errors.
        /// </summary>
        /// <param name="vesselProto"></param>
        private static void LoadVesselImpl(VesselProtoUpdate vesselProto)
        {
            if (ProtoVesselValidationsPassed(vesselProto.ProtoVessel))
            {
                RegisterServerAsteriodIfVesselIsAsteroid(vesselProto.ProtoVessel);

                if (FlightGlobals.FindVessel(vesselProto.VesselId) == null)
                {
                    FixProtoVesselFlags(vesselProto.ProtoVessel);
                    vesselProto.Loaded = LoadVesselIntoGame(vesselProto.ProtoVessel);
                }
            }
        }

        /// <summary>
        /// Reloads an existing vessel into the game
        /// Bear in mind that this method won't reload the vessel unless the part count has changed.
        /// </summary>
        public bool ReloadVesselIfChanged(VesselProtoUpdate vesselProto)
        {
            try
            {
                var vessel = FlightGlobals.Vessels.FirstOrDefault(v => v.id == vesselProto.VesselId);
                if (vessel != null)
                {
                    //Load the existing target, if any.  We will use this to reset the target to the newly loaded vessel, if the vessel we're reloading is the one that is targeted.
                    var currentTarget = FlightGlobals.fetch.VesselTarget;

                    var vesselLoaded = false;
                    //TODO: Is BackupVessel() needed or can we just look at the protoVessel?
                    if (vesselProto.ProtoVessel.protoPartSnapshots.Count != vessel.BackupVessel().protoPartSnapshots.Count)
                    {
                        //If targeted, unloading the vessel will cause the target to be lost.  We'll have to reset it later.
                        SystemsContainer.Get<VesselRemoveSystem>().UnloadVessel(vessel);
                        LoadVesselImpl(vesselProto);
                        vesselLoaded = true;
                    }

                    //TODO: Handle when it's the active vessel for the FlightGlobals as well.  If you delete the active vessel, it ends badly.  Very badly.

                    //We do want to actually compare by reference--we want to see if the vessel object we're unloading and reloading is the one that's targeted.  If so, we need to
                    //reset the target to the new instance of the vessel
                    if (vesselLoaded && currentTarget?.GetVessel() == vessel)
                    {
                        //Fetch the new vessel information for the same vessel ID, as the unload/load creates a new game object, and we need to refer to the new one for the target
                        var newVessel = FlightGlobals.Vessels.FirstOrDefault(v => v.id == vesselProto.VesselId);

                        //Record the time immediately before calling SetVesselTarget
                        var currentTime = Time.realtimeSinceStartup;
                        FlightGlobals.fetch.SetVesselTarget(newVessel, true);

                        var messagesToRemove = new List<ScreenMessage>();
                        //Remove the "Target:" message created by SetVesselTarget
                        foreach (var message in ScreenMessages.Instance.ActiveMessages)
                        {
                            //If the message started on or after the SetVesselTarget call time, remove it, as it's the target message created by SetVesselTarget
                            if (message.startTime >= currentTime)
                            {
                                messagesToRemove.Add(message);
                            }
                        }

                        foreach (var message in messagesToRemove)
                        {
                            ScreenMessages.RemoveMessage(message);
                        }
                    }
                    return vesselLoaded;

                }
                else
                {
                    LoadVesselImpl(vesselProto);
                    return true;
                }

            }
            catch (Exception e)
            {
                LunaLog.LogError($"[LMP]: Error reloading vessel: {e}");
                return false;
            }
        }


        #region Private methods

        /// <summary>
        /// Check if we were spectating the vessel
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private static bool SpectatingProtoVessel(ProtoVessel currentProto)
        {
            return FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.id == currentProto.vesselID;
        }

        /// <summary>
        /// Checks if the protovessel is a target we have locked
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private static bool ProtoVesselIsTarget(ProtoVessel currentProto)
        {
            return FlightGlobals.fetch.VesselTarget != null && FlightGlobals.fetch.VesselTarget.GetVessel() != null &&
                   FlightGlobals.fetch.VesselTarget.GetVessel().id == currentProto.vesselID;
        }

        /// <summary>
        /// Do some basic validations over the protovessel
        /// </summary>
        private static bool ProtoVesselValidationsPassed(ProtoVessel vesselProto)
        {
            if (vesselProto == null)
            {
                LunaLog.LogError("[LMP]: protoVessel is null!");
                return false;
            }

            if (vesselProto.situation == Vessel.Situations.FLYING)
            {
                if (vesselProto.orbitSnapShot == null)
                {
                    LunaLog.Log("[LMP]: Skipping flying vessel load - Protovessel does not have an orbit snapshot");
                    return false;
                }
                var updateBody = FlightGlobals.Bodies[vesselProto.orbitSnapShot.ReferenceBodyIndex];
                if (updateBody == null)
                {
                    LunaLog.Log("[LMP]: Skipping flying vessel load - Could not find celestial body index {currentProto.orbitSnapShot.ReferenceBodyIndex}");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Fixes the flags urls in the vessel
        /// </summary>
        private static void FixProtoVesselFlags(ProtoVessel vesselProto)
        {
            foreach (var part in vesselProto.protoPartSnapshots)
            {
                //Fix up flag URLS.
                if (!string.IsNullOrEmpty(part.flagURL))
                {
                    var flagFile = Path.Combine(Path.Combine(Client.KspPath, "GameData"), $"{part.flagURL}.png");
                    if (!File.Exists(flagFile))
                    {
                        LunaLog.Log($"[LMP]: Flag '{part.flagURL}' doesn't exist, setting to default!");
                        part.flagURL = "Squad/Flags/default";
                    }
                }
            }
        }

        /// <summary>
        /// Registers an asteroid
        /// </summary>
        private static void RegisterServerAsteriodIfVesselIsAsteroid(ProtoVessel possibleAsteroid)
        {
            //Register asteroids from other players
            if (ProtoVesselIsAsteroid(possibleAsteroid))
                SystemsContainer.Get<AsteroidSystem>().RegisterServerAsteroid(possibleAsteroid.vesselID.ToString());
        }

        /// <summary>
        /// Checks if vessel is an asteroid
        /// </summary>
        private static bool ProtoVesselIsAsteroid(ProtoVessel possibleAsteroid)
        {
            return possibleAsteroid.vesselType == VesselType.SpaceObject &&
                   possibleAsteroid.protoPartSnapshots?.Count == 1 &&
                   possibleAsteroid.protoPartSnapshots[0].partName == "PotatoRoid";
        }


        /// <summary>
        /// Loads the vessel proto into the current game
        /// </summary>
        private static bool LoadVesselIntoGame(ProtoVessel currentProto)
        {
            LunaLog.Log($"[LMP]: Loading {currentProto.vesselID}, Name: {currentProto.vesselName}, type: {currentProto.vesselType}");
            currentProto.Load(HighLogic.CurrentGame.flightState);

            if (currentProto.vesselRef == null)
            {
                LunaLog.Log($"[LMP]: Protovessel {currentProto.vesselID} failed to create a vessel!");
                return false;
            }
            return true;
        }

        #endregion
    }
}