import Foundation

// MARK: - Data Models

struct ServerStatus: Codable, Sendable {
    let isRunning: Bool
    let uptime: Double
    let playerCount: Int
    let maxPlayers: Int
    let memoryUsage: Int64
    let cpuUsage: Double
    let worldSaveStatus: String
    let lastSaveTime: Date?
    let version: String
    let lockdownLevel: String
}

struct Player: Codable, Identifiable, Hashable, Sendable {
    let id: Int
    let name: String
    let accessLevel: Int
    let location: String
    let map: String
    let account: String
    let playtime: Double
    let isHidden: Bool
    let isSquelched: Bool
    let isJailed: Bool
    
    var serial: Int { id }
    
    func hash(into hasher: inout Hasher) {
        hasher.combine(id)
    }
    
    static func == (lhs: Player, rhs: Player) -> Bool {
        lhs.id == rhs.id
    }
}

struct PlayerDetail: Codable, Sendable {
    let serial: Int
    let name: String
    let accessLevel: Int
    let location: String
    let map: String
    let account: String
    let playtime: Double
    let isHidden: Bool
    let isSquelched: Bool
    let isJailed: Bool
    let body: Int
    let hue: Int
    let hits: Int
    let hitsMax: Int
    let stam: Int
    let stamMax: Int
    let mana: Int
    let manaMax: Int
    let str: Int
    let dex: Int
    let int: Int
    let x: Int
    let y: Int
    let z: Int
    let region: String
    let criminal: Bool
    let blessed: Bool
}

struct Account: Codable, Identifiable, Hashable, Sendable {
    let username: String
    let accessLevel: Int
    let isBanned: Bool
    let lastLogin: Date?
    let creationDate: Date
    let characterCount: Int
    
    var id: String { username }
    
    func hash(into hasher: inout Hasher) {
        hasher.combine(username)
    }
    
    static func == (lhs: Account, rhs: Account) -> Bool {
        lhs.username == rhs.username
    }
}

struct Item: Codable, Identifiable, Sendable {
    let id: Int
    let name: String
    let itemID: Int
    let hue: Int
    let amount: Int
    let layer: String
    let properties: [Property]
    let children: [Item]?
    
    var serial: Int { id }
}

struct Property: Codable, Sendable {
    let number: Int
    let text: String
}

struct Skill: Codable, Identifiable, Sendable {
    let name: String
    let value: Double
    let base: Double
    let cap: Int
    let lock: String
    
    var id: String { name }
}

struct FirewallRule: Codable, Identifiable, Sendable {
    let entry: String
    let addedBy: String
    let dateAdded: Date
    
    var id: String { entry }
}

struct LoginResponse: Codable {
    let token: String
    let username: String
    let accessLevel: Int
    let expiresHours: Int
}

struct LogEntry: Codable, Identifiable, Sendable {
    let id: Int
    let timestamp: Date
    let level: String
    let message: String
    let source: String?
}

// MARK: - Access Level Enum

enum AccessLevel: Int, Codable, Sendable {
    case player = 0
    case counselor = 1
    case gameMaster = 2
    case seer = 3
    case administrator = 4
    case developer = 5
    case owner = 6
    
    var displayName: String {
        switch self {
        case .player: return "Player"
        case .counselor: return "Counselor"
        case .gameMaster: return "GameMaster"
        case .seer: return "Seer"
        case .administrator: return "Administrator"
        case .developer: return "Developer"
        case .owner: return "Owner"
        }
    }
    
    var color: String {
        switch self {
        case .player: return "blue"
        case .counselor: return "green"
        case .gameMaster: return "purple"
        case .seer: return "cyan"
        case .administrator: return "red"
        case .developer: return "orange"
        case .owner: return "yellow"
        }
    }
}

// MARK: - Request/Response Models

struct LoginRequest: Codable {
    let username: String
    let password: String
}

struct BroadcastRequest: Codable {
    let message: String
}

struct MessageResponse: Codable {
    let message: String
}
