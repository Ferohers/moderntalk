import SwiftUI
import Foundation

@MainActor
class AppState: ObservableObject {
    @Published var isAuthenticated = false
    @Published var currentUser: String?
    @Published var accessLevel: AccessLevel = .player
    @Published var serverStatus: ServerStatus?
    @Published var players: [Player] = []
    @Published var selectedPlayer: Player?
    @Published var playerEquipment: [Item] = []
    @Published var playerSkills: [Skill] = []
    @Published var accounts: [Account] = []
    @Published var firewallRules: [FirewallRule] = []
    @Published var logs: [LogEntry] = []
    @Published var isLoading = false
    @Published var errorMessage: String?
    @Published var isRefreshing = false
    
    private let api = UOCommanderAPI()
    private var refreshTimer: Timer?
    
    func login(username: String, password: String) async {
        isLoading = true
        errorMessage = nil
        
        do {
            let response = try await api.login(username: username, password: password)
            self.isAuthenticated = true
            self.currentUser = response.username
            self.accessLevel = AccessLevel(rawValue: response.accessLevel) ?? .player
            
            // Start auto-refresh
            startAutoRefresh()
            
        } catch let error as APIError {
            errorMessage = error.errorDescription
        } catch {
            errorMessage = error.localizedDescription
        }
        
        isLoading = false
    }
    
    func logout() async {
        stopAutoRefresh()
        do {
            try await api.logout()
        } catch {
            // Ignore logout errors
        }
        isAuthenticated = false
        currentUser = nil
        serverStatus = nil
        players = []
        selectedPlayer = nil
    }
    
    func verifyAuthentication() async {
        let isValid = await api.verifyToken()
        if isValid {
            isAuthenticated = true
            startAutoRefresh()
        }
    }
    
    // MARK: - Server Operations
    
    func refreshServerStatus() async {
        do {
            serverStatus = try await api.getServerStatus()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func saveWorld() async {
        do {
            try await api.saveWorld()
            await refreshServerStatus()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func shutdown(save: Bool = true) async {
        do {
            try await api.shutdown(save: save)
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func restart(save: Bool = true, delay: Int = 60) async {
        do {
            try await api.restart(save: save, delay: delay)
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func broadcast(message: String, staffOnly: Bool = false) async {
        do {
            try await api.broadcast(message: message, staffOnly: staffOnly)
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    // MARK: - Player Operations
    
    func refreshPlayers() async {
        do {
            players = try await api.getOnlinePlayers()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func searchPlayers(name: String) async {
        do {
            players = try await api.searchPlayers(name: name)
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func kickPlayer(serial: Int, reason: String? = nil) async {
        do {
            try await api.kickPlayer(serial: serial, reason: reason)
            await refreshPlayers()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func banPlayer(serial: Int) async {
        do {
            try await api.banPlayer(serial: serial)
            await refreshPlayers()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func unbanPlayer(serial: Int) async {
        do {
            try await api.unbanPlayer(serial: serial)
            await refreshPlayers()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func loadPlayerEquipment(serial: Int) async {
        do {
            playerEquipment = try await api.getPlayerEquipment(serial: serial)
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func loadPlayerSkills(serial: Int) async {
        do {
            playerSkills = try await api.getPlayerSkills(serial: serial)
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    // MARK: - Account Operations
    
    func searchAccounts(username: String = "") async {
        do {
            accounts = try await api.searchAccounts(username: username)
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func banAccount(username: String) async {
        do {
            try await api.banAccount(username: username)
            await searchAccounts()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func unbanAccount(username: String) async {
        do {
            try await api.unbanAccount(username: username)
            await searchAccounts()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    // MARK: - Firewall Operations
    
    func refreshFirewallRules() async {
        do {
            firewallRules = try await api.getFirewallRules()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func addFirewallRule(entry: String, comment: String = "") async {
        do {
            try await api.addFirewallRule(entry: entry, comment: comment)
            await refreshFirewallRules()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func removeFirewallRule(entry: String) async {
        do {
            try await api.removeFirewallRule(entry: entry)
            await refreshFirewallRules()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    // MARK: - Logs Operations
    
    func refreshLogs(lines: Int = 100, level: String = "all") async {
        do {
            logs = try await api.getLogs(lines: lines, level: level)
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    // MARK: - Server Lockdown
    
    func setLockdownLevel(_ level: String) async {
        do {
            try await api.setLockdownLevel(level)
            await refreshServerStatus()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func disableLockdown() async {
        do {
            try await api.disableLockdown()
            await refreshServerStatus()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    // MARK: - Auto Refresh
    
    private func startAutoRefresh() {
        Task {
            await refreshServerStatus()
            await refreshPlayers()
        }
        
        refreshTimer = Timer.scheduledTimer(withTimeInterval: 30.0, repeats: true) { [weak self] _ in
            Task { [weak self] in
                await self?.refreshServerStatus()
                await self?.refreshPlayers()
            }
        }
    }
    
    private func stopAutoRefresh() {
        refreshTimer?.invalidate()
        refreshTimer = nil
    }
}
