using System;
using Main.Core;
using Main.Domain;
using Main.Infrastructure;
using Main.Services;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Main.Presentation.Auth
{
    public sealed class AuthViewController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject avatarSelectionPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("Login Panel")]
        [SerializeField] private TMP_InputField loginEmailInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button goToRegisterButton;
        [SerializeField] private Button guestButton;
        [SerializeField] private Button forgotPasswordButton;
        [SerializeField] private TextMeshProUGUI loginErrorText;

        [Header("Register Panel")]
        [SerializeField] private TMP_InputField registerEmailInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private TMP_InputField registerNameInput;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button goToLoginButton;
        [SerializeField] private TextMeshProUGUI registerErrorText;

        [Header("Avatar Selection Panel")]
        [SerializeField] private Transform avatarGridContainer;
        [SerializeField] private AvatarSelectionItem avatarItemPrefab;
        [SerializeField] private Button confirmAvatarButton;
        [SerializeField] private TextMeshProUGUI selectedAvatarNameText;
        [SerializeField] private Image selectedAvatarPreview;

        [Header("Loading")]
        [SerializeField] private TextMeshProUGUI loadingText;

        [Header("Dependencies")]
        [SerializeField] private AvatarService avatarService;

        private AuthService _authService;
        private string _selectedAvatarId;
        private UserData _pendingUser;

        private void Awake()
        {
            SetupButtonListeners();
        }

        private void Start()
        {
            _authService = AuthService.Instance;
            
            if (_authService == null)
            {
                Debug.LogError("[AuthViewController] AuthService not found!");
                return;
            }

            if (_authService.IsInitialized)
            {
                OnAuthInitialized();
            }
            else
            {
                _authService.OnInitialized += OnAuthInitialized;
                ShowLoading("Initializing...");
            }
        }

        private void OnDestroy()
        {
            if (_authService != null)
            {
                _authService.OnInitialized -= OnAuthInitialized;
            }
        }

        private void SetupButtonListeners()
        {
            // Login panel
            loginButton?.onClick.AddListener(OnLoginClicked);
            goToRegisterButton?.onClick.AddListener(ShowRegisterPanel);
            guestButton?.onClick.AddListener(OnGuestClicked);
            forgotPasswordButton?.onClick.AddListener(OnForgotPasswordClicked);

            // Register panel
            registerButton?.onClick.AddListener(OnRegisterClicked);
            goToLoginButton?.onClick.AddListener(ShowLoginPanel);

            // Avatar selection
            confirmAvatarButton?.onClick.AddListener(OnConfirmAvatarClicked);
        }

        #region Initialization

        private void OnAuthInitialized()
        {
            Debug.Log("[AuthViewController] Auth initialized, checking for existing user");
            
            // ShowLoginPanel();
            // return;
            
            _authService.TryAutoSignIn(
                onSuccess: user =>
                {
                    if (user.IsGuest)
                    {
                        Debug.Log("[AuthViewController] Previous session was guest, showing login");
                        _authService.SignOut();
                        ShowLoginPanel();
                        return;
                    }
                    
                    if (user.HasAvatar)
                    {
                        GoToMap();
                    }
                    else
                    {
                        _pendingUser = user;
                        ShowAvatarSelectionPanel();
                    }
                },
                onNoUser: () =>
                {
                    ShowLoginPanel();
                }
            );
        }

        #endregion

        #region Panel Navigation

        private void ShowLoginPanel()
        {
            HideAllPanels();
            loginPanel?.SetActive(true);
            ClearLoginForm();
        }

        private void ShowRegisterPanel()
        {
            HideAllPanels();
            registerPanel?.SetActive(true);
            ClearRegisterForm();
        }

        private void ShowAvatarSelectionPanel()
        {
            HideAllPanels();
            avatarSelectionPanel?.SetActive(true);
            PopulateAvatarGrid();
        }

        private void ShowLoading(string message = "Loading...")
        {
            HideAllPanels();
            loadingPanel?.SetActive(true);
            if (loadingText != null)
                loadingText.text = message;
        }

        private void HideAllPanels()
        {
            loginPanel?.SetActive(false);
            registerPanel?.SetActive(false);
            avatarSelectionPanel?.SetActive(false);
            loadingPanel?.SetActive(false);
        }

        #endregion

        #region Login

        private void OnLoginClicked()
        {
            var email = loginEmailInput?.text?.Trim() ?? "";
            var password = loginPasswordInput?.text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowLoginError("Please enter email and password");
                return;
            }

            ShowLoading("Signing in...");
            ClearLoginError();

            _authService.SignInWithEmail(email, password,
                onSuccess: user =>
                {
                    _pendingUser = user;
                    if (user.HasAvatar)
                    {
                        GoToMap();
                    }
                    else
                    {
                        ShowAvatarSelectionPanel();
                    }
                },
                onError: error =>
                {
                    ShowLoginPanel();
                    ShowLoginError(error);
                }
            );
        }

        private void OnGuestClicked()
        {
            ShowLoading("Signing in as guest...");

            _authService.SignInAsGuest(
                onSuccess: user =>
                {
                    _pendingUser = user;
                    ShowAvatarSelectionPanel();
                },
                onError: error =>
                {
                    ShowLoginPanel();
                    ShowLoginError(error);
                }
            );
        }

        private void OnForgotPasswordClicked()
        {
            var email = loginEmailInput?.text?.Trim() ?? "";
            
            if (string.IsNullOrEmpty(email))
            {
                ShowLoginError("Please enter your email first");
                return;
            }

            ShowLoading("Sending reset email...");

            _authService.SendPasswordResetEmail(email,
                onSuccess: () =>
                {
                    ShowLoginPanel();
                    ShowLoginError("Password reset email sent!");
                },
                onError: error =>
                {
                    ShowLoginPanel();
                    ShowLoginError(error);
                }
            );
        }

        private void ShowLoginError(string message)
        {
            if (loginErrorText != null)
            {
                loginErrorText.text = message;
                loginErrorText.gameObject.SetActive(true);
            }
        }

        private void ClearLoginError()
        {
            if (loginErrorText != null)
            {
                loginErrorText.text = "";
                loginErrorText.gameObject.SetActive(false);
            }
        }

        private void ClearLoginForm()
        {
            if (loginEmailInput != null) loginEmailInput.text = "";
            if (loginPasswordInput != null) loginPasswordInput.text = "";
            ClearLoginError();
        }

        #endregion

        #region Registration

        private void OnRegisterClicked()
        {
            var email = registerEmailInput?.text?.Trim() ?? "";
            var password = registerPasswordInput?.text ?? "";
            var displayName = registerNameInput?.text?.Trim() ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowRegisterError("Please enter email and password");
                return;
            }

            if (password.Length < 6)
            {
                ShowRegisterError("Password must be at least 6 characters");
                return;
            }

            ShowLoading("Creating account...");
            ClearRegisterError();

            _authService.RegisterWithEmail(email, password, displayName,
                onSuccess: user =>
                {
                    _pendingUser = user;
                    ShowAvatarSelectionPanel();
                },
                onError: error =>
                {
                    ShowRegisterPanel();
                    ShowRegisterError(error);
                }
            );
        }

        private void ShowRegisterError(string message)
        {
            if (registerErrorText != null)
            {
                registerErrorText.text = message;
                registerErrorText.gameObject.SetActive(true);
            }
        }

        private void ClearRegisterError()
        {
            if (registerErrorText != null)
            {
                registerErrorText.text = "";
                registerErrorText.gameObject.SetActive(false);
            }
        }

        private void ClearRegisterForm()
        {
            if (registerEmailInput != null) registerEmailInput.text = "";
            if (registerPasswordInput != null) registerPasswordInput.text = "";
            if (registerNameInput != null) registerNameInput.text = "";
            ClearRegisterError();
        }

        #endregion

        #region Avatar Selection

        private void PopulateAvatarGrid()
        {
            if (avatarService == null || avatarService.Catalog == null)
            {
                Debug.LogError("[AuthViewController] AvatarService or Catalog is null!");
                return;
            }

            if (avatarGridContainer != null)
            {
                foreach (Transform child in avatarGridContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            var avatars = avatarService.GetAllAvatars();
            foreach (var avatar in avatars)
            {
                if (avatarItemPrefab != null && avatarGridContainer != null)
                {
                    var item = Instantiate(avatarItemPrefab, avatarGridContainer);
                    item.Setup(avatar, OnAvatarSelected);
                }
            }

            if (avatars.Count > 0)
            {
                OnAvatarSelected(avatars[0].Id);
            }

            UpdateConfirmButton();
        }

        private void OnAvatarSelected(string avatarId)
        {
            _selectedAvatarId = avatarId;
            
            var avatarEntry = avatarService?.GetAvatarInfo(avatarId);
            if (avatarEntry != null)
            {
                if (selectedAvatarNameText != null)
                    selectedAvatarNameText.text = avatarEntry.DisplayName;
                
                if (selectedAvatarPreview != null && avatarEntry.PreviewSprite != null)
                    selectedAvatarPreview.sprite = avatarEntry.PreviewSprite;
            }

            if (avatarGridContainer != null)
            {
                foreach (Transform child in avatarGridContainer)
                {
                    var item = child.GetComponent<AvatarSelectionItem>();
                    item?.SetSelected(item.AvatarId == avatarId);
                }
            }

            UpdateConfirmButton();
        }

        private void OnConfirmAvatarClicked()
        {
            if (string.IsNullOrEmpty(_selectedAvatarId))
            {
                Debug.LogWarning("[AuthViewController] No avatar selected!");
                return;
            }

            _authService.UpdateUserAvatar(_selectedAvatarId);
            
            Debug.Log($"[AuthViewController] Avatar selected: {_selectedAvatarId}");
            
            GoToMap();
        }

        private void UpdateConfirmButton()
        {
            if (confirmAvatarButton != null)
            {
                confirmAvatarButton.interactable = !string.IsNullOrEmpty(_selectedAvatarId);
            }
        }

        #endregion

        #region Navigation

        private void GoToMap()
        {
            Debug.Log("[AuthViewController] GoToMap called");
            
            if (GameBootstrap.Instance == null)
            {
                Debug.LogError("[AuthViewController] GameBootstrap.Instance is null!");
                return;
            }
            
            if (GameBootstrap.Instance.StateMachine == null)
            {
                Debug.LogError("[AuthViewController] StateMachine is null!");
                return;
            }
            
            Debug.Log($"[AuthViewController] Current state: {GameBootstrap.Instance.StateMachine.CurrentState}");
            GameBootstrap.Instance.StateMachine.SetState(AppState.Map);
            Debug.Log($"[AuthViewController] State set to Map, new state: {GameBootstrap.Instance.StateMachine.CurrentState}");
        }

        #endregion
    }
}
