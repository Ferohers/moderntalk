import Foundation
import Security

// MARK: - API Client

@MainActor
class UOCommanderAPI: ObservableObject {
    private var baseURL: URL
    @Published private var authToken: String?
    
    init(serverURL: String = "http://localhost:8081") {
        self.baseURL = URL(string: serverURL)!
    }
    
    func updateBaseURL(_ urlString: String) {
        if let url = URL(string: urlString) {
            self.baseURL = url
        }
    }
    
    // MARK: - Authentication
    
    func login(username: String, password: String) async throws -> LoginResponse {
        var request = URLRequest(url: baseURL.appendingPathComponent("/api/auth/login"))
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try JSONEncoder().encode(LoginRequest(username: username, password: password))
        
        let (data, response) = try await URLSession.shared.data(for: request)
        
        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.networkError("Invalid response")
        }
        
        if httpResponse.statusCode == 400 {
            // WebPortal returns 400 with error message
            if let errorResponse = try? JSONDecoder().decode(ErrorResponse.self, from: data) {
                throw APIError.authenticationFailedWithMessage(errorResponse.error)
            }
            throw APIError.authenticationFailed
        }
        
        if httpResponse.statusCode == 403 {
            throw APIError.insufficientPrivileges
        }
        
        // WebPortal returns username and expiresIn, but tokens are in HttpOnly cookies
        // We need to extract token info differently - for now, create a simplified response
        let loginResponse = try JSONDecoder().decode(WebPortalLoginResponse.self, from: data)
        
        // For WebPortal, we'll rely on cookies for auth, but store a flag
        self.authToken = "cookie-based"
        
        // Store username for API calls
        KeychainHelper.save(token: loginResponse.username)
        
        return LoginResponse(
            token: "cookie-based",
            username: loginResponse.username,
            accessLevel: 0, // Will be fetched from account info
            expiresHours: loginResponse.expiresIn / 3600
        )
    }
    
    func logout() async throws {
        guard let token = authToken ?? KeychainHelper.loadToken() else { return }
        
        var request = URLRequest(url: baseURL.appendingPathComponent("/api/auth/logout"))
        request.httpMethod = "POST"
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        
        _ = try await URLSession.shared.data(for: request)
        
        self.authToken = nil
        KeychainHelper.deleteToken()
    }
    
    func verifyToken() async -> Bool {
        guard let token = authToken ?? KeychainHelper.loadToken() else {
            return false
        }
        
        var request = URLRequest(url: baseURL.appendingPathComponent("/api/auth/verify"))
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        
        do {
            let (_, response) = try await URLSession.shared.data(for: request)
            guard let httpResponse = response as? HTTPURLResponse else { return false }
            return httpResponse.statusCode == 200
        } catch {
            return false
        }
    }
    
    // MARK: - Server Control
    
    func getServerStatus() async throws -> ServerStatus {
        let request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/server/status")
        )
        
        let (data, _) = try await URLSession.shared.data(for: request)
        return try JSONDecoder().decode(ServerStatus.self, from: data)
    }
    
    func saveWorld() async throws {
        var request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/server/save")
        )
        request.httpMethod = "POST"
        
        _ = try await URLSession.shared.data(for: request)
    }
    
    func shutdown(save: Bool = true) async throws {
        var components = URLComponents(url: baseURL.appendingPathComponent("/api/server/shutdown"), resolvingAgainstBaseURL: false)!
        components.queryItems = [URLQueryItem(name: "save", value: save ? "true" : "false")]
        
        var request = try createAuthenticatedRequest(url: components.url!)
        request.httpMethod = "POST"
        
        _ = try await URLSession.shared.data(for: request)
    }
    
    func restart(save: Bool = true, delay: Int = 60) async throws {
        var components = URLComponents(url: baseURL.appendingPathComponent("/api/server/restart"), resolvingAgainstBaseURL: false)!
        components.queryItems = [
            URLQueryItem(name: "save", value: save ? "true" : "false"),
            URLQueryItem(name: "delay", value: String(delay))
        ]
        
        var request = try createAuthenticatedRequest(url: components.url!)
        request.httpMethod = "POST"
        
        _ = try await URLSession.shared.data(for: request)
    }
    
    func broadcast(message: String, staffOnly: Bool = false) async throws {
        let endpoint = staffOnly ? "/api/server/staff-message" : "/api/server/broadcast"
        var request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent(endpoint)
        )
        request.httpMethod = "POST"
        request.httpBody = try JSONEncoder().encode(BroadcastRequest(message: message))
        
        _ = try await URLSession.shared.data(for: request)
    }
    
    // MARK: - Players
    
    func getOnlinePlayers() async throws -> [Player] {
        let request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/players")
        )
        
        let (data, _) = try await URLSession.shared.data(for: request)
        return try JSONDecoder().decode([Player].self, from: data)
    }
    
    func searchPlayers(name: String) async throws -> [Player] {
        var components = URLComponents(url: baseURL.appendingPathComponent("/api/players/search"), resolvingAgainstBaseURL: false)!
        components.queryItems = [URLQueryItem(name: "name", value: name)]
        
        let request = try createAuthenticatedRequest(url: components.url!)
        let (data, _) = try await URLSession.shared.data(for: request)
        return try JSONDecoder().decode([Player].self, from: data)
    }
    
    func getPlayerDetail(serial: Int) async throws -> PlayerDetail {
        let request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/players/\(serial)")
        )
        
        let (data, _) = try await URLSession.shared.data(for: request)
        return try JSONDecoder().decode(PlayerDetail.self, from: data)
    }
    
    func kickPlayer(serial: Int, reason: String? = nil) async throws {
        var request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/players/\(serial)/kick")
        )
        request.httpMethod = "POST"
        
        _ = try await URLSession.shared.data(for: request)
    }
    
    func banPlayer(serial: Int) async throws {
        var request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/players/\(serial)/ban")
        )
        request.httpMethod = "POST"
        
        _ = try await URLSession.shared.data(for: request)
    }
    
    func unbanPlayer(serial: Int) async throws {
        var request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/players/\(serial)/unban")
        )
        request.httpMethod = "POST"
        
        _ = try await URLSession.shared.data(for: request)
    }
    
    func getPlayerEquipment(serial: Int) async throws -> [Item] {
        let request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/players/\(serial)/equipment")
        )
        
        let (data, _) = try await URLSession.shared.data(for: request)
        return try JSONDecoder().decode([Item].self, from: data)
    }
    
    func getPlayerBackpack(serial: Int) async throws -> [Item] {
        let request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/players/\(serial)/backpack")
        )
        
        let (data, _) = try await URLSession.shared.data(for: request)
        return try JSONDecoder().decode([Item].self, from: data)
    }
    
    func getPlayerSkills(serial: Int) async throws -> [Skill] {
        let request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/players/\(serial)/skills")
        )
        
        let (data, _) = try await URLSession.shared.data(for: request)
        return try JSONDecoder().decode([Skill].self, from: data)
    }
    
    func getPlayerProperties(serial: Int) async throws -> [String: AnyCodable] {
        let request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/players/\(serial)/properties")
        )
        
        let (data, _) = try await URLSession.shared.data(for: request)
        let json = try JSONSerialization.jsonObject(with: data) as? [String: Any] ?? [:]
        return json.mapValues { AnyCodable($0) }
    }
    
    // MARK: - Accounts
    
    func searchAccounts(username: String = "") async throws -> [Account] {
        var components = URLComponents(url: baseURL.appendingPathComponent("/api/accounts/search"), resolvingAgainstBaseURL: false)!
        if !username.isEmpty {
            components.queryItems = [URLQueryItem(name: "username", value: username)]
        }
        
        let request = try createAuthenticatedRequest(url: components.url!)
        let (data, _) = try await URLSession.shared.data(for: request)
        return try JSONDecoder().decode([Account].self, from: data)
    }
    
    func banAccount(username: String) async throws {
        var request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/accounts/\(username)/ban")
        )
        request.httpMethod = "POST"
        
        _ = try await URLSession.shared.data(for: request)
    }
    
    func unbanAccount(username: String) async throws {
        var request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/accounts/\(username)/unban")
        )
        request.httpMethod = "POST"
        
        _ = try await URLSession.shared.data(for: request)
    }
    
    // MARK: - Firewall (DISABLED - Firewall type not available in ModernUO)
    
    /*
    func getFirewallRules() async throws -> [FirewallRule] {
        let request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/firewall")
        )

        let (data, _) = try await URLSession.shared.data(for: request)
        return try JSONDecoder().decode([FirewallRule].self, from: data)
    }

    func addFirewallRule(entry: String, comment: String = "") async throws {
        var components = URLComponents(url: baseURL.appendingPathComponent("/api/firewall"), resolvingAgainstBaseURL: false)!
        components.queryItems = [
            URLQueryItem(name: "entry", value: entry),
            URLQueryItem(name: "comment", value: comment)
        ]

        var request = try createAuthenticatedRequest(url: components.url!)
        request.httpMethod = "POST"

        _ = try await URLSession.shared.data(for: request)
    }

    func removeFirewallRule(entry: String) async throws {
        var components = URLComponents(url: baseURL.appendingPathComponent("/api/firewall"), resolvingAgainstBaseURL: false)!
        components.queryItems = [URLQueryItem(name: "entry", value: entry)]

        var request = try createAuthenticatedRequest(url: components.url!)
        request.httpMethod = "DELETE"

        _ = try await URLSession.shared.data(for: request)
    }
    */
    
    // MARK: - Logs
    
    func getLogs(lines: Int = 100, level: String = "all") async throws -> [LogEntry] {
        var components = URLComponents(url: baseURL.appendingPathComponent("/api/logs"), resolvingAgainstBaseURL: false)!
        components.queryItems = [
            URLQueryItem(name: "lines", value: String(lines)),
            URLQueryItem(name: "level", value: level)
        ]
        
        let request = try createAuthenticatedRequest(url: components.url!)
        let (data, _) = try await URLSession.shared.data(for: request)
        return try JSONDecoder().decode([LogEntry].self, from: data)
    }

    // MARK: - Server Lockdown (DISABLED - AccountHandler.LockdownLevel not available)
    
    /*
    func setLockdownLevel(_ level: String) async throws {
        var components = URLComponents(url: baseURL.appendingPathComponent("/api/server/lockdown"), resolvingAgainstBaseURL: false)!
        components.queryItems = [URLQueryItem(name: "level", value: level)]

        var request = try createAuthenticatedRequest(url: components.url!)
        request.httpMethod = "POST"

        _ = try await URLSession.shared.data(for: request)
    }

    func disableLockdown() async throws {
        var request = try createAuthenticatedRequest(
            url: baseURL.appendingPathComponent("/api/server/lockdown")
        )
        request.httpMethod = "DELETE"

        _ = try await URLSession.shared.data(for: request)
    }
    */

    // MARK: - Private Helpers
    
    private func createAuthenticatedRequest(url: URL) throws -> URLRequest {
        var request = URLRequest(url: url)
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        
        let token = authToken ?? KeychainHelper.loadToken()
        guard let token = token else {
            throw APIError.notAuthenticated
        }
        
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        return request
    }
}

// MARK: - API Errors

enum APIError: Error, LocalizedError {
    case networkError(String)
    case authenticationFailed
    case insufficientPrivileges
    case notAuthenticated
    case serverError(String)
    case decodingError(String)
    
    var errorDescription: String? {
        switch self {
        case .networkError(let message):
            return "Network error: \(message)"
        case .authenticationFailed:
            return "Invalid username or password"
        case .insufficientPrivileges:
            return "Insufficient privileges (requires GameMaster+ access)"
        case .notAuthenticated:
            return "Not authenticated"
        case .serverError(let message):
            return "Server error: \(message)"
        case .decodingError(let message):
            return "Decoding error: \(message)"
        }
    }
}

// MARK: - Keychain Helper (Simplified)

enum KeychainHelper {
    private static let serviceName = "com.uocommander.token"
    private static let accountName = "api_token"
    
    static func save(token: String) {
        guard let data = token.data(using: .utf8) else { return }
        
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: serviceName,
            kSecAttrAccount as String: accountName,
            kSecValueData as String: data
        ]
        
        SecItemDelete(query as CFDictionary)
        SecItemAdd(query as CFDictionary, nil)
    }
    
    static func loadToken() -> String? {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: serviceName,
            kSecAttrAccount as String: accountName,
            kSecReturnData as String: true,
            kSecMatchLimit as String: kSecMatchLimitOne
        ]
        
        var result: AnyObject?
        SecItemCopyMatching(query as CFDictionary, &result)
        
        guard let data = result as? Data else { return nil }
        return String(data: data, encoding: .utf8)
    }
    
    static func deleteToken() {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: serviceName,
            kSecAttrAccount as String: accountName
        ]
        
        SecItemDelete(query as CFDictionary)
    }
}

// MARK: - AnyCodable Helper

struct AnyCodable: Codable {
    let value: Any
    
    init(_ value: Any) {
        self.value = value
    }
    
    init(from decoder: Decoder) throws {
        let container = try decoder.singleValueContainer()
        if let bool = try? container.decode(Bool.self) {
            value = bool
        } else if let int = try? container.decode(Int.self) {
            value = int
        } else if let double = try? container.decode(Double.self) {
            value = double
        } else if let string = try? container.decode(String.self) {
            value = string
        } else {
            value = ""
        }
    }
    
    func encode(to encoder: Encoder) throws {
        var container = encoder.singleValueContainer()
        if let bool = value as? Bool {
            try container.encode(bool)
        } else if let int = value as? Int {
            try container.encode(int)
        } else if let double = value as? Double {
            try container.encode(double)
        } else if let string = value as? String {
            try container.encode(string)
        }
    }
}
