using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Main.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Main.Services
{
    public sealed class AuthService : MonoBehaviour
    {
        public static AuthService Instance { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsAuthenticated => _auth?.CurrentUser != null;
        public FirebaseUser CurrentFirebaseUser => _auth?.CurrentUser;
        public UserData CurrentUser { get; private set; }
        public event Action OnInitialized;
        public event Action<UserData> OnSignedIn;
        public event Action OnSignedOut;
        public event Action<string> OnError;

        private FirebaseAuth _auth;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeFirebase();
        }

        private void OnDestroy()
        {
            if (_auth != null)
            {
                _auth.StateChanged -= OnAuthStateChanged;
            }
        }

        #region Initialization

        private void InitializeFirebase()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    _auth = FirebaseAuth.DefaultInstance;
                    _auth.StateChanged += OnAuthStateChanged;
                    
                    IsInitialized = true;
                    Debug.Log("[AuthService] Firebase initialized successfully");
                    OnInitialized?.Invoke();
                    
                    if (_auth.CurrentUser != null)
                    {
                        Debug.Log($"[AuthService] User already signed in: {_auth.CurrentUser.UserId}");
                        LoadCurrentUserData();
                    }
                }
                else
                {
                    var error = $"Could not resolve Firebase dependencies: {dependencyStatus}";
                    Debug.LogError($"[AuthService] {error}");
                    OnError?.Invoke(error);
                }
            });
        }

        private void OnAuthStateChanged(object sender, EventArgs e)
        {
            if (_auth.CurrentUser != null)
            {
                Debug.Log($"[AuthService] Auth state changed: User {_auth.CurrentUser.UserId}");
            }
            else
            {
                Debug.Log("[AuthService] Auth state changed: No user");
                CurrentUser = null;
            }
        }

        #endregion

        #region Registration
        public void RegisterWithEmail(string email, string password, string displayName, 
            Action<UserData> onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                onError?.Invoke("Email and password are required");
                return;
            }

            _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    onError?.Invoke("Registration was cancelled");
                    return;
                }

                if (task.IsFaulted)
                {
                    var error = GetFirebaseErrorMessage(task.Exception);
                    Debug.LogError($"[AuthService] Registration failed: {error}");
                    onError?.Invoke(error);
                    return;
                }

                var user = task.Result.User;
                Debug.Log($"[AuthService] User registered: {user.UserId}");

                if (!string.IsNullOrEmpty(displayName))
                {
                    UpdateDisplayName(user, displayName, () =>
                    {
                        var userData = CreateUserData(user, displayName, false);
                        CompleteSignIn(userData, onSuccess);
                    }, onError);
                }
                else
                {
                    var userData = CreateUserData(user, "", false);
                    CompleteSignIn(userData, onSuccess);
                }
            });
        }

        private void UpdateDisplayName(FirebaseUser user, string displayName, 
            Action onSuccess, Action<string> onError)
        {
            var profile = new UserProfile { DisplayName = displayName };
            user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogWarning($"[AuthService] Failed to update display name: {task.Exception?.Message}");
                }
                onSuccess?.Invoke();
            });
        }

        #endregion

        #region Sign In
        public void SignInWithEmail(string email, string password, 
            Action<UserData> onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                onError?.Invoke("Email and password are required");
                return;
            }

            _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    onError?.Invoke("Sign in was cancelled");
                    return;
                }

                if (task.IsFaulted)
                {
                    var error = GetFirebaseErrorMessage(task.Exception);
                    Debug.LogError($"[AuthService] Sign in failed: {error}");
                    onError?.Invoke(error);
                    return;
                }

                var user = task.Result.User;
                Debug.Log($"[AuthService] User signed in: {user.UserId}");

                var userData = CreateUserData(user, user.DisplayName, false);
                CompleteSignIn(userData, onSuccess);
            });
        }

        public void SignInAsGuest(Action<UserData> onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            _auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    onError?.Invoke("Guest sign in was cancelled");
                    return;
                }

                if (task.IsFaulted)
                {
                    var error = GetFirebaseErrorMessage(task.Exception);
                    Debug.LogError($"[AuthService] Guest sign in failed: {error}");
                    onError?.Invoke(error);
                    return;
                }

                var user = task.Result.User;
                Debug.Log($"[AuthService] Guest signed in: {user.UserId}");

                var userData = CreateUserData(user, "Guest", true);
                CompleteSignIn(userData, onSuccess);
            });
        }

        public void TryAutoSignIn(Action<UserData> onSuccess, Action onNoUser)
        {
            if (!IsInitialized)
            {
                onNoUser?.Invoke();
                return;
            }

            if (_auth.CurrentUser != null)
            {
                LoadCurrentUserData();
                if (CurrentUser != null)
                {
                    onSuccess?.Invoke(CurrentUser);
                }
                else
                {
                    var user = _auth.CurrentUser;
                    var userData = CreateUserData(user, user.DisplayName, user.IsAnonymous);
                    CompleteSignIn(userData, onSuccess);
                }
            }
            else
            {
                onNoUser?.Invoke();
            }
        }

        #endregion

        #region Sign Out
        public void SignOut()
        {
            if (_auth != null)
            {
                _auth.SignOut();
                CurrentUser = null;
                Debug.Log("[AuthService] User signed out");
                OnSignedOut?.Invoke();
            }
        }

        #endregion

        #region Password Reset
        public void SendPasswordResetEmail(string email, Action onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            if (string.IsNullOrEmpty(email))
            {
                onError?.Invoke("Email is required");
                return;
            }

            _auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    var error = GetFirebaseErrorMessage(task.Exception);
                    onError?.Invoke(error);
                    return;
                }

                Debug.Log($"[AuthService] Password reset email sent to {email}");
                onSuccess?.Invoke();
            });
        }

        #endregion

        #region User Data Management
        public void UpdateUserAvatar(string avatarId)
        {
            if (CurrentUser == null) return;
            
            CurrentUser = CurrentUser.WithAvatar(avatarId);
            Infrastructure.UserDataRepository.Save(CurrentUser);
            
            Debug.Log($"[AuthService] User avatar updated: {avatarId}");
        }

        public void UpdateUserDisplayName(string displayName, Action onSuccess = null, Action<string> onError = null)
        {
            if (CurrentUser == null || _auth.CurrentUser == null)
            {
                onError?.Invoke("No user signed in");
                return;
            }

            var profile = new UserProfile { DisplayName = displayName };
            _auth.CurrentUser.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    onError?.Invoke(GetFirebaseErrorMessage(task.Exception));
                    return;
                }

                CurrentUser = CurrentUser.WithDisplayName(displayName);
                Infrastructure.UserDataRepository.Save(CurrentUser);
                
                Debug.Log($"[AuthService] Display name updated: {displayName}");
                onSuccess?.Invoke();
            });
        }

        private void LoadCurrentUserData()
        {
            CurrentUser = Infrastructure.UserDataRepository.Load();
        }

        private UserData CreateUserData(FirebaseUser firebaseUser, string displayName, bool isGuest)
        {
            return new UserData(
                userId: firebaseUser.UserId,
                email: firebaseUser.Email ?? "",
                displayName: !string.IsNullOrEmpty(displayName) ? displayName : 
                             !string.IsNullOrEmpty(firebaseUser.DisplayName) ? firebaseUser.DisplayName : 
                             isGuest ? "Guest" : "User",
                avatarId: "", 
                isGuest: isGuest
            );
        }

        private void CompleteSignIn(UserData userData, Action<UserData> onSuccess)
        {
            CurrentUser = userData;
            Infrastructure.UserDataRepository.Save(userData);
            
            OnSignedIn?.Invoke(userData);
            onSuccess?.Invoke(userData);
        }

        #endregion

        #region Error Handling

        private string GetFirebaseErrorMessage(AggregateException exception)
        {
            if (exception == null) return "Unknown error";

            foreach (var inner in exception.Flatten().InnerExceptions)
            {
                if (inner is FirebaseException firebaseEx)
                {
                    return GetErrorMessageForCode((AuthError)firebaseEx.ErrorCode);
                }
            }

            return exception.InnerException?.Message ?? exception.Message;
        }

        private string GetErrorMessageForCode(AuthError errorCode)
        {
            return errorCode switch
            {
                AuthError.InvalidEmail => "Invalid email address",
                AuthError.WrongPassword => "Incorrect password",
                AuthError.UserNotFound => "User not found",
                AuthError.EmailAlreadyInUse => "Email already in use",
                AuthError.WeakPassword => "Password is too weak (min 6 characters)",
                AuthError.NetworkRequestFailed => "Network error. Check your connection",
                AuthError.TooManyRequests => "Too many attempts. Please try again later",
                AuthError.InvalidCredential => "Invalid credentials",
                AuthError.AccountExistsWithDifferentCredentials => "Account exists with different credentials",
                _ => $"Authentication error: {errorCode}"
            };
        }

        #endregion
    }
}
