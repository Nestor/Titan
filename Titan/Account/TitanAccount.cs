using System;
using System.Collections.Generic;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.CSGO.Internal;
using Titan.Json;
using Titan.MatchID.Live;
using Titan.Meta;

namespace Titan.Account
{
    
    // ReSharper disable InconsistentNaming
    public abstract class TitanAccount
    {

        public const uint TF2_APPID = 440;
        public const uint CSGO_APPID = 730;
        
        ////////////////////////////////////////////////////
        // INIT
        ////////////////////////////////////////////////////

        public JsonAccounts.JsonAccount JsonAccount;

        public Dictionary<string, string> Cookies = new Dictionary<string, string>();

        internal TitanAccount(JsonAccounts.JsonAccount json)
        {
            JsonAccount = json;
        }

        ~TitanAccount()
        {
            if (IsRunning)
            {
                Stop();
            }
        }

        ////////////////////////////////////////////////////
        // TIME
        ////////////////////////////////////////////////////

        public long StartEpoch { get; set; }
        
        public bool IsRunning { get; set; }
        
        public bool IsAuthenticated { get; set; }

        ////////////////////////////////////////////////////
        // GENERAL
        ////////////////////////////////////////////////////

        public abstract Result Start();
        public abstract void Stop();

        public abstract void OnConnected(SteamClient.ConnectedCallback callback);
        public abstract void OnDisconnected(SteamClient.DisconnectedCallback callback);

        public abstract void OnLoggedOn(SteamUser.LoggedOnCallback callback);
        public abstract void OnLoggedOff(SteamUser.LoggedOffCallback callback);

        ////////////////////////////////////////////////////
        // STEAM WEB INTERFACE
        ////////////////////////////////////////////////////

        public abstract void LoginWebInterface(ulong steamID);

        public abstract void JoinSteamGroup(uint groupID = 28495194); // 28495194 is /groups/TitanReportBot

        public abstract void AddFreeLicense(uint appID = TF2_APPID); // For TF2

        ////////////////////////////////////////////////////
        // GAME COORDINATOR
        ////////////////////////////////////////////////////

        public void OnGCMessage(SteamGameCoordinator.MessageCallback callback)
        {
            var map = new Dictionary<uint, Action<IPacketGCMsg>>
            {
                { (uint) EGCBaseClientMsg.k_EMsgGCClientWelcome, OnClientWelcome },
                { (uint) ECsgoGCMsg.k_EMsgGCCStrike15_v2_ClientReportResponse, OnReportResponse },
                { (uint) ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchmakingGC2ClientHello, OnMatchmakingHelloResponse },
                { (uint) ECsgoGCMsg.k_EMsgGCCStrike15_v2_ClientCommendPlayerQueryResponse, OnCommendResponse },
                { (uint) ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchList, OnLiveGameRequestResponse }
            };

            if (map.TryGetValue(callback.EMsg, out var func))
            {
                func(callback.Message);
            }
        }

        public abstract void OnClientWelcome(IPacketGCMsg msg);
        public abstract void OnMatchmakingHelloResponse(IPacketGCMsg msg);
        public abstract void OnReportResponse(IPacketGCMsg msg);
        public abstract void OnCommendResponse(IPacketGCMsg msg);
        public abstract void OnLiveGameRequestResponse(IPacketGCMsg msg);

        ////////////////////////////////////////////////////
        // BOTTING SESSION INFORMATIONS
        ////////////////////////////////////////////////////

        public ReportInfo _reportInfo; // Setting this to private will cause it not to be visible for inheritated classes

        public void FeedReportInfo(ReportInfo info)
        {
            _reportInfo = info;
        }
        
        public CommendInfo _commendInfo; // Setting this to private will cause it not to be visible for inheritated classes

        public void FeedCommendInfo(CommendInfo info)
        {
            _commendInfo = info;
        }

        public LiveGameInfo _liveGameInfo; // Setting this to private will cause it not to be visible for inheritated classes
        public MatchInfo MatchInfo;

        public void FeedLiveGameInfo(LiveGameInfo info)
        {
            _liveGameInfo = info;
        }

        public uint GetTargetAccountID()
        {
            if (_reportInfo != null)
            {
                return _reportInfo.SteamID.AccountID;
            }

            if (_commendInfo != null)
            {
                return _commendInfo.SteamID.AccountID;
            }

            if (_liveGameInfo != null)
            {
                return _liveGameInfo.SteamID.AccountID;
            }

            return 0;
        }

        public uint GetAppID()
        {
            if (_reportInfo != null)
            {
                return _reportInfo.AppID;
            } 
            
            if (_commendInfo != null)
            {
                return _commendInfo.AppID;
            } 
            
            if (_liveGameInfo != null)
            {
                return _liveGameInfo.AppID;
            }

            return CSGO_APPID; // Default to CS:GO App ID
        }

        ////////////////////////////////////////////////////
        // PAYLOADS
        ////////////////////////////////////////////////////

        public ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientReportPlayer> GetReportPayload()
        {
            var payload = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientReportPlayer>(
                (uint) ECsgoGCMsg.k_EMsgGCCStrike15_v2_ClientReportPlayer
            )
            {
                Body =
                {
                    account_id = _reportInfo.SteamID.AccountID,
                    match_id = _reportInfo.MatchID,
                    rpt_aimbot = Convert.ToUInt32(_reportInfo.AimHacking),
                    rpt_wallhack = Convert.ToUInt32(_reportInfo.WallHacking),
                    rpt_speedhack = Convert.ToUInt32(_reportInfo.OtherHacking),
                    rpt_teamharm = Convert.ToUInt32(_reportInfo.Griefing),
                    rpt_textabuse = Convert.ToUInt32(_reportInfo.AbusiveText),
                    rpt_voiceabuse = Convert.ToUInt32(_reportInfo.AbusiveVoice)
                }
            };

            return payload;
        }

        public ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientCommendPlayer> GetCommendPayload()
        {
            var payload = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientCommendPlayer>(
                (uint) ECsgoGCMsg.k_EMsgGCCStrike15_v2_ClientCommendPlayer
            )
            {
                Body =
                {
                    account_id = _commendInfo.SteamID.AccountID,
                    match_id = 0,
                    commendation = new PlayerCommendationInfo
                    {
                        cmd_friendly = Convert.ToUInt32(_commendInfo.Friendly),
                        cmd_teaching = Convert.ToUInt32(_commendInfo.Teacher),
                        cmd_leader = Convert.ToUInt32(_commendInfo.Leader)
                    },
                    tokens = 0
                }
            };

            return payload;
        }

        public ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchListRequestLiveGameForUser> GetLiveGamePayload()
        {
            var payload = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchListRequestLiveGameForUser>(
                (uint) ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchListRequestLiveGameForUser
            ) 
            {
                Body =
                {
                    accountid = _liveGameInfo.SteamID.AccountID
                }
            };

            return payload;
        }

        public ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientRequestPlayersProfile> GetRequestPlayerProfile()
        {
            var payload = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientRequestPlayersProfile>(
                (uint) ECsgoGCMsg.k_EMsgGCCStrike15_v2_ClientRequestPlayersProfile
            )
            {
                Body =
                {
                    account_id = GetTargetAccountID(),
                    account_idSpecified = true
                }
            };

            return payload;
        }

    }
}
