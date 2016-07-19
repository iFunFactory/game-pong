﻿// Copyright 2013-2016 iFunFactory Inc. All Rights Reserved.
//
// This work is confidential and proprietary to iFunFactory Inc. and
// must not be used, disclosed, copied, or distributed without the prior
// consent of iFunFactory Inc.

using Fun;
using MiniJSON;
using ProtoBuf;
using System.Collections.Generic;
using UnityEngine;

// protobuf
using funapi.network.fun_message;
using funapi.management.maintenance_message;
using plugin_messages;


namespace Fun
{
    public class MessageHelper
    {
        public MessageHelper (FunapiSession session)
        {
            session_ = session;

            message_handler_["echo"] = OnEcho;
            message_handler_["pbuf_echo"] = OnEchoWithProtobuf;
            message_handler_["_maintenance"] = OnMaintenanceMessage;
        }

        public void OnReceivedMessage (string type, object message)
        {
            if (!message_handler_.ContainsKey(type))
            {
                FunDebug.Log("No handler for message '{0}'. Ignoring.", type);
                return;
            }

            message_handler_[type](message);
        }

        public void SendEchoMessage ()
        {
            if (session_.Started == false && !session_.SessionReliability)
            {
                FunDebug.Log("You should connect first.");
            }
            else
            {
                FunEncoding encoding = session_.GetEncoding(session_.DefaultProtocol);
                if (encoding == FunEncoding.kProtobuf)
                {
                    PbufEchoMessage echo = new PbufEchoMessage();
                    echo.msg = "hello proto";
                    FunMessage message = FunapiMessage.CreateFunMessage(echo, MessageType.pbuf_echo);
                    session_.SendMessage(MessageType.pbuf_echo, message);
                }
                if (encoding == FunEncoding.kJson)
                {
                    // In this example, we are using Dictionary<string, object>.
                    // But you can use your preferred Json implementation (e.g., Json.net) instead of Dictionary,
                    // by changing JsonHelper member in FunapiTransport.
                    Dictionary<string, object> message = new Dictionary<string, object>();
                    message["message"] = "hello world";
                    session_.SendMessage("echo", message);
                }
            }
        }

        public void OnEcho (object message)
        {
            FunDebug.Assert(message is Dictionary<string, object>);
            string strJson = Json.Serialize(message as Dictionary<string, object>);
            FunDebug.Log("Received an echo message: {0}", strJson);
        }

        public void OnEchoWithProtobuf (object message)
        {
            FunDebug.Assert(message is FunMessage);
            FunMessage msg = message as FunMessage;
            object obj = FunapiMessage.GetMessage(msg, MessageType.pbuf_echo);
            if (obj == null)
                return;

            PbufEchoMessage echo = obj as PbufEchoMessage;
            FunDebug.Log("Received an echo message: {0}", echo.msg);
        }

        public void OnMaintenanceMessage (object message)
        {
            FunEncoding encoding = session_.GetEncoding(session_.DefaultProtocol);
            if (encoding == FunEncoding.kProtobuf)
            {
                FunMessage msg = message as FunMessage;
                object obj = FunapiMessage.GetMessage(msg, MessageType.pbuf_maintenance);
                if (obj == null)
                    return;

                MaintenanceMessage maintenance = obj as MaintenanceMessage;
                FunDebug.Log("Maintenance message\nstart: {0}\nend: {1}\nmessage: {2}",
                             maintenance.date_start, maintenance.date_end, maintenance.messages);
            }
            else if (encoding == FunEncoding.kJson)
            {
                FunDebug.Assert(message is Dictionary<string, object>);
                Dictionary<string, object> msg = message as Dictionary<string, object>;
                FunDebug.Log("Maintenance message\nstart: {0}\nend: {1}\nmessage: {2}",
                             msg["date_start"], msg["date_end"], msg["messages"]);
            }
        }


        FunapiSession session_ = null;

        delegate void MessageHandler (object message);
        Dictionary<string, MessageHandler> message_handler_ = new Dictionary<string, MessageHandler>();
    }
}