using System;
using UnityEngine;
using ReadyPlayerMe.Samples.AvatarCreatorWizard;
using ReadyPlayerMe.Core;
using ReadyPlayerMe.AvatarCreator;

namespace Main.Services
{
    public sealed class ReadyPlayerMeService : MonoBehaviour
    {

        [SerializeField] private AvatarCreatorStateMachine avatarCreatorStateMachine;
        [SerializeField] private AvatarConfig inGameConfig;

        private AvatarObjectLoader avatarObjectLoader;

        public event Action<Domain.AvatarData> OnAvatarLoaded;

        void OnEnable()
        {
            avatarCreatorStateMachine.AvatarSaved += OnAvatarSaved;
        }

        void OnDisable()
        {
            avatarCreatorStateMachine.AvatarSaved -= OnAvatarSaved;
            avatarObjectLoader?.Cancel();
        }

        private void OnAvatarSaved(string userEmail, 
        string userName, 
        string userId, 
        string userToken, 
        string avatarId, 
        string partner, 
        bool isExistingAvatar, 
        BodyType bodyType, 
        OutfitGender gender)
        {
            avatarCreatorStateMachine.gameObject.SetActive(false);

            var startTime = Time.time;
            avatarObjectLoader = new AvatarObjectLoader();
            avatarObjectLoader.AvatarConfig = inGameConfig;
            avatarObjectLoader.OnCompleted += (sender, args) =>
            {
                AvatarAnimationHelper.SetupAnimator(args.Metadata, args.Avatar);
                DebugPanel.AddLogWithDuration("Created avatar loaded", Time.time - startTime);
            };

            avatarObjectLoader.LoadAvatar($"{Env.RPM_MODELS_BASE_URL}/{avatarId}.glb");
        }
    }
}