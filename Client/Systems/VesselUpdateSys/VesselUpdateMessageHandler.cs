﻿using LunaClient.Base;
using LunaClient.Base.Interface;
using LunaClient.VesselStore;
using LunaClient.VesselUtilities;
using LunaCommon.Message.Data.Vessel;
using LunaCommon.Message.Interface;
using System;
using System.Collections.Concurrent;

namespace LunaClient.Systems.VesselUpdateSys
{
    public class VesselUpdateMessageHandler : SubSystem<VesselUpdateSystem>, IMessageHandler
    {
        public ConcurrentQueue<IServerMessageBase> IncomingMessages { get; set; } = new ConcurrentQueue<IServerMessageBase>();

        public void HandleMessage(IServerMessageBase msg)
        {
            if (!(msg.Data is VesselUpdateMsgData msgData) || !System.UpdateSystemReady) return;

            var vessel = FlightGlobals.FindVessel(msgData.VesselId);
            if (vessel == null) return;
            
            UpdateVesselFields(vessel, msgData);
            UpdateActionGroups(vessel, msgData);
            UpdateProtoVesselValues(vessel.protoVessel, msgData);
            VesselsProtoStore.UpdateVesselProtoValues(msgData);
        }

        private static void UpdateVesselFields(Vessel vessel, VesselUpdateMsgData msgData)
        {
            vessel.vesselName = msgData.Name;
            vessel.vesselType = (VesselType) Enum.Parse(typeof(VesselType), msgData.Type);
            vessel.situation = (Vessel.Situations) Enum.Parse(typeof(Vessel.Situations), msgData.Situation);
            vessel.Landed = msgData.Landed;
            vessel.landedAt = msgData.LandedAt;
            vessel.displaylandedAt = msgData.DisplayLandedAt;
            vessel.Splashed = msgData.Splashed;
            vessel.missionTime = msgData.MissionTime;
            vessel.launchTime = msgData.LaunchTime;
            vessel.lastUT = msgData.LastUt;
            vessel.isPersistent = msgData.Persistent;
            vessel.referenceTransformId = msgData.RefTransformId;
        }

        private static void UpdateActionGroups(Vessel vessel, VesselUpdateMsgData msgData)
        {
            for (var i = 0; i < 17; i++)
            {
                //Ignore SAS if we are spectating as it will fight with the FI
                if ((KSPActionGroup) (1 << (i & 31)) == KSPActionGroup.SAS && VesselCommon.IsSpectating &&
                    FlightGlobals.ActiveVessel?.id == vessel.id)
                {
                    vessel.ActionGroups.groups[i] = false;
                    continue;
                }

                vessel.ActionGroups.groups[i] = msgData.ActionGroups[i].State;
                vessel.ActionGroups.cooldownTimes[i] = msgData.ActionGroups[i].Time;
            }
        }

        private static void UpdateProtoVesselValues(ProtoVessel protoVessel, VesselUpdateMsgData msgData)
        {
            if (protoVessel != null)
            {
                protoVessel.vesselName = msgData.Name;
                protoVessel.vesselType = (VesselType)Enum.Parse(typeof(VesselType), msgData.Type);
                protoVessel.situation = (Vessel.Situations)Enum.Parse(typeof(Vessel.Situations), msgData.Situation);
                protoVessel.landed = msgData.Landed;
                protoVessel.landedAt = msgData.LandedAt;
                protoVessel.displaylandedAt = msgData.DisplayLandedAt;
                protoVessel.splashed = msgData.Splashed;
                protoVessel.missionTime = msgData.MissionTime;
                protoVessel.launchTime = msgData.LaunchTime;
                protoVessel.lastUT = msgData.LastUt;
                protoVessel.persistent = msgData.Persistent;
                protoVessel.refTransform = msgData.RefTransformId;

                //TODO: Do we need to update the protovessel action group values aswell?
            }
        }
    }
}
