import SwiftUI

struct AccountsView: View {
    @EnvironmentObject var appState: AppState
    @State private var searchText = ""
    @State private var selectedAccount: Account?
    
    var body: some View {
        VStack(spacing: 0) {
            // Search Bar
            HStack {
                SearchBar(text: $searchText, placeholder: "Search accounts by username...")
                    .onChange(of: searchText) { _, newValue in
                        Task {
                            await appState.searchAccounts(username: newValue)
                        }
                    }
                
                Button {
                    Task {
                        await appState.searchAccounts()
                    }
                } label: {
                    Image(systemName: "arrow.clockwise")
                }
                .buttonStyle(.borderless)
            }
            .padding()
            
            Divider()
            
            // Account List
            if appState.accounts.isEmpty {
                ContentUnavailableView(
                    "No Accounts Found",
                    systemImage: "person.text.rectangle",
                    description: Text("Search for accounts to view their details.")
                )
            } else {
                List(appState.accounts, selection: $selectedAccount) { account in
                    HStack {
                        VStack(alignment: .leading) {
                            Text(account.username)
                                .font(.headline)
                            
                            Text("Last login: \(formatDate(account.lastLogin))")
                                .font(.caption)
                                .foregroundColor(.secondary)
                        }
                        
                        Spacer()
                        
                        VStack(alignment: .trailing) {
                            Text(AccessLevel(rawValue: account.accessLevel)?.displayName ?? "Unknown")
                                .foregroundColor(accessLevelColor(account.accessLevel))
                                .font(.caption)
                            
                            if account.isBanned {
                                Label("Banned", systemImage: "hand.raised.fill")
                                    .foregroundColor(.red)
                                    .font(.caption)
                            }
                        }
                    }
                    .padding(.vertical, 4)
                }
            }
        }
        .navigationTitle("Accounts")
        .navigationSubtitle("\(appState.accounts.count) accounts")
        .task {
            if appState.accounts.isEmpty {
                await appState.searchAccounts()
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
    
    private func formatDate(_ date: Date?) -> String {
        guard let date = date else { return "Never" }
        let formatter = RelativeDateTimeFormatter()
        return formatter.localizedString(for: date, relativeTo: Date())
    }
}

struct FirewallView: View {
    @EnvironmentObject var appState: AppState
    @State private var showAddRule = false
    @State private var newRule = ""
    
    var body: some View {
        VStack(spacing: 0) {
            // Toolbar
            HStack {
                Text("Firewall Rules")
                    .font(.title2)
                    .fontWeight(.semibold)
                
                Spacer()
                
                Button {
                    showAddRule = true
                } label: {
                    Label("Add Rule", systemImage: "plus")
                }
                .buttonStyle(.bordered)
            }
            .padding()
            
            Divider()
            
            // Rules List
            if appState.firewallRules.isEmpty {
                ContentUnavailableView(
                    "No Firewall Rules",
                    systemImage: "shield",
                    description: Text("There are no firewall rules configured.")
                )
            } else {
                List(appState.firewallRules) { rule in
                    HStack {
                        Image(systemName: "shield.lefthalf.filled")
                            .foregroundColor(.blue)
                        
                        VStack(alignment: .leading) {
                            Text(rule.entry)
                                .font(.headline)
                            
                            Text("Added by \(rule.addedBy)")
                                .font(.caption)
                                .foregroundColor(.secondary)
                        }
                        
                        Spacer()
                        
                        Text(formatDate(rule.dateAdded))
                            .font(.caption)
                            .foregroundColor(.secondary)
                        
                        Button(role: .destructive) {
                            Task {
                                await appState.removeFirewallRule(entry: rule.entry)
                            }
                        } label: {
                            Image(systemName: "trash")
                        }
                        .buttonStyle(.borderless)
                    }
                    .padding(.vertical, 4)
                }
            }
        }
        .navigationTitle("Firewall")
        .navigationSubtitle("\(appState.firewallRules.count) rules")
        .task {
            if appState.firewallRules.isEmpty {
                await appState.refreshFirewallRules()
            }
        }
    }
    
    private func formatDate(_ date: Date) -> String {
        let formatter = RelativeDateTimeFormatter()
        return formatter.localizedString(for: date, relativeTo: Date())
    }
}

struct LogsView: View {
    @EnvironmentObject var appState: AppState
    @State private var logLevel: LogLevel = .all
    @State private var searchText = ""
    
    enum LogLevel: String, CaseIterable {
        case all = "All"
        case info = "Info"
        case warning = "Warning"
        case error = "Error"
        
        var apiValue: String {
            switch self {
            case .all: return "all"
            case .info: return "info"
            case .warning: return "warning"
            case .error: return "error"
            }
        }
    }
    
    var body: some View {
        VStack(spacing: 0) {
            // Filter Bar
            HStack {
                Picker("Log Level", selection: $logLevel) {
                    ForEach(LogLevel.allCases, id: \.self) { level in
                        Text(level.rawValue).tag(level)
                    }
                }
                .pickerStyle(.segmented)
                .frame(width: 300)
                .onChange(of: logLevel) { _, newValue in
                    Task {
                        await appState.refreshLogs(level: newValue.apiValue)
                    }
                }
                
                Spacer()
                
                Button {
                    Task {
                        await appState.refreshLogs(level: logLevel.apiValue)
                    }
                } label: {
                    Image(systemName: "arrow.clockwise")
                }
                .buttonStyle(.borderless)
                .disabled(appState.isRefreshing)
            }
            .padding()
            
            Divider()
            
            // Logs List
            if appState.logs.isEmpty {
                ContentUnavailableView(
                    "No Logs",
                    systemImage: "doc.text",
                    description: Text("No logs available. Click refresh to load logs.")
                )
            } else {
                ScrollView {
                    VStack(alignment: .leading, spacing: 8) {
                        ForEach(appState.logs) { log in
                            LogEntryView(
                                timestamp: log.timestamp,
                                level: log.level,
                                message: log.message
                            )
                        }
                    }
                    .padding()
                }
            }
        }
        .navigationTitle("Server Logs")
        .navigationSubtitle("Real-time server activity")
        .task {
            if appState.logs.isEmpty {
                await appState.refreshLogs()
            }
        }
    }
}

struct LogEntryView: View {
    let timestamp: Date
    let level: String
    let message: String
    
    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            Text(formatTime(timestamp))
                .font(.system(.caption, design: .monospaced))
                .foregroundColor(.secondary)
                .frame(width: 70, alignment: .trailing)
            
            Text(level.uppercased())
                .font(.caption)
                .fontWeight(.semibold)
                .foregroundColor(levelColor)
                .frame(width: 70)
            
            Text(message)
                .font(.body)
                .lineLimit(2)
            
            Spacer()
        }
        .padding(.vertical, 4)
        .padding(.horizontal, 8)
        .background(Color(nsColor: .textBackgroundColor))
        .cornerRadius(6)
    }
    
    private var levelColor: Color {
        switch level.lowercased() {
        case "warning": return .orange
        case "error": return .red
        default: return .secondary
        }
    }
    
    private func formatTime(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "HH:mm:ss"
        return formatter.string(from: date)
    }
}
