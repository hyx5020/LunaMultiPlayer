﻿namespace LunaCommon.Message.Interface
{
    public interface IMessageData
    {
        /// <summary>
        /// This method is made to clone the message and use a memberwise copy
        /// </summary>
        IMessageData Clone();

        /// <summary>
        /// Retrieves the version of the multiplayer
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Receive time timestamp.
        /// </summary>
        long ReceiveTime { get; set; }

        /// <summary>
        /// Subtype message identifier (Chat messages for example)
        /// </summary>
        ushort SubType { get; }

        /// <summary>
        /// Sent time timestamp.
        /// </summary>
        long SentTime { get; set; }
    }
}