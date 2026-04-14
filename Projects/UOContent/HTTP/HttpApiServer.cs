/*************************************************************************
 * ModernUO HTTP API Module                                               *
 * Copyright 2026 - UO Commander Integration                              *
 *                                                                       *
 * Lightweight HTTP server for remote administration via UO Commander    *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Server.Accounting;
using Server.Commands;
using Server.Items;
using Server.Misc;
using Server.Network;

namespace Server.HTTP;

public static class HttpApiServer
{
    private static HttpListener? _listener;
    private static bool _running;
    
    // Configuration
    public static bool Enabled { get; private set; }
    public static int Port { get; private set; } = 8080;
    public static string JwtSecret { get; private set; } = "";
    public static int JwtExpiryHours { get; private set; } = 24;
    
    public static void Configure()
    {
        Enabled = ServerConfiguration.GetSetting("httpApi.enabled", false);
        
        if (!Enabled)
        {
            return;
        }
        
        Port = ServerConfiguration.GetSetting("httpApi.port", 8080);
        JwtSecret = ServerConfiguration.GetSetting("httpApi.jwtSecret", Guid.NewGuid().ToString());
        JwtExpiryHours = ServerConfiguration.GetSetting("httpApi.jwtExpiryHours", 24);
        
        if (string.IsNullOrWhiteSpace(JwtSecret) || JwtSecret == Guid.Empty.ToString())
        {
            JwtSecret = Guid.NewGuid().ToString();
            ServerConfiguration.Set("httpApi.jwtSecret", JwtSecret);
        }
    }
    
    public static async Task Start()
    {
        if (!Enabled)
        {
            return;
        }
        
        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://+:{Port}/");
            _listener.Start();
            _running = true;
            
            Console.WriteLine($"[HTTP API] Server started on port {Port}");
            
            _ = Task.Run(HandleRequests);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HTTP API] Failed to start: {ex.Message}");
        }
    }
    
    public static void Stop()
    {
        _running = false;
        _listener?.Stop();
        _listener?.Close();
    }
    
    private static async Task HandleRequests()
    {
        if (_listener == null) return;
        
        while (_running)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => ProcessRequest(context));
            }
            catch
            {
                // Listener closed or error
                break;
            }
        }
    }
    
    private static async Task ProcessRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;
            
            // CORS headers
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
            
            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }
            
            // Skip authentication for login endpoint
            var path = request.Url?.AbsolutePath ?? "/";
            if (path.StartsWith("/api/auth/") && path.Contains("/login"))
            {
                await HandleAuthLogin(request, response);
                return;
            }
            
            // Verify JWT token for all other endpoints
            var authHeader = request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                await SendJsonResponse(response, 401, new { error = "Unauthorized" });
                return;
            }
            
            var token = authHeader.Substring(7);
            if (!JwtHelper.ValidateToken(token, JwtSecret, out var username))
            {
                await SendJsonResponse(response, 401, new { error = "Invalid or expired token" });
                return;
            }
            
            // Verify user still has required access level
            if (Accounts.GetInstance(username) is not Account account || 
                account.AccessLevel < AccessLevel.GameMaster)
            {
                await SendJsonResponse(response, 403, new { error = "Insufficient privileges" });
                return;
            }
            
            // Route to handler
            await RouteRequest(path, request, response, username);
        }
        catch (Exception ex)
        {
            await SendJsonResponse(context.Response, 500, new { error = ex.Message });
        }
    }
    
    private static async Task RouteRequest(string path, HttpListenerRequest request, 
        HttpListenerResponse response, string username)
    {
        switch (path)
        {
            // Auth
            case "/api/auth/verify":
                await SendJsonResponse(response, 200, new { valid = true, username });
                break;
            case "/api/auth/logout":
                await SendJsonResponse(response, 200, new { message = "Logged out" });
                break;
            
            // Server Control
            case "/api/server/status":
                await HandleServerStatus(request, response);
                break;
            case "/api/server/save":
                await HandleServerSave(request, response, username);
                break;
            case var p when p.StartsWith("/api/server/shutdown"):
                await HandleServerShutdown(request, response, username);
                break;
            case var p when p.StartsWith("/api/server/restart"):
                await HandleServerRestart(request, response, username);
                break;
            case "/api/server/broadcast":
                await HandleBroadcast(request, response, username);
                break;
            case "/api/server/staff-message":
                await HandleStaffMessage(request, response, username);
                break;
            
            // Players
            case "/api/players":
                await HandleGetPlayers(request, response);
                break;
            case var p when p.StartsWith("/api/players/search"):
                await HandleSearchPlayers(request, response);
                break;
            case var p when p.StartsWith("/api/players/") && p.EndsWith("/equipment"):
                await HandleGetEquipment(request, response, ExtractSerial(p));
                break;
            case var p when p.StartsWith("/api/players/") && p.EndsWith("/backpack"):
                await HandleGetBackpack(request, response, ExtractSerial(p));
                break;
            case var p when p.StartsWith("/api/players/") && p.EndsWith("/skills"):
                await HandleGetSkills(request, response, ExtractSerial(p));
                break;
            case var p when p.StartsWith("/api/players/") && p.EndsWith("/properties"):
                await HandleGetProperties(request, response, ExtractSerial(p));
                break;
            case var p when p.StartsWith("/api/players/") && p.EndsWith("/kick"):
                await HandleKickPlayer(request, response, username, ExtractSerial(p));
                break;
            case var p when p.StartsWith("/api/players/") && p.EndsWith("/ban"):
                await HandleBanPlayer(request, response, username, ExtractSerial(p));
                break;
            case var p when p.StartsWith("/api/players/") && p.EndsWith("/unban"):
                await HandleUnbanPlayer(request, response, username, ExtractSerial(p));
                break;
            
            // Accounts
            case "/api/accounts/search":
                await HandleSearchAccounts(request, response);
                break;
            case var p when p.StartsWith("/api/accounts/") && p.EndsWith("/ban"):
                await HandleBanAccount(request, response, username, ExtractAccountName(p));
                break;
            case var p when p.StartsWith("/api/accounts/") && p.EndsWith("/unban"):
                await HandleUnbanAccount(request, response, username, ExtractAccountName(p));
                break;
            
            // Firewall
            case "/api/firewall":
                if (request.HttpMethod == "GET")
                    await HandleGetFirewallRules(request, response);
                else if (request.HttpMethod == "POST")
                    await HandleAddFirewallRule(request, response, username);
                else if (request.HttpMethod == "DELETE")
                    await HandleRemoveFirewallRule(request, response, username);
                break;
            
            // Logs
            case "/api/logs":
                await HandleGetLogs(request, response);
                break;
            
            // Server Lockdown
            case "/api/server/lockdown":
                if (request.HttpMethod == "POST")
                    await HandleSetLockdown(request, response, username);
                else if (request.HttpMethod == "DELETE")
                    await HandleDisableLockdown(request, response, username);
                break;
            
            default:
                await SendJsonResponse(response, 404, new { error = "Endpoint not found" });
                break;
        }
    }
    
    #region Authentication
    
    private static async Task HandleAuthLogin(HttpListenerRequest request, HttpListenerResponse response)
    {
        if (request.HttpMethod != "POST")
        {
            await SendJsonResponse(response, 405, new { error = "Method not allowed" });
            return;
        }
        
        using var reader = new System.IO.StreamReader(request.InputStream);
        var body = await reader.ReadToEndAsync();
        var json = JsonSerializer.Deserialize<LoginRequest>(body);
        
        if (json == null || string.IsNullOrEmpty(json.Username) || string.IsNullOrEmpty(json.Password))
        {
            await SendJsonResponse(response, 400, new { error = "Username and password required" });
            return;
        }
        
        if (Accounts.GetInstance(json.Username) is not Account account)
        {
            await SendJsonResponse(response, 401, new { error = "Invalid credentials" });
            return;
        }
        
        if (!account.CheckPassword(json.Password))
        {
            await SendJsonResponse(response, 401, new { error = "Invalid credentials" });
            return;
        }
        
        if (account.AccessLevel < AccessLevel.GameMaster)
        {
            await SendJsonResponse(response, 403, new { error = "Insufficient privileges" });
            return;
        }
        
        if (account.Banned)
        {
            await SendJsonResponse(response, 403, new { error = "Account is banned" });
            return;
        }
        
        var token = JwtHelper.GenerateToken(json.Username, account.AccessLevel, JwtSecret, JwtExpiryHours);
        
        await SendJsonResponse(response, 200, new
        {
            token,
            username = account.Username,
            accessLevel = (int)account.AccessLevel,
            expiresHours = JwtExpiryHours
        });
    }
    
    #endregion
    
    #region Server Control
    
    private static async Task HandleServerStatus(HttpListenerRequest request, HttpListenerResponse response)
    {
        var status = new
        {
            isRunning = Core.Running,
            uptime = (DateTime.UtcNow - Core.CreationTime).TotalSeconds,
            playerCount = NetState.Instances.Count(ns => ns.Mobile != null),
            maxPlayers = NetState.MaxConnections,
            memoryUsage = GC.GetGCMemoryInfo().HeapSizeBytes,
            cpuUsage = 0.0, // Would need performance counters
            worldSaveStatus = World.SaveThread?.ThreadState.ToString() ?? "Idle",
            lastSaveTime = World.LastSave,
            version = GitInfo.Version,
            lockdownLevel = AccountHandler.LockdownLevel?.ToString() ?? "None"
        };
        
        await SendJsonResponse(response, 200, status);
    }
    
    private static async Task HandleServerSave(HttpListenerRequest request, HttpListenerResponse response, string username)
    {
        if (request.HttpMethod != "POST")
        {
            await SendJsonResponse(response, 405, new { error = "Method not allowed" });
            return;
        }
        
        // Execute on game thread
        await EventLoopContext.ExecuteOnGameThread(() =>
        {
            World.Save();
        });
        
        Console.WriteLine($"[HTTP API] World saved by {username}");
        await SendJsonResponse(response, 200, new { message = "World save completed" });
    }
    
    private static async Task HandleServerShutdown(HttpListenerRequest request, HttpListenerResponse response, string username)
    {
        if (request.HttpMethod != "POST")
        {
            await SendJsonResponse(response, 405, new { error = "Method not allowed" });
            return;
        }
        
        var save = request.QueryString["save"] != "false";
        
        Console.WriteLine($"[HTTP API] Shutdown initiated by {username} (save={save})");
        
        await EventLoopContext.ExecuteOnGameThread(() =>
        {
            if (save)
            {
                World.Save();
            }
            NetState.Shutdown();
            Core.Kill(false);
        });
        
        await SendJsonResponse(response, 200, new { message = "Server shutting down" });
    }
    
    private static async Task HandleServerRestart(HttpListenerRequest request, HttpListenerResponse response, string username)
    {
        if (request.HttpMethod != "POST")
        {
            await SendJsonResponse(response, 405, new { error = "Method not allowed" });
            return;
        }
        
        var save = request.QueryString["save"] != "false";
        var delay = int.TryParse(request.QueryString["delay"], out var d) ? d : 60;
        
        Console.WriteLine($"[HTTP API] Restart initiated by {username} (save={save}, delay={delay}s)");
        
        await EventLoopContext.ExecuteOnGameThread(() =>
        {
            // Broadcast restart message
            World.Broadcast(0x35, true, $"Server will restart in {delay} seconds. Please find a safe location.");
            
            Timer.StartTimer(TimeSpan.FromSeconds(delay), () =>
            {
                if (save)
                {
                    World.Save();
                }
                World.Broadcast(0x26, true, "Server restarting now...");
                NetState.Shutdown();
                Core.Kill(true);
            });
        });
        
        await SendJsonResponse(response, 200, new { message = $"Restart scheduled in {delay} seconds" });
    }
    
    private static async Task HandleBroadcast(HttpListenerRequest request, HttpListenerResponse response, string username)
    {
        if (request.HttpMethod != "POST")
        {
            await SendJsonResponse(response, 405, new { error = "Method not allowed" });
            return;
        }
        
        using var reader = new System.IO.StreamReader(request.InputStream);
        var body = await reader.ReadToEndAsync();
        var json = JsonSerializer.Deserialize<BroadcastRequest>(body);
        
        if (json == null || string.IsNullOrEmpty(json.Message))
        {
            await SendJsonResponse(response, 400, new { error = "Message required" });
            return;
        }
        
        await EventLoopContext.ExecuteOnGameThread(() =>
        {
            World.Broadcast(0x35, true, json.Message);
        });
        
        Console.WriteLine($"[HTTP API] Broadcast by {username}: {json.Message}");
        await SendJsonResponse(response, 200, new { message = "Message broadcasted" });
    }
    
    private static async Task HandleStaffMessage(HttpListenerRequest request, HttpListenerResponse response, string username)
    {
        if (request.HttpMethod != "POST")
        {
            await SendJsonResponse(response, 405, new { error = "Method not allowed" });
            return;
        }
        
        using var reader = new System.IO.StreamReader(request.InputStream);
        var body = await reader.ReadToEndAsync();
        var json = JsonSerializer.Deserialize<BroadcastRequest>(body);
        
        if (json == null || string.IsNullOrEmpty(json.Message))
        {
            await SendJsonResponse(response, 400, new { error = "Message required" });
            return;
        }
        
        await EventLoopContext.ExecuteOnGameThread(() =>
        {
            World.BroadcastStaff(json.Message);
        });
        
        Console.WriteLine($"[HTTP API] Staff message by {username}: {json.Message}");
        await SendJsonResponse(response, 200, new { message = "Staff message sent" });
    }
    
    #endregion
    
    #region Players
    
    private static async Task HandleGetPlayers(HttpListenerRequest request, HttpListenerResponse response)
    {
        var players = new List<PlayerDto>();
        
        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile is Mobile mobile)
            {
                players.Add(new PlayerDto
                {
                    Serial = mobile.Serial.Value,
                    Name = mobile.Name ?? "",
                    AccessLevel = (int)mobile.AccessLevel,
                    Location = $"{mobile.X},{mobile.Y},{mobile.Z}",
                    Map = mobile.Map?.ToString() ?? "",
                    Account = ns.Account?.Username ?? "",
                    Playtime = (DateTime.UtcNow - mobile.Created).TotalSeconds,
                    IsHidden = mobile.Hidden,
                    IsSquelched = mobile.Squelched,
                    IsJailed = mobile.Region?.Name?.Contains("jail") ?? false
                });
            }
        }
        
        await SendJsonResponse(response, 200, players);
    }
    
    private static async Task HandleSearchPlayers(HttpListenerRequest request, HttpListenerResponse response)
    {
        var searchTerm = request.QueryString["name"]?.ToLower() ?? "";
        
        if (string.IsNullOrEmpty(searchTerm))
        {
            await HandleGetPlayers(request, response);
            return;
        }
        
        var players = new List<PlayerDto>();
        
        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile is Mobile mobile && mobile.Name?.ToLower().Contains(searchTerm) == true)
            {
                players.Add(new PlayerDto
                {
                    Serial = mobile.Serial.Value,
                    Name = mobile.Name ?? "",
                    AccessLevel = (int)mobile.AccessLevel,
                    Location = $"{mobile.X},{mobile.Y},{mobile.Z}",
                    Map = mobile.Map?.ToString() ?? "",
                    Account = ns.Account?.Username ?? "",
                    Playtime = (DateTime.UtcNow - mobile.Created).TotalSeconds,
                    IsHidden = mobile.Hidden,
                    IsSquelched = mobile.Squelched,
                    IsJailed = mobile.Region?.Name?.Contains("jail") ?? false
                });
            }
        }
        
        await SendJsonResponse(response, 200, players);
    }
    
    private static async Task HandleKickPlayer(HttpListenerRequest request, HttpListenerResponse response, 
        string username, int serial)
    {
        var mobile = World.FindMobile(serial);
        
        if (mobile?.NetState == null)
        {
            await SendJsonResponse(response, 404, new { error = "Player not found" });
            return;
        }
        
        // Check access level
        if (Accounts.GetInstance(username) is not Account adminAccount || 
            mobile.AccessLevel >= adminAccount.AccessLevel)
        {
            await SendJsonResponse(response, 403, new { error = "Cannot kick equal or higher rank" });
            return;
        }
        
        await EventLoopContext.ExecuteOnGameThread(() =>
        {
            mobile.NetState.Disconnect("Kicked by administrator");
        });
        
        Console.WriteLine($"[HTTP API] {mobile.Name} kicked by {username}");
        await SendJsonResponse(response, 200, new { message = "Player kicked" });
    }
    
    private static async Task HandleBanPlayer(HttpListenerRequest request, HttpListenerResponse response, 
        string username, int serial)
    {
        var mobile = World.FindMobile(serial);
        
        if (mobile?.NetState == null)
        {
            await SendJsonResponse(response, 404, new { error = "Player not found" });
            return;
        }
        
        var account = mobile.NetState.Account as Account;
        if (account == null)
        {
            await SendJsonResponse(response, 404, new { error = "Account not found" });
            return;
        }
        
        await EventLoopContext.ExecuteOnGameThread(() =>
        {
            account.Banned = true;
            account.SetBanTags(username, "Banned via HTTP API");
            mobile.NetState.Disconnect("Banned by administrator");
        });
        
        Console.WriteLine($"[HTTP API] {mobile.Name} banned by {username}");
        await SendJsonResponse(response, 200, new { message = "Player banned" });
    }
    
    private static async Task HandleUnbanPlayer(HttpListenerRequest request, HttpListenerResponse response, 
        string username, int serial)
    {
        var mobile = World.FindMobile(serial);
        
        if (mobile?.NetState == null)
        {
            await SendJsonResponse(response, 404, new { error = "Player not found" });
            return;
        }
        
        var account = mobile.NetState.Account as Account;
        if (account == null)
        {
            await SendJsonResponse(response, 404, new { error = "Account not found" });
            return;
        }
        
        await EventLoopContext.ExecuteOnGameThread(() =>
        {
            account.Banned = false;
        });
        
        Console.WriteLine($"[HTTP API] {mobile.Name} unbanned by {username}");
        await SendJsonResponse(response, 200, new { message = "Player unbanned" });
    }
    
    private static async Task HandleGetEquipment(HttpListenerRequest request, HttpListenerResponse response, int serial)
    {
        var mobile = World.FindMobile(serial);
        
        if (mobile == null)
        {
            await SendJsonResponse(response, 404, new { error = "Player not found" });
            return;
        }
        
        var items = new List<ItemDto>();
        
        foreach (var item in mobile.Items)
        {
            items.Add(new ItemDto
            {
                Serial = item.Serial.Value,
                Name = item.Name ?? item.GetType().Name,
                ItemID = item.ItemID,
                Hue = item.Hue,
                Amount = item.Amount,
                Layer = item.Layer.ToString(),
                Properties = GetItemProperties(item)
            });
        }
        
        await SendJsonResponse(response, 200, items);
    }
    
    private static async Task HandleGetBackpack(HttpListenerRequest request, HttpListenerResponse response, int serial)
    {
        var mobile = World.FindMobile(serial);
        
        if (mobile?.Backpack == null)
        {
            await SendJsonResponse(response, 404, new { error = "Backpack not found" });
            return;
        }
        
        var items = SerializeContainer(mobile.Backpack);
        await SendJsonResponse(response, 200, items);
    }
    
    private static async Task HandleGetSkills(HttpListenerRequest request, HttpListenerResponse response, int serial)
    {
        var mobile = World.FindMobile(serial);
        
        if (mobile == null)
        {
            await SendJsonResponse(response, 404, new { error = "Player not found" });
            return;
        }
        
        var skills = new List<SkillDto>();
        
        for (int i = 0; i < mobile.Skills.Length; i++)
        {
            var skill = mobile.Skills[i];
            if (skill?.Base > 0)
            {
                skills.Add(new SkillDto
                {
                    Name = skill.Info.Name ?? "",
                    Value = skill.Value,
                    Base = skill.Base,
                    Cap = skill.Cap,
                    Lock = skill.Lock.ToString()
                });
            }
        }
        
        await SendJsonResponse(response, 200, skills);
    }
    
    private static async Task HandleGetProperties(HttpListenerRequest request, HttpListenerResponse response, int serial)
    {
        var mobile = World.FindMobile(serial);
        
        if (mobile == null)
        {
            await SendJsonResponse(response, 404, new { error = "Player not found" });
            return;
        }
        
        var properties = new Dictionary<string, object>
        {
            ["Name"] = mobile.Name ?? "",
            ["Body"] = mobile.Body,
            ["Hue"] = mobile.Hue,
            ["Hits"] = mobile.Hits,
            ["HitsMax"] = mobile.HitsMax,
            ["Stam"] = mobile.Stam,
            ["StamMax"] = mobile.StamMax,
            ["Mana"] = mobile.Mana,
            ["ManaMax"] = mobile.ManaMax,
            ["Str"] = mobile.RawStr,
            ["Dex"] = mobile.RawDex,
            ["Int"] = mobile.RawInt,
            ["X"] = mobile.X,
            ["Y"] = mobile.Y,
            ["Z"] = mobile.Z,
            ["Map"] = mobile.Map?.ToString() ?? "",
            ["AccessLevel"] = mobile.AccessLevel.ToString(),
            ["Hidden"] = mobile.Hidden,
            ["Blessed"] = mobile.Blessed,
            ["Criminal"] = mobile.Criminal,
            ["Region"] = mobile.Region?.Name ?? ""
        };
        
        await SendJsonResponse(response, 200, properties);
    }
    
    #endregion
    
    #region Accounts
    
    private static async Task HandleSearchAccounts(HttpListenerRequest request, HttpListenerResponse response)
    {
        var searchTerm = request.QueryString["username"]?.ToLower() ?? "";
        
        var accounts = new List<AccountDto>();
        
        foreach (Account account in Accounts.GetAccounts())
        {
            if (string.IsNullOrEmpty(searchTerm) || 
                account.Username.ToLower().Contains(searchTerm))
            {
                accounts.Add(new AccountDto
                {
                    Username = account.Username,
                    AccessLevel = (int)account.AccessLevel,
                    IsBanned = account.Banned,
                    LastLogin = account.LastLogin,
                    CreationDate = account.Created,
                    CharacterCount = account.Length
                });
            }
        }
        
        await SendJsonResponse(response, 200, accounts);
    }
    
    private static async Task HandleBanAccount(HttpListenerRequest request, HttpListenerResponse response, 
        string username, string targetAccount)
    {
        if (Accounts.GetInstance(targetAccount) is not Account account)
        {
            await SendJsonResponse(response, 404, new { error = "Account not found" });
            return;
        }
        
        await EventLoopContext.ExecuteOnGameThread(() =>
        {
            account.Banned = true;
            account.SetBanTags(username, "Banned via HTTP API");
        });
        
        Console.WriteLine($"[HTTP API] Account {targetAccount} banned by {username}");
        await SendJsonResponse(response, 200, new { message = "Account banned" });
    }
    
    private static async Task HandleUnbanAccount(HttpListenerRequest request, HttpListenerResponse response, 
        string username, string targetAccount)
    {
        if (Accounts.GetInstance(targetAccount) is not Account account)
        {
            await SendJsonResponse(response, 404, new { error = "Account not found" });
            return;
        }
        
        await EventLoopContext.ExecuteOnGameThread(() =>
        {
            account.Banned = false;
        });
        
        Console.WriteLine($"[HTTP API] Account {targetAccount} unbanned by {username}");
        await SendJsonResponse(response, 200, new { message = "Account unbanned" });
    }
    
    #endregion
    
    #region Firewall
    
    private static async Task HandleGetFirewallRules(HttpListenerRequest request, HttpListenerResponse response)
    {
        var rules = Firewall.Entries.Select(e => new
        {
            entry = e.Entry,
            addedBy = e.Comment ?? "",
            dateAdded = e.DateAdded
        }).ToList();
        
        await SendJsonResponse(response, 200, rules);
    }
    
    private static async Task HandleAddFirewallRule(HttpListenerRequest request, HttpListenerResponse response, string username)
    {
        var entry = request.QueryString["entry"];
        var comment = request.QueryString["comment"] ?? "";
        
        if (string.IsNullOrEmpty(entry))
        {
            await SendJsonResponse(response, 400, new { error = "Entry parameter required" });
            return;
        }
        
        await EventLoopContext.ExecuteOnGameThread(() =>
        {
            Firewall.Add(new FirewallEntry
            {
                Entry = entry,
                Comment = $"Added via HTTP API by {username}: {comment}",
                DateAdded = DateTime.UtcNow
            });
        });
        
        Console.WriteLine($"[HTTP API] Firewall rule added by {username}: {entry}");
        await SendJsonResponse(response, 200, new { message = "Firewall rule added" });
    }
    
    private static async Task HandleRemoveFirewallRule(HttpListenerRequest request, HttpListenerResponse response, string username)
    {
        var entry = request.QueryString["entry"];
        
        if (string.IsNullOrEmpty(entry))
        {
            await SendJsonResponse(response, 400, new { error = "Entry parameter required" });
            return;
        }
        
        await EventLoopContext.ExecuteOnGameThread(() =>
        {
            Firewall.Remove(entry);
        });
        
        Console.WriteLine($"[HTTP API] Firewall rule removed by {username}: {entry}");
        await SendJsonResponse(response, 200, new { message = "Firewall rule removed" });
    }
    
    #endregion
    
    #region Logs
    
    private static async Task HandleGetLogs(HttpListenerRequest request, HttpListenerResponse response)
    {
        var lines = int.TryParse(request.QueryString["lines"], out var l) ? l : 100;
        var level = request.QueryString["level"] ?? "all";
        
        // This would integrate with ModernUO's logging system
        // For now, return a simple response
        var logs = new List<object>();
        
        await SendJsonResponse(response, 200, logs);
    }
    
    #endregion
    
    #region Server Lockdown
    
    private static async Task HandleSetLockdown(HttpListenerRequest request, HttpListenerResponse response, string username)
    {
        var level = request.QueryString["level"];
        
        if (string.IsNullOrEmpty(level))
        {
            await SendJsonResponse(response, 400, new { error = "Level parameter required" });
            return;
        }
        
        if (Enum.TryParse<AccessLevel>(level, true, out var accessLevel))
        {
            await EventLoopContext.ExecuteOnGameThread(() =>
            {
                AccountHandler.LockdownLevel = accessLevel;
            });
            
            Console.WriteLine($"[HTTP API] Lockdown set to {level} by {username}");
            await SendJsonResponse(response, 200, new { message = $"Lockdown set to {level}" });
        }
        else
        {
            await SendJsonResponse(response, 400, new { error = $"Invalid access level: {level}" });
        }
    }
    
    private static async Task HandleDisableLockdown(HttpListenerRequest request, HttpListenerResponse response, string username)
    {
        await EventLoopContext.ExecuteOnGameThread(() =>
        {
            AccountHandler.LockdownLevel = null;
        });
        
        Console.WriteLine($"[HTTP API] Lockdown disabled by {username}");
        await SendJsonResponse(response, 200, new { message = "Lockdown disabled" });
    }
    
    #endregion
    
    #region Helpers
    
    private static async Task SendJsonResponse(HttpListenerResponse response, int statusCode, object data)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        var buffer = System.Text.Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.Close();
    }
    
    private static int ExtractSerial(string path)
    {
        var parts = path.Split('/');
        return int.TryParse(parts[3], out var serial) ? serial : 0;
    }
    
    private static string ExtractAccountName(string path)
    {
        var parts = path.Split('/');
        return parts[3];
    }
    
    private static List<ItemDto> SerializeContainer(Container container)
    {
        var items = new List<ItemDto>();
        
        foreach (var item in container.Items)
        {
            var dto = new ItemDto
            {
                Serial = item.Serial.Value,
                Name = item.Name ?? item.GetType().Name,
                ItemID = item.ItemID,
                Hue = item.Hue,
                Amount = item.Amount,
                Properties = GetItemProperties(item)
            };
            
            if (item is Container nestedContainer && nestedContainer.Items.Count > 0)
            {
                dto.Children = SerializeContainer(nestedContainer);
            }
            
            items.Add(dto);
        }
        
        return items;
    }
    
    private static List<PropertyDto> GetItemProperties(Item item)
    {
        var properties = new List<PropertyDto>();
        
        var list = item.GetProperties();
        foreach (var entry in list)
        {
            properties.Add(new PropertyDto
            {
                Number = entry.Number,
                Text = entry.String
            });
        }
        
        return properties;
    }
    
    #endregion
}

#region DTOs

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class BroadcastRequest
{
    public string Message { get; set; } = "";
}

public class PlayerDto
{
    public int Serial { get; set; }
    public string Name { get; set; } = "";
    public int AccessLevel { get; set; }
    public string Location { get; set; } = "";
    public string Map { get; set; } = "";
    public string Account { get; set; } = "";
    public double Playtime { get; set; }
    public bool IsHidden { get; set; }
    public bool IsSquelched { get; set; }
    public bool IsJailed { get; set; }
}

public class AccountDto
{
    public string Username { get; set; } = "";
    public int AccessLevel { get; set; }
    public bool IsBanned { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime CreationDate { get; set; }
    public int CharacterCount { get; set; }
}

public class ItemDto
{
    public int Serial { get; set; }
    public string Name { get; set; } = "";
    public int ItemID { get; set; }
    public int Hue { get; set; }
    public int Amount { get; set; }
    public string Layer { get; set; } = "";
    public List<PropertyDto> Properties { get; set; } = new();
    public List<ItemDto>? Children { get; set; }
}

public class PropertyDto
{
    public int Number { get; set; }
    public string Text { get; set; } = "";
}

public class SkillDto
{
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public double Base { get; set; }
    public int Cap { get; set; }
    public string Lock { get; set; } = "";
}

#endregion
