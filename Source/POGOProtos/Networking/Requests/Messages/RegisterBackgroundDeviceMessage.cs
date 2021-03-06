// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: POGOProtos/Networking/Requests/Messages/RegisterBackgroundDeviceMessage.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace POGOProtos.Networking.Requests.Messages {

  /// <summary>Holder for reflection information generated from POGOProtos/Networking/Requests/Messages/RegisterBackgroundDeviceMessage.proto</summary>
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  public static partial class RegisterBackgroundDeviceMessageReflection {

    #region Descriptor
    /// <summary>File descriptor for POGOProtos/Networking/Requests/Messages/RegisterBackgroundDeviceMessage.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static RegisterBackgroundDeviceMessageReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Ck1QT0dPUHJvdG9zL05ldHdvcmtpbmcvUmVxdWVzdHMvTWVzc2FnZXMvUmVn",
            "aXN0ZXJCYWNrZ3JvdW5kRGV2aWNlTWVzc2FnZS5wcm90bxInUE9HT1Byb3Rv",
            "cy5OZXR3b3JraW5nLlJlcXVlc3RzLk1lc3NhZ2VzIkkKH1JlZ2lzdGVyQmFj",
            "a2dyb3VuZERldmljZU1lc3NhZ2USEwoLZGV2aWNlX3R5cGUYASABKAkSEQoJ",
            "ZGV2aWNlX2lkGAIgASgJYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::POGOProtos.Networking.Requests.Messages.RegisterBackgroundDeviceMessage), global::POGOProtos.Networking.Requests.Messages.RegisterBackgroundDeviceMessage.Parser, new[]{ "DeviceType", "DeviceId" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  public sealed partial class RegisterBackgroundDeviceMessage : pb::IMessage<RegisterBackgroundDeviceMessage> {
    private static readonly pb::MessageParser<RegisterBackgroundDeviceMessage> _parser = new pb::MessageParser<RegisterBackgroundDeviceMessage>(() => new RegisterBackgroundDeviceMessage());
    public static pb::MessageParser<RegisterBackgroundDeviceMessage> Parser { get { return _parser; } }

    public static pbr::MessageDescriptor Descriptor {
      get { return global::POGOProtos.Networking.Requests.Messages.RegisterBackgroundDeviceMessageReflection.Descriptor.MessageTypes[0]; }
    }

    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    public RegisterBackgroundDeviceMessage() {
      OnConstruction();
    }

    partial void OnConstruction();

    public RegisterBackgroundDeviceMessage(RegisterBackgroundDeviceMessage other) : this() {
      deviceType_ = other.deviceType_;
      deviceId_ = other.deviceId_;
    }

    public RegisterBackgroundDeviceMessage Clone() {
      return new RegisterBackgroundDeviceMessage(this);
    }

    /// <summary>Field number for the "device_type" field.</summary>
    public const int DeviceTypeFieldNumber = 1;
    private string deviceType_ = "";
    public string DeviceType {
      get { return deviceType_; }
      set {
        deviceType_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "device_id" field.</summary>
    public const int DeviceIdFieldNumber = 2;
    private string deviceId_ = "";
    public string DeviceId {
      get { return deviceId_; }
      set {
        deviceId_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    public override bool Equals(object other) {
      return Equals(other as RegisterBackgroundDeviceMessage);
    }

    public bool Equals(RegisterBackgroundDeviceMessage other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (DeviceType != other.DeviceType) return false;
      if (DeviceId != other.DeviceId) return false;
      return true;
    }

    public override int GetHashCode() {
      int hash = 1;
      if (DeviceType.Length != 0) hash ^= DeviceType.GetHashCode();
      if (DeviceId.Length != 0) hash ^= DeviceId.GetHashCode();
      return hash;
    }

    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    public void WriteTo(pb::CodedOutputStream output) {
      if (DeviceType.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(DeviceType);
      }
      if (DeviceId.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(DeviceId);
      }
    }

    public int CalculateSize() {
      int size = 0;
      if (DeviceType.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(DeviceType);
      }
      if (DeviceId.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(DeviceId);
      }
      return size;
    }

    public void MergeFrom(RegisterBackgroundDeviceMessage other) {
      if (other == null) {
        return;
      }
      if (other.DeviceType.Length != 0) {
        DeviceType = other.DeviceType;
      }
      if (other.DeviceId.Length != 0) {
        DeviceId = other.DeviceId;
      }
    }

    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            input.SkipLastField();
            break;
          case 10: {
            DeviceType = input.ReadString();
            break;
          }
          case 18: {
            DeviceId = input.ReadString();
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
