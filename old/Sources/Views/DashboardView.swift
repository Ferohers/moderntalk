import SwiftUI

struct DashboardView: View {
    @EnvironmentObject var appState: AppState
    @State private var showBroadcastDialog = false
    @State private var showRestartDialog = false
    
    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                // Server Status Cards
                HStack(spacing: 16) {
                    if let status = appState.serverStatus {
                        ServerStatusCard(
                            title: "Players Online",
                            value: "\(status.playerCount)",
                            subtitle: "of \(status.maxPlayers) max",
                            icon: "person.3.fill",
                            color: .blue
                        )
                        
                        ServerStatusCard(
                            title: "Uptime",
                            value: formatUptime(status.uptime),
                            subtitle: status.version,
                            icon: "clock.fill",
                            color: .green
                        )
                        
                        ServerStatusCard(
                            title: "Memory",
                            value: formatMemory(status.memoryUsage),
                            subtitle: status.worldSaveStatus,
                            icon: "memorychip",
                            color: .purple
                        )
                        
                        ServerStatusCard(
                            title: "Lockdown",
                            value: status.lockdownLevel,
                            subtitle: "Access Level",
                            icon: "lock.fill",
                            color: .orange
                        )
                    }
                }
                
                // Quick Actions
                VStack(alignment: .leading, spacing: 12) {
                    Text("Quick Actions")
                        .font(.title2)
                        .fontWeight(.semibold)
                    
                    HStack(spacing: 12) {
                        QuickActionButton(
                            title: "Save World",
                            icon: "internaldrive",
                            color: .blue
                        ) {
                            Task {
                                await appState.saveWorld()
                            }
                        }
                        
                        QuickActionButton(
                            title: "Broadcast",
                            icon: "speaker.wave.3.fill",
                            color: .green
                        ) {
                            showBroadcastDialog = true
                        }
                        
                        QuickActionButton(
                            title: "Restart",
                            icon: "arrow.clockwise",
                            color: .orange
                        ) {
                            showRestartDialog = true
                        }
                        
                        QuickActionButton(
                            title: "Shutdown",
                            icon: "power",
                            color: .red
                        ) {
                            Task {
                                await appState.shutdown(save: true)
                            }
                        }
                    }
                }
                .padding()
                .background(Color(nsColor: .textBackgroundColor))
                .cornerRadius(12)
                
                // Players Overview
                VStack(alignment: .leading, spacing: 12) {
                    HStack {
                        Text("Online Players")
                            .font(.title2)
                            .fontWeight(.semibold)
                        
                        Spacer()
                        
                        Button("Refresh") {
                            Task {
                                await appState.refreshPlayers()
                            }
                        }
                        .buttonStyle(.borderless)
                    }
                    
                    if appState.players.isEmpty {
                        Text("No players online")
                            .foregroundColor(.secondary)
                            .frame(maxWidth: .infinity, alignment: .center)
                            .padding()
                    } else {
                        ScrollView(.horizontal, showsIndicators: false) {
                            HStack(spacing: 12) {
                                ForEach(appState.players.prefix(10)) { player in
                                    PlayerQuickView(player: player)
                                }
                            }
                            .padding(.vertical, 8)
                        }
                    }
                }
                .padding()
                .background(Color(nsColor: .textBackgroundColor))
                .cornerRadius(12)
                
                Spacer()
            }
            .padding()
        }
        .task {
            if appState.serverStatus == nil {
                await appState.refreshServerStatus()
            }
        }
        .sheet(isPresented: $showBroadcastDialog) {
            BroadcastMessageView(isPresented: $showBroadcastDialog)
        }
        .sheet(isPresented: $showRestartDialog) {
            RestartCountdownView(isPresented: $showRestartDialog)
        }
    }
    
    private func formatUptime(_ seconds: Double) -> String {
        let hours = Int(seconds) / 3600
        let minutes = (Int(seconds) % 3600) / 60
        return String(format: "%02d:%02d", hours, minutes)
    }
    
    private func formatMemory(_ bytes: Int64) -> String {
        let mb = Double(bytes) / 1_048_576
        return String(format: "%.0f MB", mb)
    }
}

// MARK: - Server Status Card

struct ServerStatusCard: View {
    let title: String
    let value: String
    let subtitle: String
    let icon: String
    let color: Color
    
    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Image(systemName: icon)
                    .font(.title2)
                    .foregroundColor(color)
                
                Spacer()
            }
            
            Text(value)
                .font(.system(size: 28, weight: .bold, design: .rounded))
            
            Text(title)
                .font(.caption)
                .foregroundColor(.secondary)
            
            if !subtitle.isEmpty {
                Text(subtitle)
                    .font(.caption2)
                    .foregroundColor(.secondary)
            }
        }
        .padding()
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(Color(nsColor: .textBackgroundColor))
        .cornerRadius(12)
    }
}

// MARK: - Quick Action Button

struct QuickActionButton: View {
    let title: String
    let icon: String
    let color: Color
    let action: () -> Void
    
    var body: some View {
        Button(action: action) {
            VStack(spacing: 8) {
                Image(systemName: icon)
                    .font(.title2)
                    .foregroundColor(color)
                
                Text(title)
                    .font(.caption)
                    .fontWeight(.medium)
            }
            .frame(maxWidth: .infinity)
            .padding()
            .background(Color.secondary.opacity(0.1))
            .cornerRadius(12)
        }
        .buttonStyle(.plain)
    }
}

// MARK: - Player Quick View

struct PlayerQuickView: View {
    let player: Player
    
    var body: some View {
        VStack(spacing: 8) {
            Circle()
                .fill(accessLevelColor(player.accessLevel))
                .frame(width: 40, height: 40)
                .overlay(
                    Text(String(player.name.prefix(1)).uppercased())
                        .font(.headline)
                        .foregroundColor(.white)
                )
            
            Text(player.name)
                .font(.caption)
                .lineLimit(1)
                .frame(width: 80)
        }
        .padding()
        .background(Color(nsColor: .textBackgroundColor))
        .cornerRadius(12)
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
