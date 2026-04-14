import SwiftUI

struct ServerControlView: View {
    @EnvironmentObject var appState: AppState
    @State private var showBroadcastDialog = false
    @State private var showRestartDialog = false
    @State private var showShutdownConfirm = false
    @State private var restartDelay = 60
    @State private var saveBeforeAction = true
    
    var body: some View {
        ScrollView {
            VStack(spacing: 24) {
                // Server Information
                if let status = appState.serverStatus {
                    ServerInfoCard(status: status)
                }
                
                // Broadcast Section
                VStack(alignment: .leading, spacing: 12) {
                    Text("Broadcast Messages")
                        .font(.title2)
                        .fontWeight(.semibold)
                    
                    HStack(spacing: 12) {
                        Button {
                            showBroadcastDialog = true
                        } label: {
                            Label("Broadcast to All", systemImage: "speaker.wave.3.fill")
                                .frame(maxWidth: .infinity)
                        }
                        .buttonStyle(.borderedProminent)
                        .controlSize(.large)
                        
                        Button {
                            Task {
                                await appState.broadcast(message: "Staff meeting in 5 minutes.", staffOnly: true)
                            }
                        } label: {
                            Label("Staff Message", systemImage: "person.2.fill")
                                .frame(maxWidth: .infinity)
                        }
                        .buttonStyle(.bordered)
                        .controlSize(.large)
                    }
                }
                .padding()
                .background(Color(nsColor: .textBackgroundColor))
                .cornerRadius(12)
                
                // World Save
                VStack(alignment: .leading, spacing: 12) {
                    Text("World Save")
                        .font(.title2)
                        .fontWeight(.semibold)
                    
                    Button {
                        Task {
                            await appState.saveWorld()
                        }
                    } label: {
                        Label("Save World Now", systemImage: "internaldrive")
                            .frame(maxWidth: .infinity)
                    }
                    .buttonStyle(.bordered)
                    .controlSize(.large)
                    
                    if let status = appState.serverStatus {
                        LabeledContent("Last Save", value: status.worldSaveStatus)
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }
                }
                .padding()
                .background(Color(nsColor: .textBackgroundColor))
                .cornerRadius(12)
                
                // Shutdown / Restart
                VStack(alignment: .leading, spacing: 12) {
                    Text("Server Control")
                        .font(.title2)
                        .fontWeight(.semibold)
                        .foregroundColor(.red)
                    
                    HStack(spacing: 12) {
                        Button {
                            showRestartDialog = true
                        } label: {
                            Label("Restart Server", systemImage: "arrow.clockwise")
                                .frame(maxWidth: .infinity)
                        }
                        .buttonStyle(.bordered)
                        .controlSize(.large)
                        .tint(.orange)
                        
                        Button {
                            showShutdownConfirm = true
                        } label: {
                            Label("Shutdown", systemImage: "power")
                                .frame(maxWidth: .infinity)
                        }
                        .buttonStyle(.bordered)
                        .controlSize(.large)
                        .tint(.red)
                    }
                    
                    Toggle("Save before action", isOn: $saveBeforeAction)
                        .toggleStyle(.switch)
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
                .padding()
                .background(Color(nsColor: .textBackgroundColor))
                .cornerRadius(12)
                
                // Server Lockdown
                if let status = appState.serverStatus {
                    VStack(alignment: .leading, spacing: 12) {
                        Text("Server Lockdown")
                            .font(.title2)
                            .fontWeight(.semibold)
                        
                        LabeledContent("Current Level", value: status.lockdownLevel)
                        
                        HStack(spacing: 8) {
                            Button("Set to GameMaster+") {
                                Task {
                                    await appState.setLockdownLevel("GameMaster")
                                }
                            }
                            .buttonStyle(.bordered)
                            .disabled(status.lockdownLevel == "GameMaster")
                            
                            Button("Set to Administrator+") {
                                Task {
                                    await appState.setLockdownLevel("Administrator")
                                }
                            }
                            .buttonStyle(.bordered)
                            .disabled(status.lockdownLevel == "Administrator")
                            
                            Button("Disable Lockdown") {
                                Task {
                                    await appState.disableLockdown()
                                }
                            }
                            .buttonStyle(.bordered)
                            .tint(.red)
                            .disabled(status.lockdownLevel == "None")
                        }
                    }
                    .padding()
                    .background(Color(nsColor: .textBackgroundColor))
                    .cornerRadius(12)
                }
                
                Spacer()
            }
            .padding()
        }
        .navigationTitle("Server Control")
        .navigationSubtitle("Manage server operations")
        .sheet(isPresented: $showBroadcastDialog) {
            BroadcastMessageView(isPresented: $showBroadcastDialog)
        }
        .sheet(isPresented: $showRestartDialog) {
            RestartCountdownView(
                isPresented: $showRestartDialog,
                defaultDelay: restartDelay,
                saveBefore: saveBeforeAction
            )
        }
        .alert("Shutdown Server?", isPresented: $showShutdownConfirm) {
            Button("Cancel", role: .cancel) { }
            Button(saveBeforeAction ? "Save & Shutdown" : "Shutdown", role: .destructive) {
                Task {
                    await appState.shutdown(save: saveBeforeAction)
                }
            }
        } message: {
            Text("Are you sure you want to shut down the server? This will disconnect all players.")
        }
    }
}

// MARK: - Server Info Card

struct ServerInfoCard: View {
    let status: ServerStatus
    
    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            HStack {
                Text("Server Status")
                    .font(.title2)
                    .fontWeight(.semibold)
                
                Spacer()
                
                HStack(spacing: 6) {
                    Circle()
                        .fill(status.isRunning ? .green : .red)
                        .frame(width: 8, height: 8)
                    
                    Text(status.isRunning ? "Online" : "Offline")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
            }
            
            Grid(alignment: .leading, horizontalSpacing: 16, verticalSpacing: 8) {
                GridRow {
                    Text("Version")
                        .foregroundColor(.secondary)
                    Text(status.version)
                }
                
                GridRow {
                    Text("Uptime")
                        .foregroundColor(.secondary)
                    Text(formatUptime(status.uptime))
                }
                
                GridRow {
                    Text("Players")
                        .foregroundColor(.secondary)
                    Text("\(status.playerCount) / \(status.maxPlayers)")
                }
                
                GridRow {
                    Text("Memory")
                        .foregroundColor(.secondary)
                    Text(formatMemory(status.memoryUsage))
                }
                
                GridRow {
                    Text("World Save")
                        .foregroundColor(.secondary)
                    Text(status.worldSaveStatus)
                }
            }
        }
        .padding()
        .background(Color(nsColor: .textBackgroundColor))
        .cornerRadius(12)
    }
    
    private func formatUptime(_ seconds: Double) -> String {
        let days = Int(seconds) / 86400
        let hours = (Int(seconds) % 86400) / 3600
        let minutes = (Int(seconds) % 3600) / 60
        if days > 0 {
            return "\(days)d \(hours)h \(minutes)m"
        }
        return String(format: "%02d:%02d:%02d", hours, minutes, (Int(seconds) % 60))
    }
    
    private func formatMemory(_ bytes: Int64) -> String {
        let mb = Double(bytes) / 1_048_576
        return String(format: "%.0f MB", mb)
    }
}

// MARK: - Broadcast Message View

struct BroadcastMessageView: View {
    @EnvironmentObject var appState: AppState
    @Binding var isPresented: Bool
    @State private var message = ""
    @State private var staffOnly = false
    @State private var isSending = false
    
    var body: some View {
        NavigationStack {
            VStack(spacing: 16) {
                TextEditor(text: $message)
                    .frame(height: 120)
                    .padding(8)
                    .background(Color(nsColor: .textBackgroundColor))
                    .cornerRadius(8)
                    .overlay(
                        RoundedRectangle(cornerRadius: 8)
                            .stroke(Color.secondary.opacity(0.3), lineWidth: 1)
                    )
                
                Toggle("Staff only", isOn: $staffOnly)
                    .toggleStyle(.switch)
                
                Spacer()
            }
            .padding()
            .navigationTitle("Broadcast Message")
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        isPresented = false
                    }
                }
                
                ToolbarItem(placement: .confirmationAction) {
                    Button("Send") {
                        Task {
                            isSending = true
                            await appState.broadcast(message: message, staffOnly: staffOnly)
                            isSending = false
                            isPresented = false
                        }
                    }
                    .buttonStyle(.borderedProminent)
                    .disabled(message.isEmpty || isSending)
                }
            }
        }
        .frame(width: 500, height: 300)
    }
}

// MARK: - Restart Countdown View

struct RestartCountdownView: View {
    @EnvironmentObject var appState: AppState
    @Binding var isPresented: Bool
    @State var defaultDelay: Int = 60
    @State var saveBefore: Bool = true
    @State private var selectedDelay = 60
    @State private var customDelay = 60
    @State private var isRestarting = false
    @State private var showCustomInput = false
    
    var body: some View {
        NavigationStack {
            VStack(spacing: 24) {
                Image(systemName: "arrow.clockwise.circle.fill")
                    .font(.system(size: 64))
                    .foregroundColor(.orange)
                
                Text("Restart Server")
                    .font(.title)
                    .fontWeight(.bold)
                
                // Delay Selection
                VStack(alignment: .leading, spacing: 12) {
                    Text("Restart Delay")
                        .font(.headline)
                    
                    VStack(spacing: 8) {
                        ForEach([30, 60, 120, 300], id: \.self) { delay in
                            DelayOptionButton(
                                label: formatDelayLabel(delay),
                                isSelected: selectedDelay == delay && !showCustomInput
                            ) {
                                selectedDelay = delay
                                showCustomInput = false
                            }
                        }
                        
                        HStack {
                            DelayOptionButton(
                                label: "Custom",
                                isSelected: showCustomInput
                            ) {
                                showCustomInput = true
                            }
                            
                            if showCustomInput {
                                TextField("Seconds", value: $customDelay, formatter: NumberFormatter())
                                    .textFieldStyle(.roundedBorder)
                                    .frame(width: 100)
                                    .onChange(of: customDelay) { _, newValue in
                                        selectedDelay = max(10, newValue)
                                    }
                            }
                        }
                    }
                }
                .padding()
                .background(Color(nsColor: .textBackgroundColor))
                .cornerRadius(12)
                
                // Save Toggle
                Toggle("Save world before restart", isOn: $saveBefore)
                    .toggleStyle(.switch)
                    .padding(.horizontal)
                
                Spacer()
            }
            .padding()
            .navigationTitle("Restart Server")
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        isPresented = false
                    }
                    .disabled(isRestarting)
                }
                
                ToolbarItem(placement: .confirmationAction) {
                    Button(isRestarting ? "Restarting..." : "Restart Now") {
                        Task {
                            isRestarting = true
                            await appState.restart(save: saveBefore, delay: selectedDelay)
                            isRestarting = false
                            isPresented = false
                        }
                    }
                    .buttonStyle(.borderedProminent)
                    .tint(.orange)
                    .disabled(isRestarting)
                }
            }
        }
        .frame(width: 500, height: 500)
    }
    
    private func formatDelayLabel(_ seconds: Int) -> String {
        switch seconds {
        case 30: return "30 seconds"
        case 60: return "1 minute"
        case 120: return "2 minutes"
        case 300: return "5 minutes"
        default: return "\(seconds) seconds"
        }
    }
}

struct DelayOptionButton: View {
    let label: String
    let isSelected: Bool
    let action: () -> Void
    
    var body: some View {
        Button(action: action) {
            Text(label)
                .fontWeight(isSelected ? .semibold : .regular)
                .frame(maxWidth: .infinity)
                .padding(.vertical, 8)
                .background(isSelected ? Color.orange.opacity(0.2) : Color.secondary.opacity(0.1))
                .cornerRadius(8)
        }
        .buttonStyle(.plain)
    }
}
