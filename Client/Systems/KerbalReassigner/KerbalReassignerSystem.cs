﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LunaClient.Systems.KerbalReassigner
{
    public class KerbalReassignerSystem : Base.System
    {
        private KerbalReassignerEvents KerbalReassignerEvents { get; } = new KerbalReassignerEvents();
        public Dictionary<Guid, List<string>> VesselToKerbal { get; } = new Dictionary<Guid, List<string>>();
        public Dictionary<string, Guid> KerbalToVessel { get; } = new Dictionary<string, Guid>();

        #region Base overrides

        protected override void OnEnabled()
        {
            base.OnEnabled();
            GameEvents.onVesselCreate.Add(KerbalReassignerEvents.OnVesselCreate);
            GameEvents.onVesselWasModified.Add(KerbalReassignerEvents.OnVesselWasModified);
            GameEvents.onVesselDestroy.Add(KerbalReassignerEvents.OnVesselDestroyed);
            GameEvents.onFlightReady.Add(KerbalReassignerEvents.OnFlightReady);
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            GameEvents.onVesselCreate.Remove(KerbalReassignerEvents.OnVesselCreate);
            GameEvents.onVesselWasModified.Remove(KerbalReassignerEvents.OnVesselWasModified);
            GameEvents.onVesselDestroy.Remove(KerbalReassignerEvents.OnVesselDestroyed);
            GameEvents.onFlightReady.Remove(KerbalReassignerEvents.OnFlightReady);
            VesselToKerbal.Clear();
            KerbalToVessel.Clear();
        }

        #endregion

        #region Public methods

        public void DodgeKerbals(ConfigNode inputNode, Guid protovesselId)
        {
            var takenKerbals = new List<string>();
            foreach (var partNode in inputNode.GetNodes("PART"))
            {
                var crewIndex = 0;
                foreach (var currentKerbalName in partNode.GetValues("crew"))
                {
                    if (KerbalToVessel.ContainsKey(currentKerbalName) && KerbalToVessel[currentKerbalName] != protovesselId)
                    {
                        ProtoCrewMember newKerbal = null;
                        var newKerbalGender = GetKerbalGender(currentKerbalName);
                        string newExperienceTrait = null;
                        if (HighLogic.CurrentGame.CrewRoster.Exists(currentKerbalName))
                        {
                            var oldKerbal = HighLogic.CurrentGame.CrewRoster[currentKerbalName];
                            newKerbalGender = oldKerbal.gender;
                            newExperienceTrait = oldKerbal.experienceTrait.TypeName;
                        }
                        foreach (var possibleKerbal in HighLogic.CurrentGame.CrewRoster.Crew)
                        {
                            var kerbalOk =
                                !(KerbalToVessel.ContainsKey(possibleKerbal.name) &&
                                  (takenKerbals.Contains(possibleKerbal.name) ||
                                   KerbalToVessel[possibleKerbal.name] != protovesselId));

                            kerbalOk &= possibleKerbal.gender == newKerbalGender;
                            kerbalOk &=
                                !(newExperienceTrait != null &&
                                  newExperienceTrait != possibleKerbal.experienceTrait.TypeName);

                            if (kerbalOk)
                            {
                                newKerbal = possibleKerbal;
                                break;
                            }
                        }
                        while (newKerbal == null)
                        {
                            var possibleKerbal = HighLogic.CurrentGame.CrewRoster.GetNewKerbal();

                            var kerbalOk = possibleKerbal.gender == newKerbalGender;
                            kerbalOk &=
                                !(newExperienceTrait != null &&
                                  newExperienceTrait != possibleKerbal.experienceTrait.TypeName);

                            if (kerbalOk)
                                newKerbal = possibleKerbal;
                        }
                        partNode.SetValue("crew", newKerbal.name, crewIndex);
                        newKerbal.seatIdx = crewIndex;
                        newKerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                        takenKerbals.Add(newKerbal.name);
                    }
                    else
                    {
                        takenKerbals.Add(currentKerbalName);
                        CreateKerbalIfMissing(currentKerbalName, protovesselId);
                        HighLogic.CurrentGame.CrewRoster[currentKerbalName].rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                        HighLogic.CurrentGame.CrewRoster[currentKerbalName].seatIdx = crewIndex;
                    }
                    crewIndex++;
                }
            }

            VesselToKerbal[protovesselId] = takenKerbals;
            foreach (var name in takenKerbals)
                KerbalToVessel[name] = protovesselId;
        }

        public void CreateKerbalIfMissing(string kerbalName, Guid vesselId)
        {
            if (!HighLogic.CurrentGame.CrewRoster.Exists(kerbalName))
            {
                var pcm = CrewGenerator.RandomCrewMemberPrototype();
                pcm.ChangeName(kerbalName);
                pcm.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                HighLogic.CurrentGame.CrewRoster.AddCrewMember(pcm);
                LunaLog.Log($"[LMP]: Created kerbal {pcm.name} for vessel {vesselId}, Kerbal was missing");
            }
        }

        #endregion

        #region Private methods

        //Better not use a bool for this and enforce the gender binary on xir!
        private static ProtoCrewMember.Gender GetKerbalGender(string kerbalName)
        {
            var trimmedName = kerbalName;
            if (kerbalName.Contains(" Kerman"))
            {
                trimmedName = kerbalName.Substring(0, kerbalName.IndexOf(" Kerman", StringComparison.Ordinal));
                LunaLog.Log($"[LMP]: Trimming to '{trimmedName}'");
            }
            try
            {
                var femaleNames =
                    (string[])typeof(CrewGenerator).GetField("\u0004", BindingFlags.Static | BindingFlags.NonPublic)?
                        .GetValue(null);

                var femaleNamesPrefix =
                    (string[])typeof(CrewGenerator).GetField("\u0005", BindingFlags.Static | BindingFlags.NonPublic)?
                        .GetValue(null);

                var femaleNamesPostfix =
                    (string[])typeof(CrewGenerator).GetField("\u0006", BindingFlags.Static | BindingFlags.NonPublic)?
                        .GetValue(null);

                //Not part of the generator
                if (trimmedName == "Valentina")
                    return ProtoCrewMember.Gender.Female;

                if (femaleNames != null && femaleNames.Any(name => name == trimmedName))
                    return ProtoCrewMember.Gender.Female;

                if (femaleNamesPrefix != null && femaleNamesPostfix != null &&
                    femaleNamesPrefix.Where(fp => trimmedName.StartsWith(fp))
                        .Any(prefixName => femaleNamesPostfix.Any(postfixName => trimmedName == prefixName + postfixName)))
                {
                    return ProtoCrewMember.Gender.Female;
                }
            }
            catch (Exception e)
            {
                LunaLog.LogError($"[LMP]: LunaMultiPlayer Name identifier exception: {e}");
            }
            return ProtoCrewMember.Gender.Male;
        }

        #endregion
    }
}