import SwiftUI

struct MainContentView: View {
    @EnvironmentObject var appState: AppState
    @State private var selectedTab: AppTab = .dashboard
    @State private var showSettings = false
    
    enum AppTab: String, CaseIterable, Identifiable, Hashable {
        case dashboard = "Dashboard"
        case players = "Players"
        case accounts = "Accounts"
        case server = "Server"
        case firewall = "Firewall"
        case logs = "Logs"
        
        var id: String { rawValue }
        
        var icon: String {
            switch self {
            case .dashboard: return "chart.bar.fill"
            case .players: return "person.3.fill"
            case .accounts: return "person.text.rectangle.fill"
            case .server: return "server.rack"
            case .firewall: return "shield.lefthalf.filled"
            case .logs: return "doc.text.fill"
            }
        }
    }
    
    var body: some View {
        NavigationSplitView {
            // Sidebar
            List(AppTab.allCases, selection: $selectedTab) { tab in
                Label(tab.rawValue, systemImage: tab.icon)
                    .tag(tab)
            }
            .listStyle(.sidebar)
            .navigationSplitViewColumnWidth(min: 180, ideal: 200)
        } detail: {
            // Detail Content
            Group {
                switch selectedTab {
                case .dashboard:
                    DashboardView()
                case .players:
                    PlayersView()
                case .accounts:
                    AccountsView()
                case .server:
                    ServerControlView()
                case .firewall:
                    FirewallView()
                case .logs:
                    LogsView()
                }
            }
            .navigationSplitViewColumnWidth(min: 500, ideal: 700)
        }
        .toolbar {
            ToolbarItem(placement: .automatic) {
                HStack {
                    // Connection Status
                    if let status = appState.serverStatus {
                        ConnectionStatusView(isRunning: status.isRunning)
                    }
                    
                    // User Menu
                    Menu {
                        Button("Logout", action: {
                            Task {
                                await appState.logout()
                            }
                        })
                    } label: {
                        Label(appState.currentUser ?? "User", systemImage: "person.circle")
                    }
                }
            }
        }
        .task {
            await appState.verifyAuthentication()
        }
    }
}

// MARK: - Connection Status Indicator

struct ConnectionStatusView: View {
    let isRunning: Bool
    
    var body: some View {
        HStack(spacing: 6) {
            Circle()
                .fill(isRunning ? .green : .red)
                .frame(width: 8, height: 8)
            
            Text(isRunning ? "Online" : "Offline")
                .font(.caption)
                .foregroundColor(.secondary)
        }
        .padding(.horizontal, 12)
        .padding(.vertical, 6)
        .background(Color.secondary.opacity(0.1))
        .cornerRadius(12)
    }
}
