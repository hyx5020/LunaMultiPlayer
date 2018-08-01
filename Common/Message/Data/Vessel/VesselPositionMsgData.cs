﻿using Lidgren.Network;
using LunaCommon.Message.Types;

namespace LunaCommon.Message.Data.Vessel
{
    public class VesselPositionMsgData : VesselBaseMsgData
    {
        /// <inheritdoc />
        internal VesselPositionMsgData() { }
        public override VesselMessageType VesselMessageType => VesselMessageType.Position;

        //Avoid using reference types in this message as it can generate allocations and is sent VERY often.
        public int BodyIndex;
        public int SubspaceId;
        public bool Landed;
        public bool Splashed;
        public double[] LatLonAlt = new double[3];
        public double[] FloatingOriginLatLonAlt = new double[3];
        public double[] FloatingOriginNonKrakensbaneLatLonAlt = new double[3];
        public float[] TransformPos = new float[3];
        public double[] NormalVector = new double[3];
        public double[] Velocity = new double[3];
        public double[] Orbit = new double[8];
        public float[] SrfRelRotation = new float[4];
        public float HeightFromTerrain;
        public bool HackingGravity;

        public override string ClassName { get; } = nameof(VesselPositionMsgData);

        internal override void InternalSerialize(NetOutgoingMessage lidgrenMsg)
        {
            base.InternalSerialize(lidgrenMsg);

            lidgrenMsg.Write(BodyIndex);
            lidgrenMsg.Write(SubspaceId);
            lidgrenMsg.Write(Landed);
            lidgrenMsg.Write(Splashed);

            for (var i = 0; i < 3; i++)
                lidgrenMsg.Write(LatLonAlt[i]);

            for (var i = 0; i < 3; i++)
                lidgrenMsg.Write(FloatingOriginLatLonAlt[i]);

            for (var i = 0; i < 3; i++)
                lidgrenMsg.Write(FloatingOriginNonKrakensbaneLatLonAlt[i]);

            for (var i = 0; i < 3; i++)
                lidgrenMsg.Write(TransformPos[i]);

            for (var i = 0; i < 3; i++)
                lidgrenMsg.Write(NormalVector[i]);

            for (var i = 0; i < 3; i++)
                lidgrenMsg.Write(Velocity[i]);

            for (var i = 0; i < 8; i++)
                lidgrenMsg.Write(Orbit[i]);

            for (var i = 0; i < 4; i++)
                lidgrenMsg.Write(SrfRelRotation[i]);

            lidgrenMsg.Write(HeightFromTerrain);
            lidgrenMsg.Write(HackingGravity);
        }

        internal override void InternalDeserialize(NetIncomingMessage lidgrenMsg)
        {
            base.InternalDeserialize(lidgrenMsg);

            BodyIndex = lidgrenMsg.ReadInt32();
            SubspaceId = lidgrenMsg.ReadInt32();
            Landed = lidgrenMsg.ReadBoolean();
            Splashed = lidgrenMsg.ReadBoolean();

            for (var i = 0; i < 3; i++)
                LatLonAlt[i] = lidgrenMsg.ReadDouble();

            for (var i = 0; i < 3; i++)
                FloatingOriginLatLonAlt[i] = lidgrenMsg.ReadDouble();

            for (var i = 0; i < 3; i++)
                FloatingOriginNonKrakensbaneLatLonAlt[i] = lidgrenMsg.ReadDouble();

            for (var i = 0; i < 3; i++)
                TransformPos[i] = lidgrenMsg.ReadFloat();

            for (var i = 0; i < 3; i++)
                NormalVector[i] = lidgrenMsg.ReadDouble();
            
            for (var i = 0; i < 3; i++)
                Velocity[i] = lidgrenMsg.ReadDouble();

            for (var i = 0; i < 8; i++)
                Orbit[i] = lidgrenMsg.ReadDouble();
            
            for (var i = 0; i < 4; i++)
                SrfRelRotation[i] = lidgrenMsg.ReadFloat();

            HeightFromTerrain = lidgrenMsg.ReadFloat();
            HackingGravity = lidgrenMsg.ReadBoolean();
        }
        
        internal override int InternalGetMessageSize()
        {
            return base.InternalGetMessageSize() + sizeof(int) * 2 + sizeof(bool) * 2 + sizeof(double) * 3 * 5 + 
                sizeof(float) * 4 * 1 + sizeof(float) * 3 * 1 + sizeof(float) + sizeof(bool);
        }
    }
}
