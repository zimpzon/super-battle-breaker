using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class Playfab
{
    public static string DisplayStatus = "not logged in";
    public static LoginResult LoginRes;
    static bool LoginComplete => LoginRes is not null;

    public const string SendStatsEvent = "send_stats_event";

    public const string GameTimeAccumulated = "game_time_accumulated";
    public const string ArenaLevel = "arena_level";

    const string Version = "1.0";

    static string GetOrCreatePlayerId()
    {
        const string Key = "super-battle-breaker-v1/playerId";
        string result = PlayerPrefs.GetString(Key);
        if (string.IsNullOrWhiteSpace(result))
        {
            result = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(Key, result);
        }
        return result;
    }

    public static void Login()
    {
        PlayFabSettings.TitleId = "3F45A"; // Chuck

        var req = new LoginWithCustomIDRequest
        {
            CreateAccount = true,
            CustomId = GetOrCreatePlayerId(),
            TitleId = PlayFabSettings.TitleId,
        };

        void Callback(LoginResult result)
        {
            LoginRes = result;
            DisplayStatus = "logged in";

            Debug.Log($"login successful, id: {result.PlayFabId}, created: {result.NewlyCreated}");
            Debug.Log($"Sending platform info ({Application.platform})");
            SendLoginInfo();
        }

        void ErrorCallback(PlayFabError result)
        {
            DisplayStatus = "error logging in";
            Debug.LogError($"login error: {result}");
        }

        PlayFabClientAPI.LoginWithCustomID(req, Callback, ErrorCallback);
    }

    static void SendLoginInfo()
    {
        var data = new Dictionary<string, string>
        {
            { "Platform|DeviceModel|OS", $"{Application.platform} | {SystemInfo.deviceModel} | {SystemInfo.operatingSystem}" },
            { "game_version", "V1.2" },
            { "super-battle-breaker",  "Super Battle Breaker" },
            { "hosting_info", JsMappings.GetHostingInfo() },
        };

        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = data,
            Permission = UserDataPermission.Public
        },
        result => Debug.Log("Platform info sent to PlayFab"),
        error => Debug.LogError("Failed to send platform info: " + error.GenerateErrorReport()));
    }

    public static void PlayerEvent(string eventName, Dictionary<string, object> properties)
    {
        if (!LoginComplete)
        {
            Debug.Log("Cannot send event, login not complete.");
            return;
        }

        var req = new WriteClientPlayerEventRequest
        {
            Body = properties,
            CustomTags = new Dictionary<string, string> { { "version", Version } },
            EventName = eventName,
            AuthenticationContext = new PlayFabAuthenticationContext
            {
                ClientSessionTicket = LoginRes.SessionTicket,
                PlayFabId = LoginRes.PlayFabId,
            }
        };

        PlayFabClientAPI.WritePlayerEvent(req,
            res => Debug.Log($"event {eventName} sent"),
            err => Debug.LogError($"error sending event {eventName}: {err}"));
    }

    public static void PlayerStat(Dictionary<string, int> stats)
    {
        if (!LoginComplete)
        {
            Debug.Log("Cannot send player stats, login not complete.");
            return;
        }

        if (string.IsNullOrEmpty(LoginRes?.SessionTicket))
        {
            Debug.Log("No valid session ticket found. Re-logging in...");
            LoginAndSendStats(stats);
            return;
        }

        SendStats(stats, retryOnAuthError: true);
    }

    private static void SendStats(Dictionary<string, int> stats, bool retryOnAuthError)
    {
        var req = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>(),
            AuthenticationContext = new PlayFabAuthenticationContext
            {
                ClientSessionTicket = LoginRes.SessionTicket,
                PlayFabId = LoginRes.PlayFabId,
            }
        };

        foreach (var pair in stats)
        {
            req.Statistics.Add(new StatisticUpdate { StatisticName = pair.Key, Value = pair.Value });
        }

        PlayFabClientAPI.UpdatePlayerStatistics(req,
            res => Debug.Log($"stats sent successfully"),
            err =>
            {
                // Avoid serializing dictionary in error message for WebGL compatibility
                Debug.LogError($"error sending stats: {err}");

                if (retryOnAuthError)
                {
                    Debug.Log("Stats error detected, re-logging in...");
                    LoginAndSendStats(stats);
                }
            });
    }

    private static void LoginAndSendStats(Dictionary<string, int> statsToSend)
    {
        var req = new LoginWithCustomIDRequest
        {
            CreateAccount = true,
            CustomId = GetOrCreatePlayerId(),
            TitleId = PlayFabSettings.TitleId,
        };

        PlayFabClientAPI.LoginWithCustomID(req,
            result =>
            {
                LoginRes = result;
                Debug.Log("Re-login successful, sending stats...");
                SendStats(statsToSend, retryOnAuthError: false); // avoid infinite loop
            },
            error =>
            {
                Debug.LogError("Re-login failed: " + error.GenerateErrorReport());
            });
    }

    public static void PlayerStat(string statName, int value)
    {
        PlayerStat(new Dictionary<string, int> { { statName, value } });
    }
}
