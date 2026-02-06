using System;
using Main.Infrastructure;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Main.Presentation.Auth
{
    public sealed class AvatarSelectionItem : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image previewImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedIndicator;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.greenYellow;

        public string AvatarId { get; private set; }

        private Action<string> _onSelected;
        private bool _isSelected;

        private void Awake()
        {
            selectButton?.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            selectButton?.onClick.RemoveListener(OnClicked);
        }

        public void Setup(AvatarEntry avatarEntry, Action<string> onSelected)
        {
            if (avatarEntry == null)
            {
                Debug.LogWarning("[AvatarSelectionItem] AvatarEntry is null");
                return;
            }

            AvatarId = avatarEntry.Id;
            _onSelected = onSelected;

            if (previewImage != null && avatarEntry.PreviewSprite != null)
            {
                previewImage.sprite = avatarEntry.PreviewSprite;
            }

            if (nameText != null)
            {
                nameText.text = avatarEntry.DisplayName;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(selected);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? selectedColor : normalColor;
            }
        }

        private void OnClicked()
        {
            _onSelected?.Invoke(AvatarId);
        }
    }
}
