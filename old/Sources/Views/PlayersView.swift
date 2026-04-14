import SwiftUI

struct PlayersView: View {
    @EnvironmentObject var appState: AppState
    @State private var searchText = ""
    @State private var selectedPlayerDetail: Player?
    @State private var showPlayerDetail = false
    @State private var showBanConfirmation = false
    @State private var playerToBan: Player?
    @State private var showKickConfirmation = false
    @State private var playerToKick: Player?
    
    var body: some View {
        VStack(spacing: 0) {
            // Search Bar
            HStack {
                SearchBar(text: $searchText, placeholder: "Search players by name...")
                    .onChange(of: searchText) { _, newValue in
                        Task {
                            if newValue.isEmpty {
                                await appState.refreshPlayers()
                            } else {
                                await appState.searchPlayers(name: newValue)
                            }
                        }
                    }
                
                Button {
                    Task {
                        await appState.refreshPlayers()
                    }
                } label: {
                    Image(systemName: "arrow.clockwise")
                }
                .buttonStyle(.borderless)
                .disabled(appState.isRefreshing)
            }
            .padding()
            
            Divider()
            
            // Player List
            if appState.players.isEmpty && !appState.isLoading {
                ContentUnavailableView(
                    "No Players Online",
                    systemImage: "person.3.slash",
                    description: Text("There are currently no players connected to the server.")
                )
            } else {
                List(appState.players, selection: $selectedPlayerDetail) { player in
                    PlayerRow(player: player)
                        .tag(player)
                }
                .contextMenu(forSelectionType: Player.self) { players in
                    if let player = players.first {
                        Button {
                            selectedPlayerDetail = player
                            showPlayerDetail = true
                        } label: {
                            Label("Inspect", systemImage: "magnifyingglass")
                        }
                        
                        Divider()
                        
                        Button {
                            playerToKick = player
                            showKickConfirmation = true
                        } label: {
                            Label("Kick", systemImage: "person.fill.xmark")
                        }
                        
                        Button(role: .destructive) {
                            playerToBan = player
                            showBanConfirmation = true
                        } label: {
                            Label("Ban", systemImage: "hand.raised.fill")
                        }
                    }
                } primaryAction: { selectedPlayers in
                    if let player = selectedPlayers.first {
                        selectedPlayerDetail = player
                        showPlayerDetail = true
                    }
                }
            }
        }
        .navigationTitle("Players")
        .navigationSubtitle("\(appState.players.count) online")
        .sheet(isPresented: $showPlayerDetail) {
            if let player = selectedPlayerDetail {
                PlayerDetailView(player: player)
            }
        }
        .alert("Kick Player?", isPresented: $showKickConfirmation) {
            Button("Cancel", role: .cancel) { }
            Button("Kick", role: .destructive) {
                if let player = playerToKick {
                    Task {
                        await appState.kickPlayer(serial: player.serial)
                    }
                }
            }
        } message: {
            Text("Are you sure you want to kick \(playerToKick?.name ?? "this player")?")
        }
        .alert("Ban Player?", isPresented: $showBanConfirmation) {
            Button("Cancel", role: .cancel) { }
            Button("Ban", role: .destructive) {
                if let player = playerToBan {
                    Task {
                        await appState.banPlayer(serial: player.serial)
                    }
                }
            }
        } message: {
            Text("This will ban \(playerToBan?.name ?? "this player") and their entire account. They will not be able to reconnect.")
        }
        .task {
            if appState.players.isEmpty {
                await appState.refreshPlayers()
            }
        }
    }
    
    private func accessLevelColor(_ level: Int) -> Color {
        switch level {
        case 0: return .blue
        case 1: return .green
        case 2: return .purple
        case 3: return .cyan
        case 4: return .red
        case 5: return .orange
        case 6: return .yellow
        default: return .gray
        }
    }
    
    private func formatPlaytime(_ seconds: Double) -> String {
        let hours = Int(seconds) / 3600
        let minutes = (Int(seconds) % 3600) / 60
        if hours > 0 {
            return "\(hours)h \(minutes)m"
        }
        return "\(minutes)m"
    }
}

// MARK: - Search Bar Component

struct SearchBar: View {
    @Binding var text: String
    var placeholder: String
    
    var body: some View {
        HStack {
            Image(systemName: "magnifyingglass")
                .foregroundColor(.secondary)
            
            TextField(placeholder, text: $text)
                .textFieldStyle(.plain)
            
            if !text.isEmpty {
                Button {
                    text = ""
                } label: {
                    Image(systemName: "xmark.circle.fill")
                        .foregroundColor(.secondary)
                }
                .buttonStyle(.plain)
            }
        }
        .padding(8)
        .background(Color(nsColor: .textBackgroundColor))
        .cornerRadius(8)
    }
}

// MARK: - Player Detail View

struct PlayerDetailView: View {
    @EnvironmentObject var appState: AppState
    let player: Player
    @State private var selectedTab = 0
    @State private var equipment: [Item] = []
    @State private var skills: [Skill] = []
    
    var body: some View {
        NavigationStack {
            TabView(selection: $selectedTab) {
                // Overview Tab
                PlayerOverviewTab(player: player)
                    .tabItem {
                        Label("Overview", systemImage: "info.circle")
                    }
                    .tag(0)
                
                // Equipment Tab
                PlayerEquipmentTab(player: player)
                    .tabItem {
                        Label("Equipment", systemImage: "shield")
                    }
                    .tag(1)
                
                // Skills Tab
                PlayerSkillsTab(player: player)
                    .tabItem {
                        Label("Skills", systemImage: "star")
                    }
                    .tag(2)
                
                // Properties Tab
                PlayerPropertiesTab(player: player)
                    .tabItem {
                        Label("Properties", systemImage: "doc.text")
                    }
                    .tag(3)
            }
            .frame(minWidth: 600, minHeight: 500)
            .toolbar {
                ToolbarItem(placement: .automatic) {
                    Menu("Actions") {
                        Button {
                            Task {
                                await appState.kickPlayer(serial: player.serial)
                            }
                        } label: {
                            Label("Kick Player", systemImage: "person.fill.xmark")
                        }
                        
                        Button(role: .destructive) {
                            Task {
                                await appState.banPlayer(serial: player.serial)
                            }
                        } label: {
                            Label("Ban Account", systemImage: "hand.raised.fill")
                        }
                    }
                }
            }
            .navigationTitle(player.name)
            .navigationSubtitle(player.account)
        }
    }
}

// MARK: - Player Overview Tab

struct PlayerOverviewTab: View {
    let player: Player
    
    var body: some View {
        Form {
            Section("Character Information") {
                LabeledContent("Name", value: player.name)
                LabeledContent("Access Level", value: AccessLevel(rawValue: player.accessLevel)?.displayName ?? "Unknown")
                LabeledContent("Location", value: player.location)
                LabeledContent("Map", value: player.map)
                LabeledContent("Playtime", value: formatPlaytime(player.playtime))
            }
            
            Section("Account") {
                LabeledContent("Account", value: player.account)
            }
            
            Section("Status") {
                LabeledContent("Hidden", value: player.isHidden ? "Yes" : "No")
                LabeledContent("Squelched", value: player.isSquelched ? "Yes" : "No")
                LabeledContent("Jailed", value: player.isJailed ? "Yes" : "No")
            }
        }
        .formStyle(.grouped)
    }
    
    private func formatPlaytime(_ seconds: Double) -> String {
        let hours = Int(seconds) / 3600
        let minutes = (Int(seconds) % 3600) / 60
        return "\(hours) hours, \(minutes) minutes"
    }
}

// MARK: - Player Equipment Tab

struct PlayerEquipmentTab: View {
    @EnvironmentObject var appState: AppState
    let player: Player
    
    var body: some View {
        VStack {
            if appState.playerEquipment.isEmpty {
                ContentUnavailableView(
                    "Loading Equipment",
                    systemImage: "shield",
                    description: Text("Loading player equipment...")
                )
                .task {
                    await appState.loadPlayerEquipment(serial: player.serial)
                }
            } else {
                List(appState.playerEquipment) { item in
                    HStack {
                        Image(systemName: itemIcon(for: item.name))
                            .foregroundColor(.blue)
                        
                        VStack(alignment: .leading) {
                            Text(item.name)
                                .font(.headline)
                            
                            if item.amount > 1 {
                                Text("x\(item.amount)")
                                    .font(.caption)
                                    .foregroundColor(.secondary)
                            }
                        }
                        
                        Spacer()
                        
                        if !item.layer.isEmpty {
                            Text(item.layer)
                                .font(.caption)
                                .foregroundColor(.secondary)
                                .padding(.horizontal, 8)
                                .padding(.vertical, 4)
                                .background(Color.secondary.opacity(0.2))
                                .cornerRadius(8)
                        }
                    }
                    .padding(.vertical, 4)
                }
            }
        }
    }
    
    private func itemIcon(for name: String) -> String {
        // Map common item types to appropriate SF Symbols
        let lowerName = name.lowercased()
        
        // Weapons
        if lowerName.contains("sword") || lowerName.contains("blade") {
            return "sword"
        }
        if lowerName.contains("axe") {
            return "axe"
        }
        if lowerName.contains("bow") || lowerName.contains("crossbow") {
            return "bow"
        }
        if lowerName.contains("staff") || lowerName.contains("stick") {
            return "cane"
        }
        if lowerName.contains("dagger") || lowerName.contains("knife") {
            return "knife"
        }
        
        // Armor
        if lowerName.contains("helm") || lowerName.contains("hat") || lowerName.contains("cap") {
            return "hat.fill"
        }
        if lowerName.contains("shield") {
            return "shield.fill"
        }
        if lowerName.contains("armor") || lowerName.contains("chest") || lowerName.contains("robe") {
            return "vest.fill"
        }
        if lowerName.contains("glove") || lowerName.contains("hand") {
            return "hand.raised.fill"
        }
        if lowerName.contains("boot") || lowerName.contains("shoe") || lowerName.contains("feet") {
            return "shoe.fill"
        }
        if lowerName.contains("leg") || lowerName.contains("pants") {
            return "figure.stand"
        }
        
        // Jewelry
        if lowerName.contains("ring") {
            return "circle.fill"
        }
        if lowerName.contains("bracelet") || lowerName.contains("necklace") || lowerName.contains("amulet") {
            return "chain"
        }
        if lowerName.contains("earring") {
            return "circle"
        }
        
        // Consumables
        if lowerName.contains("potion") || lowerName.contains("bottle") {
            return "potion.fill"
        }
        if lowerName.contains("scroll") {
            return "doc.text.fill"
        }
        if lowerName.contains("food") || lowerName.contains("meal") {
            return "fork.knife"
        }
        
        // Resources
        if lowerName.contains("gold") || lowerName.contains("coin") {
            return "dollarsign.circle.fill"
        }
        if lowerName.contains("gem") || lowerName.contains("crystal") {
            return "gemstone.fill"
        }
        
        // Default
        return "cube.fill"
    }
}

// MARK: - Player Skills Tab

struct PlayerSkillsTab: View {
    @EnvironmentObject var appState: AppState
    let player: Player
    
    var body: some View {
        VStack {
            if appState.playerSkills.isEmpty {
                ContentUnavailableView(
                    "Loading Skills",
                    systemImage: "star",
                    description: Text("Loading player skills...")
                )
                .task {
                    await appState.loadPlayerSkills(serial: player.serial)
                }
            } else {
                List(appState.playerSkills.sorted { $0.value > $1.value }) { skill in
                    HStack {
                        Text(skill.name)
                            .font(.headline)
                        
                        Spacer()
                        
                        VStack(alignment: .trailing) {
                            Text(String(format: "%.1f", skill.value))
                                .fontWeight(.semibold)
                            
                            if skill.base != skill.value {
                                Text(String(format: "+%.1f", skill.value - skill.base))
                                    .font(.caption)
                                    .foregroundColor(.green)
                            }
                        }
                        
                        Text("/ \(skill.cap)")
                            .foregroundColor(.secondary)
                    }
                    .padding(.vertical, 4)
                }
            }
        }
    }
}

// MARK: - Player Properties Tab

struct PlayerPropertiesTab: View {
    let player: Player
    
    var body: some View {
        Form {
            Section("Character") {
                LabeledContent("Name", value: player.name)
                LabeledContent("Serial", value: String(format: "0x%08X", player.serial))
                LabeledContent("Access Level", value: AccessLevel(rawValue: player.accessLevel)?.displayName ?? "Unknown")
            }
            
            Section("Location") {
                LabeledContent("Coordinates", value: player.location)
                LabeledContent("Map", value: player.map)
            }
            
            Section("Account") {
                LabeledContent("Account", value: player.account)
                LabeledContent("Playtime", value: formatPlaytime(player.playtime))
            }
            
            Section("Status Flags") {
                LabeledContent("Hidden", value: player.isHidden ? "Yes" : "No")
                LabeledContent("Squelched", value: player.isSquelched ? "Yes" : "No")
                LabeledContent("Jailed", value: player.isJailed ? "Yes" : "No")
            }
            
            Section("Note") {
                Text("Detailed properties (hits, stamina, mana, stats) require in-game inspection via the [Props command.")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
        }
        .formStyle(.grouped)
    }
    
    private func formatPlaytime(_ seconds: Double) -> String {
        let hours = Int(seconds) / 3600
        let minutes = (Int(seconds) % 3600) / 60
        return "\(hours)h \(minutes)m"
    }
}

// MARK: - Helper Components

struct LocationCell: View {
    let location: String
    
    var body: some View {
        let text = Text(location)
            .font(.system(.body, design: .monospaced))
            .foregroundColor(.secondary)
        text
    }
}

struct PlayerNameCell: View {
    let player: Player
    
    var body: some View {
        let hstack = HStack {
            Circle()
                .fill(accessLevelColor(player.accessLevel))
                .frame(width: 8, height: 8)
            
            Text(player.name)
            
            if player.isHidden {
                Image(systemName: "eye.slash")
                    .foregroundColor(.secondary)
                    .font(.caption)
            }
            
            if player.isJailed {
                Image(systemName: "lock.fill")
                    .foregroundColor(.orange)
                    .font(.caption)
            }
            
            if player.isSquelched {
                Image(systemName: "speaker.slash")
                    .foregroundColor(.red)
                    .font(.caption)
            }
        }
        hstack
    }
    
    private func accessLevelColor(_ level: Int) -> Color {
        switch level {
        case 0: return .blue
        case 1: return .green
        case 2: return .purple
        case 3: return .cyan
        case 4: return .red
        case 5: return .orange
        case 6: return .yellow
        default: return .gray
        }
    }
}

struct PlayerRow: View {
    let player: Player
    
    var body: some View {
        HStack {
            PlayerNameCell(player: player)
            Spacer()
            AccountCell(account: player.account)
            LocationCell(location: player.location)
            MapCell(map: player.map)
            PlaytimeCell(playtime: player.playtime)
            AccessLevelCell(accessLevel: player.accessLevel)
        }
        .padding(.vertical, 4)
    }
}

struct AccountCell: View {
    let account: String
    
    var body: some View {
        Text(account)
            .foregroundColor(.secondary)
    }
}

struct MapCell: View {
    let map: String
    
    var body: some View {
        Text(map)
    }
}

struct PlaytimeCell: View {
    let playtime: Double
    
    var body: some View {
        let text = Text(formatPlaytimeValue(playtime))
            .foregroundColor(.secondary)
        text
    }
    
    private func formatPlaytimeValue(_ seconds: Double) -> String {
        let hours = Int(seconds) / 3600
        let minutes = (Int(seconds) % 3600) / 60
        if hours > 0 {
            return "\(hours)h \(minutes)m"
        }
        return "\(minutes)m"
    }
}

struct AccessLevelCell: View {
    let accessLevel: Int
    
    var body: some View {
        let levelName = AccessLevel(rawValue: accessLevel)?.displayName ?? "Unknown"
        let text = Text(levelName)
            .foregroundColor(accessLevelColor(accessLevel))
        text
    }
    
    private func accessLevelColor(_ level: Int) -> Color {
        switch level {
        case 0: return .blue
        case 1: return .green
        case 2: return .purple
        case 3: return .cyan
        case 4: return .red
        case 5: return .orange
        case 6: return .yellow
        default: return .gray
        }
    }
}
