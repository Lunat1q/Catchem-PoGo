// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: POGOProtos/Networking/Requests/Messages/CheckChallenge.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace POGOProtos.Networking.Requests.Messages {

  /// <summary>Holder for reflection information generated from POGOProtos/Networking/Requests/Messages/CheckChallenge.proto</summary>
  public static partial class CheckChallengeReflection {

    #region Descriptor
    /// <summary>File descriptor for POGOProtos/Networking/Requests/Messages/CheckChallenge.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static CheckChallengeReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CjxQT0dPUHJvdG9zL05ldHdvcmtpbmcvUmVxdWVzdHMvTWVzc2FnZXMvQ2hl",
            "Y2tDaGFsbGVuZ2UucHJvdG8SJ1BPR09Qcm90b3MuTmV0d29ya2luZy5SZXF1",
            "ZXN0cy5NZXNzYWdlcyIuChVDaGVja0NoYWxsZW5nZU1lc3NhZ2USFQoNZGVi",
            "dWdfcmVxdWVzdBgBIAEoCGIGcHJvdG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::POGOProtos.Networking.Requests.Messages.CheckChallengeMessage), global::POGOProtos.Networking.Requests.Messages.CheckChallengeMessage.Parser, new[]{ "DebugRequest" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class CheckChallengeMessage : pb::IMessage<CheckChallengeMessage> {
    private static readonly pb::MessageParser<CheckChallengeMessage> _parser = new pb::MessageParser<CheckChallengeMessage>(() => new CheckChallengeMessage());
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<CheckChallengeMessage> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::POGOProtos.Networking.Requests.Messages.CheckChallengeReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public CheckChallengeMessage() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public CheckChallengeMessage(CheckChallengeMessage other) : this() {
      debugRequest_ = other.debugRequest_;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public CheckChallengeMessage Clone() {
      return new CheckChallengeMessage(this);
    }

    /// <summary>Field number for the "debug_request" field.</summary>
    public const int DebugRequestFieldNumber = 1;
    private bool debugRequest_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool DebugRequest {
      get { return debugRequest_; }
      set {
        debugRequest_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as CheckChallengeMessage);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(CheckChallengeMessage other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (DebugRequest != other.DebugRequest) return false;
      return true;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (DebugRequest != false) hash ^= DebugRequest.GetHashCode();
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (DebugRequest != false) {
        output.WriteRawTag(8);
        output.WriteBool(DebugRequest);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (DebugRequest != false) {
        size += 1 + 1;
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(CheckChallengeMessage other) {
      if (other == null) {
        return;
      }
      if (other.DebugRequest != false) {
        DebugRequest = other.DebugRequest;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            input.SkipLastField();
            break;
          case 8: {
            DebugRequest = input.ReadBool();
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
