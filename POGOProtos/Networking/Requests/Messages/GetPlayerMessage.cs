// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: POGOProtos/Networking/Requests/Messages/GetPlayerMessage.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace POGOProtos.Networking.Requests.Messages
{

    /// <summary>Holder for reflection information generated from POGOProtos/Networking/Requests/Messages/GetPlayerMessage.proto</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public static partial class GetPlayerMessageReflection
    {

        #region Descriptor
        /// <summary>File descriptor for POGOProtos/Networking/Requests/Messages/GetPlayerMessage.proto</summary>
        public static pbr::FileDescriptor Descriptor
        {
            get { return descriptor; }
        }
        private static pbr::FileDescriptor descriptor;

        static GetPlayerMessageReflection()
        {
            byte[] descriptorData = global::System.Convert.FromBase64String(
                string.Concat(
                  "Cj5QT0dPUHJvdG9zL05ldHdvcmtpbmcvUmVxdWVzdHMvTWVzc2FnZXMvR2V0",
                  "UGxheWVyTWVzc2FnZS5wcm90bxInUE9HT1Byb3Rvcy5OZXR3b3JraW5nLlJl",
                  "cXVlc3RzLk1lc3NhZ2VzIhIKEEdldFBsYXllck1lc3NhZ2ViBnByb3RvMw=="));
            descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
                new pbr::FileDescriptor[] { },
                new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::POGOProtos.Networking.Requests.Messages.GetPlayerMessage), global::POGOProtos.Networking.Requests.Messages.GetPlayerMessage.Parser, null, null, null, null), 
                }));
        }
        #endregion

    }
    #region Messages
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public sealed partial class GetPlayerMessage : pb::IMessage<GetPlayerMessage>
    {
        private static readonly pb::MessageParser<GetPlayerMessage> _parser = new pb::MessageParser<GetPlayerMessage>(() => new GetPlayerMessage());
        public static pb::MessageParser<GetPlayerMessage> Parser { get { return _parser; } }

        public static pbr::MessageDescriptor Descriptor
        {
            get { return global::POGOProtos.Networking.Requests.Messages.GetPlayerMessageReflection.Descriptor.MessageTypes[0]; }
        }

        pbr::MessageDescriptor pb::IMessage.Descriptor
        {
            get { return Descriptor; }
        }

        public GetPlayerMessage()
        {
            OnConstruction();
        }

        partial void OnConstruction();

        public GetPlayerMessage(GetPlayerMessage other) : this()
        {
        }

        public GetPlayerMessage Clone()
        {
            return new GetPlayerMessage(this);
        }

        public override bool Equals(object other)
        {
            return Equals(other as GetPlayerMessage);
        }

        public bool Equals(GetPlayerMessage other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 1;
            return hash;
        }

        public override string ToString()
        {
            return pb::JsonFormatter.ToDiagnosticString(this);
        }

        public void WriteTo(pb::CodedOutputStream output)
        {
        }

        public int CalculateSize()
        {
            int size = 0;
            return size;
        }

        public void MergeFrom(GetPlayerMessage other)
        {
            if (other == null)
            {
                return;
            }
        }

        public void MergeFrom(pb::CodedInputStream input)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                switch (tag)
                {
                    default:
                        input.SkipLastField();
                        break;
                }
            }
        }

    }

    #endregion

}

#endregion Designer generated code
