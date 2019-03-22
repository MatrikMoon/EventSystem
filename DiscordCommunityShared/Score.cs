// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: score.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from score.proto</summary>
public static partial class ScoreReflection {

  #region Descriptor
  /// <summary>File descriptor for score.proto</summary>
  public static pbr::FileDescriptor Descriptor {
    get { return descriptor; }
  }
  private static pbr::FileDescriptor descriptor;

  static ScoreReflection() {
    byte[] descriptorData = global::System.Convert.FromBase64String(
        string.Concat(
          "CgtzY29yZS5wcm90byK0AQoFU2NvcmUSEAoIc3RlYW1faWQYASABKAkSDwoH",
          "c29uZ19pZBgCIAEoCRINCgVzY29yZRgDIAEoBRIYChBkaWZmaWN1bHR5X2xl",
          "dmVsGAQgASgFEhIKCmZ1bGxfY29tYm8YBSABKAgSDgoGc2lnbmVkGAYgASgJ",
          "EhYKDnBsYXllcl9vcHRpb25zGAcgASgFEhQKDGdhbWVfb3B0aW9ucxgIIAEo",
          "BRINCgVzcGVlZBgJIAEoBWIGcHJvdG8z"));
    descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
        new pbr::FileDescriptor[] { },
        new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
          new pbr::GeneratedClrTypeInfo(typeof(global::Score), global::Score.Parser, new[]{ "SteamId", "SongId", "Score_", "DifficultyLevel", "FullCombo", "Signed", "PlayerOptions", "GameOptions", "Speed" }, null, null, null)
        }));
  }
  #endregion

}
#region Messages
public sealed partial class Score : pb::IMessage<Score> {
  private static readonly pb::MessageParser<Score> _parser = new pb::MessageParser<Score>(() => new Score());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pb::MessageParser<Score> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::ScoreReflection.Descriptor.MessageTypes[0]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public Score() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public Score(Score other) : this() {
    steamId_ = other.steamId_;
    songId_ = other.songId_;
    score_ = other.score_;
    difficultyLevel_ = other.difficultyLevel_;
    fullCombo_ = other.fullCombo_;
    signed_ = other.signed_;
    playerOptions_ = other.playerOptions_;
    gameOptions_ = other.gameOptions_;
    speed_ = other.speed_;
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public Score Clone() {
    return new Score(this);
  }

  /// <summary>Field number for the "steam_id" field.</summary>
  public const int SteamIdFieldNumber = 1;
  private string steamId_ = "";
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public string SteamId {
    get { return steamId_; }
    set {
      steamId_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
    }
  }

  /// <summary>Field number for the "song_id" field.</summary>
  public const int SongIdFieldNumber = 2;
  private string songId_ = "";
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public string SongId {
    get { return songId_; }
    set {
      songId_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
    }
  }

  /// <summary>Field number for the "score" field.</summary>
  public const int Score_FieldNumber = 3;
  private int score_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int Score_ {
    get { return score_; }
    set {
      score_ = value;
    }
  }

  /// <summary>Field number for the "difficulty_level" field.</summary>
  public const int DifficultyLevelFieldNumber = 4;
  private int difficultyLevel_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int DifficultyLevel {
    get { return difficultyLevel_; }
    set {
      difficultyLevel_ = value;
    }
  }

  /// <summary>Field number for the "full_combo" field.</summary>
  public const int FullComboFieldNumber = 5;
  private bool fullCombo_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public bool FullCombo {
    get { return fullCombo_; }
    set {
      fullCombo_ = value;
    }
  }

  /// <summary>Field number for the "signed" field.</summary>
  public const int SignedFieldNumber = 6;
  private string signed_ = "";
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public string Signed {
    get { return signed_; }
    set {
      signed_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
    }
  }

  /// <summary>Field number for the "player_options" field.</summary>
  public const int PlayerOptionsFieldNumber = 7;
  private int playerOptions_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int PlayerOptions {
    get { return playerOptions_; }
    set {
      playerOptions_ = value;
    }
  }

  /// <summary>Field number for the "game_options" field.</summary>
  public const int GameOptionsFieldNumber = 8;
  private int gameOptions_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int GameOptions {
    get { return gameOptions_; }
    set {
      gameOptions_ = value;
    }
  }

  /// <summary>Field number for the "speed" field.</summary>
  public const int SpeedFieldNumber = 9;
  private int speed_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int Speed {
    get { return speed_; }
    set {
      speed_ = value;
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override bool Equals(object other) {
    return Equals(other as Score);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public bool Equals(Score other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (SteamId != other.SteamId) return false;
    if (SongId != other.SongId) return false;
    if (Score_ != other.Score_) return false;
    if (DifficultyLevel != other.DifficultyLevel) return false;
    if (FullCombo != other.FullCombo) return false;
    if (Signed != other.Signed) return false;
    if (PlayerOptions != other.PlayerOptions) return false;
    if (GameOptions != other.GameOptions) return false;
    if (Speed != other.Speed) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override int GetHashCode() {
    int hash = 1;
    if (SteamId.Length != 0) hash ^= SteamId.GetHashCode();
    if (SongId.Length != 0) hash ^= SongId.GetHashCode();
    if (Score_ != 0) hash ^= Score_.GetHashCode();
    if (DifficultyLevel != 0) hash ^= DifficultyLevel.GetHashCode();
    if (FullCombo != false) hash ^= FullCombo.GetHashCode();
    if (Signed.Length != 0) hash ^= Signed.GetHashCode();
    if (PlayerOptions != 0) hash ^= PlayerOptions.GetHashCode();
    if (GameOptions != 0) hash ^= GameOptions.GetHashCode();
    if (Speed != 0) hash ^= Speed.GetHashCode();
    if (_unknownFields != null) {
      hash ^= _unknownFields.GetHashCode();
    }
    return hash;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override string ToString() {
    return pb::JsonFormatter.ToDiagnosticString(this);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void WriteTo(pb::CodedOutputStream output) {
    if (SteamId.Length != 0) {
      output.WriteRawTag(10);
      output.WriteString(SteamId);
    }
    if (SongId.Length != 0) {
      output.WriteRawTag(18);
      output.WriteString(SongId);
    }
    if (Score_ != 0) {
      output.WriteRawTag(24);
      output.WriteInt32(Score_);
    }
    if (DifficultyLevel != 0) {
      output.WriteRawTag(32);
      output.WriteInt32(DifficultyLevel);
    }
    if (FullCombo != false) {
      output.WriteRawTag(40);
      output.WriteBool(FullCombo);
    }
    if (Signed.Length != 0) {
      output.WriteRawTag(50);
      output.WriteString(Signed);
    }
    if (PlayerOptions != 0) {
      output.WriteRawTag(56);
      output.WriteInt32(PlayerOptions);
    }
    if (GameOptions != 0) {
      output.WriteRawTag(64);
      output.WriteInt32(GameOptions);
    }
    if (Speed != 0) {
      output.WriteRawTag(72);
      output.WriteInt32(Speed);
    }
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int CalculateSize() {
    int size = 0;
    if (SteamId.Length != 0) {
      size += 1 + pb::CodedOutputStream.ComputeStringSize(SteamId);
    }
    if (SongId.Length != 0) {
      size += 1 + pb::CodedOutputStream.ComputeStringSize(SongId);
    }
    if (Score_ != 0) {
      size += 1 + pb::CodedOutputStream.ComputeInt32Size(Score_);
    }
    if (DifficultyLevel != 0) {
      size += 1 + pb::CodedOutputStream.ComputeInt32Size(DifficultyLevel);
    }
    if (FullCombo != false) {
      size += 1 + 1;
    }
    if (Signed.Length != 0) {
      size += 1 + pb::CodedOutputStream.ComputeStringSize(Signed);
    }
    if (PlayerOptions != 0) {
      size += 1 + pb::CodedOutputStream.ComputeInt32Size(PlayerOptions);
    }
    if (GameOptions != 0) {
      size += 1 + pb::CodedOutputStream.ComputeInt32Size(GameOptions);
    }
    if (Speed != 0) {
      size += 1 + pb::CodedOutputStream.ComputeInt32Size(Speed);
    }
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(Score other) {
    if (other == null) {
      return;
    }
    if (other.SteamId.Length != 0) {
      SteamId = other.SteamId;
    }
    if (other.SongId.Length != 0) {
      SongId = other.SongId;
    }
    if (other.Score_ != 0) {
      Score_ = other.Score_;
    }
    if (other.DifficultyLevel != 0) {
      DifficultyLevel = other.DifficultyLevel;
    }
    if (other.FullCombo != false) {
      FullCombo = other.FullCombo;
    }
    if (other.Signed.Length != 0) {
      Signed = other.Signed;
    }
    if (other.PlayerOptions != 0) {
      PlayerOptions = other.PlayerOptions;
    }
    if (other.GameOptions != 0) {
      GameOptions = other.GameOptions;
    }
    if (other.Speed != 0) {
      Speed = other.Speed;
    }
    _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(pb::CodedInputStream input) {
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
      switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
          break;
        case 10: {
          SteamId = input.ReadString();
          break;
        }
        case 18: {
          SongId = input.ReadString();
          break;
        }
        case 24: {
          Score_ = input.ReadInt32();
          break;
        }
        case 32: {
          DifficultyLevel = input.ReadInt32();
          break;
        }
        case 40: {
          FullCombo = input.ReadBool();
          break;
        }
        case 50: {
          Signed = input.ReadString();
          break;
        }
        case 56: {
          PlayerOptions = input.ReadInt32();
          break;
        }
        case 64: {
          GameOptions = input.ReadInt32();
          break;
        }
        case 72: {
          Speed = input.ReadInt32();
          break;
        }
      }
    }
  }

}

#endregion


#endregion Designer generated code
