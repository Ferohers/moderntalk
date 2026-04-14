import SwiftUI

@main
struct UOCommanderApp: App {
    @StateObject private var appState = AppState()
    @AppStorage("serverURL") private var serverURL: String = "http://localhost:8080"
    
    var body: some Scene {
        WindowGroup {
            Group {
                if appState.isAuthenticated {
                    MainContentView()
                        .environmentObject(appState)
                } else {
                    LoginView()
                        .environmentObject(appState)
                }
            }
            .frame(minWidth: 1000, minHeight: 700)
        }
        .windowStyle(.hiddenTitleBar)
        .windowToolbarStyle(.unified)
        .commands {
            CommandGroup(after: .appInfo) {
                Button("Server Settings") {
                    // TODO: Implement server settings sheet
                }
                .keyboardShortcut(",", modifiers: .command)
            }
        }
    }
}
