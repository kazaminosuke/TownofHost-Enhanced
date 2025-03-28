using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TOHE.Modules;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Translator;
using static UnityEngine.RemoteConfigSettingsHelper;

namespace TOHE;

enum CustomRPC
{
    VersionCheck = 60,
    RequestRetryVersionCheck = 61,
    SyncCustomSettings = 80,
    RestTOHESetting,
    SetDeathReason,
    EndGame,
    PlaySound,
    SetCustomRole,
    SetBountyTarget,
    SyncPuppet,
    SetKillOrSpell,
    SetKillOrHex,
    SetKillOrCurse,
    SetSheriffShotLimit,
    //SetCopyCatMiscopyLimit,
    SetDousedPlayer,
    setPlaguedPlayer,
    SetNameColorData,
    DoSpell,
    DoHex,
    DoCurse,
    SniperSync,
    SetLoversPlayers,
    SetExecutionerTarget,
    RemoveExecutionerTarget,
    SetLawyerTarget,
    RemoveLawyerTarget,
    SendFireWorksState,
    SetCurrentDousingTarget,
    SetEvilTrackerTarget,
    SetRealKiller,

    // TOHE
    AntiBlackout,
    PlayCustomSound,
    SetKillTimer,
    SyncAllPlayerNames,
    SyncNameNotify,
    ShowPopUp,
    KillFlash,

    //Roles
    SetDrawPlayer,
    SetCurrentDrawTarget,
    SetGamerHealth,
    RpcPassBomb,
    SyncRomanticTarget,
    SyncVengefulRomanticTarget,
    SetJailerTarget,
    SetJailerExeLimit,
    SetCleanserCleanLimit,
    SetSoulCollectorLimit,
    SetPelicanEtenNum,
    SwordsManKill,
    SetAlchemistTimer,
    UndertakerLocationSync,
    SetCounterfeiterSellLimit,
    SetPursuerSellLimit,
    SetMedicalerProtectLimit,
    SetMiniLimit,
    SetMiniSellLimit,
    SetGangsterRecruitLimit,
    SetGhostPlayer,
    SetDarkHiderKillCount,
    SetEvilDiviner,
    SetGreedierOE,
    SetCursedWolfSpellCount,
    SetJinxSpellCount,
    SetCollectorVotes,
    SetSwapperVotes,
    SetQuickShooterShotLimit,
    SetEraseLimit,
    GuessKill,
    SetMarkedPlayer,
    SetConcealerTimer,
    SetMedicalerProtectList,
    SetHackerHackLimit,
    SyncPsychicRedList,
    SetMorticianArrow,
    SetTracefinderArrow,
    Judge,
    Guess,
    PresidentEnd,
    MeetingKill,
    MafiaRevenge,
    RetributionistRevenge,
    SetSwooperTimer,
    SetWraithTimer,
    SetShadeTimer,
    SetBKTimer,
    SetBansheeTimer,
    SyncTotocalcioTargetAndTimes,
    SetSuccubusCharmLimit,
    SetCursedSoulCurseLimit,
    SetMonarchKnightLimit,
    SetDeputyHandcuffLimit,
    SetInvestgatorLimit,
    SyncInvestigator, // Unused
    SetVirusInfectLimit,
    SetRevealedPlayer,
    SetCurrentRevealTarget,
    SetJackalRecruitLimit,
    SetBanditStealLimit,
    SetDoppelgangerStealLimit,
    SetBloodhoundArrow,
    SetVultureArrow,
    SetSpiritcallerSpiritLimit,
    SetDoomsayerProgress,
    SetTrackerTarget,
    SetSeekerTarget,
    SetSeekerPoints,
    SetPoliceLimlit,
    SetPotionMaster,
    SetChameleonTimer,
    SetAdmireLimit,
    SetRememberLimit,
    SetImitateLimit,
    SyncCovenLeader,
    SyncNWitch,
    SyncShroud,
    SyncMiniAge,
    SyncSabotageMasterSkill,
}
public enum Sounds
{
    KillSound,
    TaskComplete,
    TaskUpdateSound,
    ImpTransform,

    Test,
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal class RPCHandlerPatch
{
    public static bool TrustedRpc(byte id)
    => (CustomRPC)id is CustomRPC.VersionCheck or CustomRPC.RequestRetryVersionCheck or CustomRPC.AntiBlackout or CustomRPC.Judge or CustomRPC.MeetingKill or CustomRPC.Guess or CustomRPC.PresidentEnd or CustomRPC.MafiaRevenge or CustomRPC.RetributionistRevenge or CustomRPC.SetSwapperVotes;
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        var rpcType = (RpcCalls)callId;
        MessageReader subReader = MessageReader.Get(reader);
        if (EAC.ReceiveRpc(__instance, callId, reader)) return false;
        Logger.Info($"{__instance?.Data?.PlayerId}({(__instance?.Data?.PlayerId == 0 ? "Host" : __instance?.Data?.PlayerName)}):{callId}({RPC.GetRpcName(callId)})", "ReceiveRPC");
        switch (rpcType)
        {
            case RpcCalls.SetName: //SetNameRPC
                string name = subReader.ReadString();
                if (subReader.BytesRemaining > 0 && subReader.ReadBoolean()) return false;
                Logger.Info("RPC名称修改:" + __instance.GetNameWithRole() + " => " + name, "SetName");
                break;
            case RpcCalls.SetRole: //SetNameRPC
                var role = (RoleTypes)subReader.ReadUInt16();
                Logger.Info("RPC设置职业:" + __instance.GetRealName() + " => " + role, "SetRole");
                break;
            case RpcCalls.SendChat:
                var text = subReader.ReadString();
                Logger.Info($"{__instance.GetNameWithRole()}:{text}", "ReceiveChat");
                ChatCommands.OnReceiveChat(__instance, text, out var canceled);
                if (canceled) return false;
                break;
            case RpcCalls.StartMeeting:
                var p = Utils.GetPlayerById(subReader.ReadByte());
                Logger.Info($"{__instance.GetNameWithRole()} => {p?.GetNameWithRole() ?? "null"}", "StartMeeting");
                break;
        }
        if (__instance.PlayerId != 0
            && Enum.IsDefined(typeof(CustomRPC), (int)callId)
            && !TrustedRpc(callId)) //ホストではなく、CustomRPCで、VersionCheckではない
        {
            Logger.Warn($"{__instance?.Data?.PlayerName}:{callId}({RPC.GetRpcName(callId)}) 已取消，因为它是由主机以外的其他人发送的。", "CustomRPC");
            if (AmongUsClient.Instance.AmHost)
            {
                if (!EAC.ReceiveInvalidRpc(__instance, callId)) return false;
                AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), false);
                Logger.Warn($"收到来自 {__instance?.Data?.PlayerName} 的不受信用的RPC，因此将其踢出。", "Kick");
                Logger.SendInGame(string.Format(GetString("Warning.InvalidRpc"), __instance?.Data?.PlayerName));
            }
            return false;
        }
        return true;
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        var rpcType = (CustomRPC)callId;
        switch (rpcType)
        {
            case CustomRPC.AntiBlackout:
                if (Options.EndWhenPlayerBug.GetBool())
                {
                    Logger.Fatal($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): {reader.ReadString()} 错误，根据设定终止游戏", "Anti-black");
                    ChatUpdatePatch.DoBlockChat = true;
                    Main.OverrideWelcomeMsg = string.Format(GetString("RpcAntiBlackOutNotifyInLobby"), __instance?.Data?.PlayerName, GetString("EndWhenPlayerBug"));
                    _ = new LateTask(() =>
                    {
                        Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutEndGame"), __instance?.Data?.PlayerName), true);
                    }, 3f, "Anti-Black Msg SendInGame");
                    _ = new LateTask(() =>
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Error);
                        GameManager.Instance.LogicFlow.CheckEndCriteria();
                        RPC.ForceEndGame(CustomWinner.Error);
                    }, 5.5f, "Anti-Black End Game");
                }
                else if (GameStates.IsOnlineGame)
                {
                    Logger.Fatal($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): Change Role Setting Postfix 错误，根据设定继续游戏", "Anti-black");
                    _ = new LateTask(() =>
                    {
                        Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutIgnored"), __instance?.Data?.PlayerName), true);
                    }, 3f, "Anti-Black Msg SendInGame");
                }
                break;
            case CustomRPC.VersionCheck:
                try
                {
                    Version version = Version.Parse(reader.ReadString());
                    string tag = reader.ReadString();
                    string forkId = reader.ReadString();
                    Main.playerVersion[__instance.PlayerId] = new PlayerVersion(version, tag, forkId);

                    if (Main.VersionCheat.Value && __instance.PlayerId == 0) RPC.RpcVersionCheck();

                    if (Main.VersionCheat.Value && AmongUsClient.Instance.AmHost)
                        Main.playerVersion[__instance.PlayerId] = Main.playerVersion[0];

                    // Kick Unmached Player Start
                    if (AmongUsClient.Instance.AmHost)
                    {
                        if (!IsVersionMatch(__instance.PlayerId))
                        {
                            _ = new LateTask(() =>
                            {
                                if (__instance?.Data?.Disconnected is not null and not true)
                                {
                                    var msg = string.Format(GetString("KickBecauseDiffrentVersionOrMod"), __instance?.Data?.PlayerName);
                                    Logger.Warn(msg, "Version Kick");
                                    Logger.SendInGame(msg);
                                    AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), false);
                                }
                            }, 5f, "Kick");
                        }
                    }
                    // Kick Unmached Player End
                }
                catch
                {
                    Logger.Warn($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): バージョン情報が無効です", "RpcVersionCheck");
                    _ = new LateTask(() =>
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequestRetryVersionCheck, SendOption.Reliable, __instance.GetClientId());
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }, 1f, "Retry Version Check Task");
                }
                break;
            case CustomRPC.RequestRetryVersionCheck:
                RPC.RpcVersionCheck();
                break;
            case CustomRPC.SyncCustomSettings:
                if (AmongUsClient.Instance.AmHost) break;
                List<OptionItem> list = new();
                var startAmount = reader.ReadInt32();
                var lastAmount = reader.ReadInt32();
                for (var i = startAmount; i < OptionItem.AllOptions.Count && i <= lastAmount; i++)
                    list.Add(OptionItem.AllOptions[i]);
                Logger.Info($"{startAmount}-{lastAmount}:{list.Count}/{OptionItem.AllOptions.Count}", "SyncCustomSettings");
                foreach (var co in list) co.SetValue(reader.ReadInt32());
                OptionShower.GetText();
                break;
            case CustomRPC.SetDeathReason:
                RPC.GetDeathReason(reader);
                break;
            case CustomRPC.EndGame:
                RPC.EndGame(reader);
                break;
            case CustomRPC.PlaySound:
                byte playerID = reader.ReadByte();
                Sounds sound = (Sounds)reader.ReadByte();
                RPC.PlaySound(playerID, sound);
                break;
            case CustomRPC.ShowPopUp:
                string msg = reader.ReadString();
                HudManager.Instance.ShowPopUp(msg);
                break;
            case CustomRPC.SetCustomRole:
                byte CustomRoleTargetId = reader.ReadByte();
                CustomRoles role = (CustomRoles)reader.ReadPackedInt32();
                RPC.SetCustomRole(CustomRoleTargetId, role);
                break;
            case CustomRPC.SetBountyTarget:
                BountyHunter.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncPuppet:
                Puppeteer.ReceiveRPC(reader);
                break;
            case CustomRPC.SetKillOrSpell:
                Witch.ReceiveRPC(reader, false);
                break;
            case CustomRPC.SetKillOrHex:
                HexMaster.ReceiveRPC(reader, false);
                break;
            case CustomRPC.SetKillOrCurse:
                Occultist.ReceiveRPC(reader, false);
                break;

            case CustomRPC.SetSheriffShotLimit:
                Sheriff.ReceiveRPC(reader);
                break;
        /*    case CustomRPC.SetCopyCatMiscopyLimit:
                CopyCat.ReceiveRPC(reader);
                break; */
            case CustomRPC.SetDousedPlayer:
                byte ArsonistId = reader.ReadByte();
                byte DousedId = reader.ReadByte();
                bool doused = reader.ReadBoolean();
                Main.isDoused[(ArsonistId, DousedId)] = doused;
                break;
            case CustomRPC.setPlaguedPlayer:
                PlagueBearer.receiveRPC(reader);
                break;
            case CustomRPC.SetDrawPlayer:
                byte RevolutionistId = reader.ReadByte();
                byte DrawId = reader.ReadByte();
                bool drawed = reader.ReadBoolean();
                Main.isDraw[(RevolutionistId, DrawId)] = drawed;
                break;
            case CustomRPC.SetRevealedPlayer:
                byte FarseerId = reader.ReadByte();
                byte RevealId = reader.ReadByte();
                bool revealed = reader.ReadBoolean();
                Main.isRevealed[(FarseerId, RevealId)] = revealed;
                break;
            case CustomRPC.SetNameColorData:
                NameColorManager.ReceiveRPC(reader);
                break;
            case CustomRPC.DoSpell:
                Witch.ReceiveRPC(reader, true);
                break;
            case CustomRPC.DoHex:
                HexMaster.ReceiveRPC(reader, true);
                break;
            case CustomRPC.DoCurse:
                Occultist.ReceiveRPC(reader, true);
                break;
            case CustomRPC.SniperSync:
                Sniper.ReceiveRPC(reader);
                break;
            case CustomRPC.UndertakerLocationSync:
                Undertaker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetLoversPlayers:
                Main.LoversPlayers.Clear();
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    Main.LoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRPC.SetExecutionerTarget:
                Executioner.ReceiveRPC(reader, SetTarget: true);
                break;
            case CustomRPC.RemoveExecutionerTarget:
                Executioner.ReceiveRPC(reader, SetTarget: false);
                break;
            case CustomRPC.SetLawyerTarget:
                Lawyer.ReceiveRPC(reader, SetTarget: true);
                break;
            case CustomRPC.RemoveLawyerTarget:
                Lawyer.ReceiveRPC(reader, SetTarget: false);
                break;
            case CustomRPC.SendFireWorksState:
                FireWorks.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCurrentDousingTarget:
                byte arsonistId = reader.ReadByte();
                byte dousingTargetId = reader.ReadByte();
                if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
                    Main.currentDousingTarget = dousingTargetId;
                break;
            case CustomRPC.SetCurrentDrawTarget:
                byte arsonistId1 = reader.ReadByte();
                byte doTargetId = reader.ReadByte();
                if (PlayerControl.LocalPlayer.PlayerId == arsonistId1)
                    Main.currentDrawTarget = doTargetId;
                break;
            case CustomRPC.SetEvilTrackerTarget:
                EvilTracker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetRealKiller:
                byte targetId = reader.ReadByte();
                byte killerId = reader.ReadByte();
                RPC.SetRealKiller(targetId, killerId);
                break;
            case CustomRPC.SetGamerHealth:
                Gamer.ReceiveRPC(reader);
                break;
            case CustomRPC.SetPelicanEtenNum:
                Pelican.ReceiveRPC(reader);
                break;
            case CustomRPC.SetDoomsayerProgress:
                Doomsayer.ReceiveRPC(reader);
                break;
            case CustomRPC.SetTrackerTarget:
                Tracker.ReceiveRPC(reader);
                break;
            case CustomRPC.SwordsManKill:
                SwordsMan.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCounterfeiterSellLimit:
                Counterfeiter.ReceiveRPC(reader);
                break;
            case CustomRPC.SetJailerExeLimit:
                Jailer.ReceiveRPC(reader, setTarget: false);
                break;
            case CustomRPC.SetJailerTarget:
                Jailer.ReceiveRPC(reader, setTarget: true);
                break;
            case CustomRPC.SetPursuerSellLimit:
                Pursuer.ReceiveRPC(reader);
                break;
            case CustomRPC.SetMedicalerProtectLimit:
                Medic.ReceiveRPC(reader);
                break;
            case CustomRPC.SetGangsterRecruitLimit:
                Gangster.ReceiveRPC(reader);
                break;
            case CustomRPC.SetJackalRecruitLimit:
                Jackal.ReceiveRPC(reader);
                break;
            case CustomRPC.SetBanditStealLimit:
                Bandit.ReceiveRPC(reader);
                break;
            case CustomRPC.SetDoppelgangerStealLimit:
                Doppelganger.ReceiveRPC(reader);
                break;
            case CustomRPC.SetAdmireLimit:
                Admirer.ReceiveRPC(reader);
                break;
            case CustomRPC.SetRememberLimit:
                Amnesiac.ReceiveRPC(reader);
                break;
            case CustomRPC.SetImitateLimit:
                Imitator.ReceiveRPC(reader);
                break;
            case CustomRPC.PlayCustomSound:
                CustomSoundsManager.ReceiveRPC(reader);
                break;
            case CustomRPC.SetGhostPlayer:
                BallLightning.ReceiveRPC(reader);
                break;
            case CustomRPC.SetDarkHiderKillCount:
                DarkHide.ReceiveRPC(reader);
                break;
            case CustomRPC.SetGreedierOE:
                Greedier.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCursedWolfSpellCount:
                byte CursedWolfId = reader.ReadByte();
                int GuardNum = reader.ReadInt32();
                if (Main.CursedWolfSpellCount.ContainsKey(CursedWolfId))
                    Main.CursedWolfSpellCount[CursedWolfId] = GuardNum;
                else
                    Main.CursedWolfSpellCount.Add(CursedWolfId, Options.GuardSpellTimes.GetInt());
                break;
            case CustomRPC.SetJinxSpellCount:
                byte JinxId = reader.ReadByte();
                int JinxGuardNum = reader.ReadInt32();
                if (Main.JinxSpellCount.ContainsKey(JinxId))
                    Main.JinxSpellCount[JinxId] = JinxGuardNum;
                else
                    Main.JinxSpellCount.Add(JinxId, Jinx.JinxSpellTimes.GetInt());
                break;
            case CustomRPC.SetCollectorVotes:
                Collector.ReceiveRPC(reader);
                break;
            case CustomRPC.SetQuickShooterShotLimit:
                QuickShooter.ReceiveRPC(reader);
                break;
            case CustomRPC.RestTOHESetting:
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValueNoRpc(x.DefaultValue));
                OptionShower.GetText();
                break;
            case CustomRPC.SetEraseLimit:
                Eraser.ReceiveRPC(reader);
                break;
            case CustomRPC.GuessKill:
                GuessManager.RpcClientGuess(Utils.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRPC.SetMarkedPlayer:
                Assassin.ReceiveRPC(reader);
                break;
            case CustomRPC.SetMedicalerProtectList:
                Medic.ReceiveRPCForProtectList(reader);
                break;
            case CustomRPC.SetHackerHackLimit:
                Hacker.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncPsychicRedList:
                Psychic.ReceiveRPC(reader);
                break;
            case CustomRPC.SetKillTimer:
                float time = reader.ReadSingle();
                PlayerControl.LocalPlayer.SetKillTimer(time);
                break;
            case CustomRPC.SyncAllPlayerNames:
                Main.AllPlayerNames = new();
                int num = reader.ReadInt32();
                for (int i = 0; i < num; i++)
                    Main.AllPlayerNames.TryAdd(reader.ReadByte(), reader.ReadString());
                break;
            case CustomRPC.SyncCovenLeader:
                CovenLeader.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncNWitch:
                NWitch.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncShroud:
                Shroud.ReceiveRPC(reader);
                break;
            case CustomRPC.SetMorticianArrow:
                Mortician.ReceiveRPC(reader);
                break;
            case CustomRPC.SetTracefinderArrow:
                Tracefinder.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncNameNotify:
                NameNotifyManager.ReceiveRPC(reader);
                break;
            case CustomRPC.Judge:
                Judge.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.PresidentEnd:
                President.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.MeetingKill:
                Councillor.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.Guess:
                GuessManager.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.MafiaRevenge:
                MafiaRevengeManager.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.RetributionistRevenge:
                RetributionistRevengeManager.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.SetSwooperTimer:
                Swooper.ReceiveRPC(reader);
                break;
            case CustomRPC.SetWraithTimer:
                Wraith.ReceiveRPC(reader);
                break;
            case CustomRPC.SetShadeTimer:
                Shade.ReceiveRPC(reader);
                break;
            case CustomRPC.SetChameleonTimer:
                Chameleon.ReceiveRPC(reader);
                break;
            case CustomRPC.SetAlchemistTimer:
                Alchemist.ReceiveRPC(reader);
                break;
            case CustomRPC.SetBKTimer:
                BloodKnight.ReceiveRPC(reader);
                break;
            case CustomRPC.SetBansheeTimer:
                Banshee.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncTotocalcioTargetAndTimes:
                Totocalcio.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncRomanticTarget:
                Romantic.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncVengefulRomanticTarget:
                VengefulRomantic.ReceiveRPC(reader);
                break;
            case CustomRPC.SetSuccubusCharmLimit:
                Succubus.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCursedSoulCurseLimit:
                CursedSoul.ReceiveRPC(reader);
                break;
            case CustomRPC.SetMonarchKnightLimit:
                Monarch.ReceiveRPC(reader);
                break;
            case CustomRPC.SetEvilDiviner:
                EvilDiviner.ReceiveRPC(reader);
                break;
            case CustomRPC.SetPotionMaster:
                PotionMaster.ReceiveRPC(reader);
                break;
            case CustomRPC.SetDeputyHandcuffLimit:
                Deputy.ReceiveRPC(reader);
                break;
            case CustomRPC.SetInvestgatorLimit:
                Investigator.ReceiveRPC(reader);
                break;
            case CustomRPC.SetVirusInfectLimit:
                Virus.ReceiveRPC(reader);
                break;
            case CustomRPC.KillFlash:
                Utils.FlashColor(new(1f, 0f, 0f, 0.3f));
                if (Constants.ShouldPlaySfx()) RPC.PlaySound(PlayerControl.LocalPlayer.PlayerId, Sounds.KillSound);
                break;
            case CustomRPC.SetBloodhoundArrow:
                Bloodhound.ReceiveRPC(reader);
                break;
            case CustomRPC.SetVultureArrow:
                Vulture.ReceiveRPC(reader);
                break;
            case CustomRPC.SetSpiritcallerSpiritLimit:
                Spiritcaller.ReceiveRPC(reader);
                break;
            case CustomRPC.SetSeekerTarget:
                Seeker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetSeekerPoints:
                Seeker.ReceiveRPC(reader, setTarget: false);
                break;
            case CustomRPC.RpcPassBomb:
                Agitater.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCleanserCleanLimit:
                Cleanser.ReceiveRPC(reader);
                break;
            case CustomRPC.SetSoulCollectorLimit:
                SoulCollector.ReceiveRPC(reader);
                break;
            case CustomRPC.SetSwapperVotes:
                Swapper.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.SetPoliceLimlit:
                ChiefOfPolice.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncMiniAge:
                Mini.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncSabotageMasterSkill:
                SabotageMaster.ReceiveRPC(reader);
                break;
        }
    }

    private static bool IsVersionMatch(Byte PlayerId)
    {
        if (Main.VersionCheat.Value) return true;
        Version version = Main.playerVersion[PlayerId].version;
        string tag = Main.playerVersion[PlayerId].tag;
        string forkId = Main.playerVersion[PlayerId].forkId;
        
        if (version != Main.version
            || tag != $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})"
            || forkId != Main.ForkId)
            return false;

        return true;
    }
}

internal static class RPC
{
    //SyncCustomSettingsRPC Sender
    public static void SyncCustomSettingsRPC(int targetId = -1)
    {
        if (targetId != -1)
        {
            var client = Utils.GetClientById(targetId);
            if (client == null || client.Character == null || !Main.playerVersion.ContainsKey(client.Character.PlayerId)) return;
        }
        if (!AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls.Count <= 1 || (AmongUsClient.Instance.AmHost == false && PlayerControl.LocalPlayer == null)) return;
        var amount = OptionItem.AllOptions.Count;
        int divideBy = amount / 10;
        for (var i = 0; i <= 10; i++)
            SyncOptionsBetween(i * divideBy, (i + 1) * divideBy, targetId);
    }
    public static void SyncCustomSettingsRPCforOneOption(OptionItem option)
    {
        List<OptionItem> allOptions = new(OptionItem.AllOptions);
        var placement = allOptions.IndexOf(option);
        if (placement != -1)
            SyncOptionsBetween(placement, placement);
    }
    static void SyncOptionsBetween(int startAmount, int lastAmount, int targetId = -1)
    {
        if (targetId != -1)
        {
            var client = Utils.GetClientById(targetId);
            if (client == null || client.Character == null || !Main.playerVersion.ContainsKey(client.Character.PlayerId)) return;
        }
        if (!AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls.Count <= 1 || (AmongUsClient.Instance.AmHost == false && PlayerControl.LocalPlayer == null)) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 80, SendOption.Reliable, targetId);
        List<OptionItem> list = new();
        writer.Write(startAmount);
        writer.Write(lastAmount);
        for (var i = startAmount; i < OptionItem.AllOptions.Count && i <= lastAmount; i++)
            list.Add(OptionItem.AllOptions[i]);
        Logger.Info($"{startAmount}-{lastAmount}:{list.Count}/{OptionItem.AllOptions.Count}", "SyncCustomSettings");
        foreach (var co in list) writer.Write(co.GetValue());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void PlaySoundRPC(byte PlayerID, Sounds sound)
    {
        if (AmongUsClient.Instance.AmHost)
            PlaySound(PlayerID, sound);
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaySound, SendOption.Reliable, -1);
        writer.Write(PlayerID);
        writer.Write((byte)sound);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SyncAllPlayerNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncAllPlayerNames, SendOption.Reliable, -1);
        writer.Write(Main.AllPlayerNames.Count);
        foreach (var name in Main.AllPlayerNames)
        {
            writer.Write(name.Key);
            writer.Write(name.Value);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ShowPopUp(this PlayerControl pc, string msg)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShowPopUp, SendOption.Reliable, pc.GetClientId());
        writer.Write(msg);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ExileAsync(PlayerControl player)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.Reliable, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        player.Exiled();
    }
    public static async void RpcVersionCheck()
    {
        while (PlayerControl.LocalPlayer == null) await Task.Delay(500);
        if (Main.playerVersion.ContainsKey(0) || !Main.VersionCheat.Value)
        {
            bool cheating = Main.VersionCheat.Value;
            MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionCheck, SendOption.Reliable);
            writer.Write(cheating ? Main.playerVersion[0].version.ToString() : Main.PluginVersion);
            writer.Write(cheating ? Main.playerVersion[0].tag : $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})");
            writer.Write(cheating ? Main.playerVersion[0].forkId : Main.ForkId);
            writer.EndMessage();
        }
        Main.playerVersion[PlayerControl.LocalPlayer.PlayerId] = new PlayerVersion(Main.PluginVersion, $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})", Main.ForkId);
    }
    public static void SendDeathReason(byte playerId, PlayerState.DeathReason deathReason)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDeathReason, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write((int)deathReason);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void GetDeathReason(MessageReader reader)
    {
        var playerId = reader.ReadByte();
        var deathReason = (PlayerState.DeathReason)reader.ReadInt32();
        Main.PlayerStates[playerId].deathReason = deathReason;
        Main.PlayerStates[playerId].IsDead = true;
    }
    public static void ForceEndGame(CustomWinner win)
    {
        if (ShipStatus.Instance == null) return;
        try { CustomWinnerHolder.ResetAndSetWinner(win); }
        catch { }
        if (AmongUsClient.Instance.AmHost)
        {
            ShipStatus.Instance.enabled = false;
            try { GameManager.Instance.LogicFlow.CheckEndCriteria(); }
            catch { }
            try { GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false); }
            catch { }
        }
    }
    public static void EndGame(MessageReader reader)
    {
        try
        {
            CustomWinnerHolder.ReadFrom(reader);
        }
        catch (Exception ex)
        {
            Logger.Error($"正常にEndGameを行えませんでした。\n{ex}", "EndGame", false);
        }
    }
    public static void PlaySound(byte playerID, Sounds sound)
    {
        if (PlayerControl.LocalPlayer.PlayerId == playerID)
        {
            switch (sound)
            {
                case Sounds.KillSound:
                    SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.KillSfx, false, 1f);
                    break;
                case Sounds.TaskComplete:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskCompleteSound, false, 1f);
                    break;
                case Sounds.TaskUpdateSound:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskUpdateSound, false, 1f);
                    break;
                case Sounds.ImpTransform:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HnSImpostorScreamSfx>.Instance.HnSOtherImpostorTransformSfx, false, 0.8f);
                    break;
            }
        }
    }
    public static void SetCustomRole(byte targetId, CustomRoles role)
    {
        if (role < CustomRoles.NotAssigned)
        {
            Main.PlayerStates[targetId].SetMainRole(role);
        }
        else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
        {
            Main.PlayerStates[targetId].SetSubRole(role);
        }
        switch (role)
        {
            case CustomRoles.BountyHunter:
                BountyHunter.Add(targetId);
                break;
            case CustomRoles.SerialKiller:
                SerialKiller.Add(targetId);
                break;
            case CustomRoles.FireWorks:
                FireWorks.Add(targetId);
                break;
            case CustomRoles.TimeThief:
                TimeThief.Add(targetId);
                break;
            case CustomRoles.Puppeteer:
                Puppeteer.Add(targetId);
                break;
            case CustomRoles.Mastermind:
                Mastermind.Add(targetId);
                break;
            case CustomRoles.Sniper:
                Sniper.Add(targetId);
                break;
            case CustomRoles.Undertaker:
                Undertaker.Add(targetId);
                break;
            case CustomRoles.Crusader:
                Crusader.Add(targetId);
                break;
        /*    case CustomRoles.Mare:
                Mare.Add(targetId);
                break; */
            case CustomRoles.EvilTracker:
                EvilTracker.Add(targetId);
                break;
            case CustomRoles.Witch:
                Witch.Add(targetId);
                break;
            case CustomRoles.Vampire:
                Vampire.Add(targetId);
                break;
            case CustomRoles.Vampiress:
                Vampiress.Add(targetId);
                break;
            case CustomRoles.Executioner:
                Executioner.Add(targetId);
                break;
            case CustomRoles.Farseer:
                Farseer.Add(targetId);
                break;
            case CustomRoles.Lawyer:
                Lawyer.Add(targetId);
                break;
            case CustomRoles.HexMaster:
                HexMaster.Add(targetId);
                break;
            case CustomRoles.Occultist:
                Occultist.Add(targetId);
                break;
            case CustomRoles.Camouflager:
                Camouflager.Add(targetId);
                break;
            case CustomRoles.Jackal:
                Jackal.Add(targetId);
                break;
            case CustomRoles.Sidekick:
                Sidekick.Add(targetId);
                break;
            case CustomRoles.Bandit:
                Bandit.Add(targetId);
                break;
            case CustomRoles.Doppelganger:
                Doppelganger.Add(targetId);
                break;
            case CustomRoles.Poisoner:
                Poisoner.Add(targetId);
                break;
            case CustomRoles.Sheriff:
                Sheriff.Add(targetId);
                break;
            case CustomRoles.CopyCat:
                CopyCat.Add(targetId);
                break;
            case CustomRoles.Pickpocket:
                Pickpocket.Add(targetId);
                break;
            case CustomRoles.Cleanser:
                Cleanser.Add(targetId);
                break;
            case CustomRoles.SoulCollector:
                SoulCollector.Add(targetId);
                break;
            case CustomRoles.Agitater:
                Agitater.Add(targetId);
                break;
            case CustomRoles.QuickShooter:
                QuickShooter.Add(targetId);
                break;
            case CustomRoles.SwordsMan:
                SwordsMan.Add(targetId);
                break;
            case CustomRoles.SabotageMaster:
                SabotageMaster.Add(targetId);
                break;
            case CustomRoles.Repairman:
                Repairman.Add(targetId);
                break;
            case CustomRoles.Snitch:
                Snitch.Add(targetId);
                break;
            case CustomRoles.Chronomancer:
                Chronomancer.Add(targetId);
                break;
            case CustomRoles.Medusa:
                Medusa.Add(targetId);
                break;
            case CustomRoles.Necromancer:
                Necromancer.Add(targetId);
                break;
            case CustomRoles.Marshall:
                Marshall.Add(targetId);
                break;
            case CustomRoles.AntiAdminer:
                AntiAdminer.Add(targetId);
                break;
            case CustomRoles.Monitor:
                Monitor.Add(targetId);
                break;
            case CustomRoles.LastImpostor:
                LastImpostor.Add(targetId);
                break;
            case CustomRoles.Aware:
                Main.AwareInteracted[targetId] = new();
                break;
            case CustomRoles.TimeManager:
                TimeManager.Add(targetId);
                break;
            case CustomRoles.Workhorse:
                Workhorse.Add(targetId);
                break;
            case CustomRoles.Pelican:
                Pelican.Add(targetId);
                break;
            case CustomRoles.Glitch:
                Glitch.Add(targetId);
                break;
            case CustomRoles.Counterfeiter:
                Counterfeiter.Add(targetId);
                break;
            case CustomRoles.Jailer:
                Jailer.Add(targetId);
                break;
            case CustomRoles.Pursuer:
                Pursuer.Add(targetId);
                break;
            case CustomRoles.Gangster:
                Gangster.Add(targetId);
                break;
            case CustomRoles.EvilDiviner:
                EvilDiviner.Add(targetId);
                break;
            case CustomRoles.PotionMaster:
                PotionMaster.Add(targetId);
                break;
            case CustomRoles.Medic:
                Medic.Add(targetId);
                break;
            case CustomRoles.Divinator:
                Divinator.Add(targetId);
                break;
            case CustomRoles.Oracle:
                Oracle.Add(targetId);
                break;
            case CustomRoles.Gamer:
                Gamer.Add(targetId);
                break;
            case CustomRoles.BallLightning:
                BallLightning.Add(targetId);
                break;
            case CustomRoles.DarkHide:
                DarkHide.Add(targetId);
                break;
            case CustomRoles.Greedier:
                Greedier.Add(targetId);
                break;
            case CustomRoles.Collector:
                Collector.Add(targetId);
                break;
            case CustomRoles.CursedWolf:
                Main.CursedWolfSpellCount[targetId] = Options.GuardSpellTimes.GetInt();
                break;
            case CustomRoles.Jinx:
                Main.JinxSpellCount[targetId] = Jinx.JinxSpellTimes.GetInt();
                Jinx.Add(targetId);
                break;
            case CustomRoles.Eraser:
                Eraser.Add(targetId);
                break;
            case CustomRoles.Assassin:
                Assassin.Add(targetId);
                break;
            case CustomRoles.Sans:
                Sans.Add(targetId);
                break;
            case CustomRoles.Juggernaut:
                Juggernaut.Add(targetId);
                break;
            case CustomRoles.Reverie:
                Reverie.Add(targetId);
                break;
            case CustomRoles.Hacker:
                Hacker.Add(targetId);
                break;
            case CustomRoles.Psychic:
                Psychic.Add(targetId);
                break;
            case CustomRoles.Hangman:
                Hangman.Add(targetId);
                break;
            case CustomRoles.Judge:
                Judge.Add(targetId);
                break;
            case CustomRoles.President:
                President.Add(targetId);
                break;
            case CustomRoles.ParityCop:
                ParityCop.Add(targetId);
                break;
            case CustomRoles.Councillor:
                Councillor.Add(targetId);
                break;
            case CustomRoles.Mortician:
                Mortician.Add(targetId);
                break;
            case CustomRoles.Tracefinder:
                Tracefinder.Add(targetId);
                break;
            case CustomRoles.Mediumshiper:
                Mediumshiper.Add(targetId);
                break;
            case CustomRoles.Veteran:
                Main.VeteranNumOfUsed.Add(targetId, Options.VeteranSkillMaxOfUseage.GetInt());
                break;
            case CustomRoles.Grenadier:
                Main.GrenadierNumOfUsed.Add(targetId, Options.GrenadierSkillMaxOfUseage.GetInt());
                break;
            case CustomRoles.Lighter:
                Main.LighterNumOfUsed.Add(targetId, Options.LighterSkillMaxOfUseage.GetInt());
                break;
            case CustomRoles.Bastion:
                Main.BastionNumberOfAbilityUses = Options.BastionMaxBombs.GetInt();
                break;
            case CustomRoles.TimeMaster:
                Main.TimeMasterNumOfUsed.Add(targetId, Options.TimeMasterMaxUses.GetInt());
                break;
            case CustomRoles.Swooper:
                Swooper.Add(targetId);
                break;
            case CustomRoles.Wraith:
                Wraith.Add(targetId);
                break;
            case CustomRoles.Shade:
                Shade.Add(targetId);
                break;
            case CustomRoles.Chameleon:
                Chameleon.Add(targetId);
                break;
            case CustomRoles.BloodKnight:
                BloodKnight.Add(targetId);
                break;
            case CustomRoles.Alchemist:
                Alchemist.Add(targetId);
                break;
            case CustomRoles.Banshee:
                Banshee.Add(targetId);
                break;
            case CustomRoles.Totocalcio:
                Totocalcio.Add(targetId);
                break;
            case CustomRoles.Romantic:
                Romantic.Add(targetId);
                break;
            case CustomRoles.VengefulRomantic:
                VengefulRomantic.Add(targetId);
                break;
            case CustomRoles.RuthlessRomantic:
                RuthlessRomantic.Add(targetId);
                break;
            case CustomRoles.Succubus:
                Succubus.Add(targetId);
                break;
            case CustomRoles.CursedSoul:
                CursedSoul.Add(targetId);
                break;
            case CustomRoles.Admirer:
                Admirer.Add(targetId);
                break;
            case CustomRoles.Amnesiac:
                Amnesiac.Add(targetId);
                break;
            case CustomRoles.Imitator:
                Imitator.Add(targetId);
                break;
            case CustomRoles.DovesOfNeace:
                Main.DovesOfNeaceNumOfUsed.Add(targetId, Options.DovesOfNeaceMaxOfUseage.GetInt());
                break;
            case CustomRoles.Infectious:
                Infectious.Add(targetId);
                break;
            case CustomRoles.Monarch:
                Monarch.Add(targetId);
                break;
            case CustomRoles.Deputy:
                Deputy.Add(targetId);
                break;
            case CustomRoles.Investigator:
                Investigator.Add(targetId);
                break;
            case CustomRoles.Virus:
                Virus.Add(targetId);
                break;
            case CustomRoles.Bloodhound:
                Bloodhound.Add(targetId); 
                break;
            case CustomRoles.Vulture:
                Vulture.Add(targetId); 
                break;
            case CustomRoles.PlagueBearer:
                PlagueBearer.Add(targetId);
                break;
            case CustomRoles.Tracker:
                Tracker.Add(targetId);
                break;
            case CustomRoles.Merchant:
                Merchant.Add(targetId);
                break;
            case CustomRoles.NSerialKiller:
                NSerialKiller.Add(targetId);
                break;
            case CustomRoles.Pyromaniac:
                Pyromaniac.Add(targetId);
                break;
            case CustomRoles.Werewolf:
                Werewolf.Add(targetId);
                break;
            case CustomRoles.Traitor:
                Traitor.Add(targetId);
                break;
            case CustomRoles.Huntsman:
                Huntsman.Add(targetId);
                break;
            case CustomRoles.NWitch:
                NWitch.Add(targetId);
                break;
            case CustomRoles.CovenLeader:
                CovenLeader.Add(targetId);
                break;
            case CustomRoles.Shroud:
                Shroud.Add(targetId);
                break;
            case CustomRoles.Maverick:
                Maverick.Add(targetId);
                break;
            case CustomRoles.Dazzler:
                Dazzler.Add(targetId);
                break;
            case CustomRoles.Addict:
                Addict.Add(targetId);
                break;
            case CustomRoles.Deathpact:
                Deathpact.Add(targetId);
                break;
            case CustomRoles.Wildling:
                Wildling.Add(targetId);
                break;
            case CustomRoles.Morphling:
                Morphling.Add(targetId);
                break;
            case CustomRoles.Devourer:
                Devourer.Add(targetId);
                break;
            case CustomRoles.Spiritualist:
                Spiritualist.Add(targetId);
                break;
            case CustomRoles.Spiritcaller:
                Spiritcaller.Add(targetId);
                break;
            case CustomRoles.Lurker:
                Lurker.Add(targetId);
                break;
            case CustomRoles.Doomsayer:
                Doomsayer.Add(targetId);
                break;
            case CustomRoles.Pirate:
                Pirate.Add(targetId);
                break;
            case CustomRoles.Seeker:
                Seeker.Add(targetId);
                break;
            case CustomRoles.Pitfall:
                Pitfall.Add(targetId);
                break;
            case CustomRoles.Swapper: 
                Swapper.Add(targetId);
                break;
            case CustomRoles.ChiefOfPolice:
                ChiefOfPolice.Add(targetId);
                break;
            case CustomRoles.NiceMini:
                Mini.Add(targetId);
                break;
            case CustomRoles.EvilMini:
                Mini.Add(targetId);
                break;
            case CustomRoles.Blackmailer:
                Blackmailer.Add(targetId);
                break;
            case CustomRoles.Spy:
                Spy.Add(targetId);
                break;
        }
        HudManager.Instance.SetHudActive(true);
    //    HudManager.Instance.Chat.SetVisible(true);
        if (PlayerControl.LocalPlayer.PlayerId == targetId) RemoveDisableDevicesPatch.UpdateDisableDevices();
    }
    public static void RpcDoSpell(byte targetId, byte killerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoSpell, SendOption.Reliable, -1);
        writer.Write(targetId);
        writer.Write(killerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SyncLoversPlayers()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetLoversPlayers, SendOption.Reliable, -1);
        writer.Write(Main.LoversPlayers.Count);
        foreach (var lp in Main.LoversPlayers)
        {
            writer.Write(lp.PlayerId);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendRpcLogger(uint targetNetId, byte callId, int targetClientId = -1)
    {
        if (!DebugModeManager.AmDebugger) return;
        string rpcName = GetRpcName(callId);
        string from = targetNetId.ToString();
        string target = targetClientId.ToString();
        try
        {
            target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId).PlayerName;
            from = Main.AllPlayerControls.Where(c => c.NetId == targetNetId).FirstOrDefault()?.Data?.PlayerName;
        }
        catch { }
        Logger.Info($"FromNetID:{targetNetId}({from}) TargetClientID:{targetClientId}({target}) CallID:{callId}({rpcName})", "SendRPC");
    }
    public static string GetRpcName(byte callId)
    {
        string rpcName;
        if ((rpcName = Enum.GetName(typeof(RpcCalls), callId)) != null) { }
        else if ((rpcName = Enum.GetName(typeof(CustomRPC), callId)) != null) { }
        else rpcName = callId.ToString();
        return rpcName;
    }
    public static void SetCurrentDousingTarget(byte arsonistId, byte targetId)
    {
        if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
        {
            Main.currentDousingTarget = targetId;
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCurrentDousingTarget, SendOption.Reliable, -1);
            writer.Write(arsonistId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void SetCurrentDrawTarget(byte arsonistId, byte targetId)
    {
        if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
        {
            Main.currentDrawTarget = targetId;
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCurrentDrawTarget, SendOption.Reliable, -1);
            writer.Write(arsonistId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void SetCurrentRevealTarget(byte arsonistId, byte targetId)
    {
        if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
        {
            Main.currentDrawTarget = targetId;
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCurrentRevealTarget, SendOption.Reliable, -1);
            writer.Write(arsonistId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void SendRPCCursedWolfSpellCount(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCursedWolfSpellCount, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(Main.CursedWolfSpellCount[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendRPCJinxSpellCount(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetJinxSpellCount, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(Main.JinxSpellCount[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ResetCurrentDousingTarget(byte arsonistId) => SetCurrentDousingTarget(arsonistId, 255);
    public static void ResetCurrentDrawTarget(byte arsonistId) => SetCurrentDrawTarget(arsonistId, 255);
    public static void ResetCurrentRevealTarget(byte arsonistId) => SetCurrentRevealTarget(arsonistId, 255);
    public static void SetRealKiller(byte targetId, byte killerId)
    {
        var state = Main.PlayerStates[targetId];
        state.RealKiller.Item1 = DateTime.Now;
        state.RealKiller.Item2 = killerId;

        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRealKiller, SendOption.Reliable, -1);
        writer.Write(targetId);
        writer.Write(killerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpc))]
internal class StartRpcPatch
{
    public static void Prefix(InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId)
    {
        RPC.SendRpcLogger(targetNetId, callId);
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpcImmediately))]
internal class StartRpcImmediatelyPatch
{
    public static void Prefix(InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId, [HarmonyArgument(3)] int targetClientId = -1)
    {
        RPC.SendRpcLogger(targetNetId, callId, targetClientId);
    }
}

