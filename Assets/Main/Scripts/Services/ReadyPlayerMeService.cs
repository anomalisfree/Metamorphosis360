using System;
using UnityEngine;
using ReadyPlayerMe.Samples.AvatarCreatorWizard;
using ReadyPlayerMe.Core;
//using ReadyPlayerMe.AvatarCreator;
using Main.Core;
using Main.Infrastructure;

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

            var avatarData = new Domain.AvatarData(
                userId: userId,
                userEmail: userEmail,
                userName: userName,
                userToken: userToken,
                avatarId: avatarId,
                avatarPartner: partner,
                avatarIsDraft: !isExistingAvatar,
                avatarBodyType: bodyType,
                avatarOutfitGender: gender
            );

            AvatarDataRepository.Save(avatarData);

            // var startTime = Time.time;
            // avatarObjectLoader = new AvatarObjectLoader();
            // avatarObjectLoader.AvatarConfig = inGameConfig;
            // avatarObjectLoader.OnCompleted += (sender, args) =>
            // {
            //     AvatarAnimationHelper.SetupAnimator(args.Metadata, args.Avatar);
            //     DebugPanel.AddLogWithDuration("Created avatar loaded", Time.time - startTime);

            //     // Вызываем событие после полной загрузки
            //     OnAvatarLoaded?.Invoke(avatarData);
            // };

            // avatarObjectLoader.LoadAvatar($"{Env.RPM_MODELS_BASE_URL}/{avatarId}.glb");
            GameBootstrap.Instance.StateMachine.SetState(AppState.Map);
        }
    }
}