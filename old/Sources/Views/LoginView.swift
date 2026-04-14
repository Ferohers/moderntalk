import SwiftUI

struct LoginView: View {
    @EnvironmentObject var appState: AppState
    @State private var username: String = ""
    @State private var password: String = ""
    @State private var serverURL: String = "http://localhost:8080"
    @AppStorage("savedServerURL") private var savedServerURL: String = "http://localhost:8080"
    
    var body: some View {
        ZStack {
            // Background gradient
            LinearGradient(
                colors: [Color(red: 0.1, green: 0.1, blue: 0.15), Color(red: 0.05, green: 0.05, blue: 0.1)],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
            .ignoresSafeArea()
            
            VStack(spacing: 0) {
                // Logo and Title
                VStack(spacing: 16) {
                    Image(systemName: "shield.lefthalf.filled")
                        .font(.system(size: 64))
                        .foregroundColor(.blue)
                    
                    Text("UO Commander")
                        .font(.system(size: 36, weight: .bold, design: .rounded))
                        .foregroundColor(.white)
                    
                    Text("Ultima Online Server Administration")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                }
                .padding(.bottom, 40)
                
                // Login Form
                VStack(spacing: 20) {
                    // Server URL
                    HStack {
                        Image(systemName: "server.rack")
                            .foregroundColor(.secondary)
                            .frame(width: 20)
                        
                        TextField("Server URL", text: $serverURL)
                            .textFieldStyle(.plain)
                    }
                    .padding()
                    .background(Color(nsColor: .textBackgroundColor))
                    .cornerRadius(8)
                    
                    // Username
                    HStack {
                        Image(systemName: "person")
                            .foregroundColor(.secondary)
                            .frame(width: 20)
                        
                        TextField("Username", text: $username)
                            .textFieldStyle(.plain)
                            .textContentType(.username)
                    }
                    .padding()
                    .background(Color(nsColor: .textBackgroundColor))
                    .cornerRadius(8)
                    
                    // Password
                    HStack {
                        Image(systemName: "lock")
                            .foregroundColor(.secondary)
                            .frame(width: 20)
                        
                        SecureField("Password", text: $password)
                            .textFieldStyle(.plain)
                            .textContentType(.password)
                    }
                    .padding()
                    .background(Color(nsColor: .textBackgroundColor))
                    .cornerRadius(8)
                    
                    // Error Message
                    if let error = appState.errorMessage {
                        Text(error)
                            .font(.caption)
                            .foregroundColor(.red)
                            .multilineTextAlignment(.center)
                    }
                    
                    // Login Button
                    Button(action: {
                        Task {
                            await appState.login(username: username, password: password)
                        }
                    }) {
                        if appState.isLoading {
                            ProgressView()
                                .progressViewStyle(.circular)
                                .scaleEffect(0.8)
                        } else {
                            Text("Login")
                                .fontWeight(.semibold)
                        }
                    }
                    .buttonStyle(.borderedProminent)
                    .controlSize(.large)
                    .frame(maxWidth: .infinity)
                    .disabled(appState.isLoading || username.isEmpty || password.isEmpty)
                }
                .padding(.horizontal, 40)
                
                Spacer()
                
                // Footer
                Text("Requires GameMaster access level or higher")
                    .font(.caption)
                    .foregroundColor(.secondary)
                    .padding(.bottom, 20)
            }
            .padding(40)
        }
        .frame(width: 450, height: 550)
        .onAppear {
            serverURL = savedServerURL
        }
    }
}

#Preview {
    LoginView()
        .environmentObject(AppState())
}
