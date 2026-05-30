using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NavAR.Presentation.Controllers
{
    public sealed class FeedbackController : MonoBehaviour
    {
        [Header("Route Labels")]
        [SerializeField] private TextMeshProUGUI startLocationText;
        [SerializeField] private TextMeshProUGUI destinationText;

        [Header("Rating")]
        [SerializeField] private Image[] starImages = new Image[5];
        [SerializeField] private bool autoWireStarButtons = true;
        [SerializeField] private Color filledStarColor = new Color(1f, 0.76f, 0.16f, 1f);
        [SerializeField] private Color emptyStarColor = new Color(1f, 1f, 1f, 0.35f);

        [Header("Events")]
        [SerializeField] private UnityEvent<int> ratingSubmitted;

        public int CurrentRating { get; private set; }
        public string StartLocationName { get; private set; }
        public string DestinationName { get; private set; }

        private void Awake()
        {
            WireStarButtons();
            RefreshStars();
        }

        public void OpenFeedbackScreen(string startName, string endName)
        {
            StartLocationName = string.IsNullOrWhiteSpace(startName) ? "Unknown start" : startName;
            DestinationName = string.IsNullOrWhiteSpace(endName) ? "Unknown destination" : endName;
            CurrentRating = 0;

            if (startLocationText != null)
            {
                startLocationText.text = StartLocationName;
            }

            if (destinationText != null)
            {
                destinationText.text = DestinationName;
            }

            RefreshStars();
            gameObject.SetActive(true);
        }

        public void SetRating(int rating)
        {
            CurrentRating = Mathf.Clamp(rating, 1, starImages != null ? starImages.Length : 5);
            RefreshStars();
        }

        public void SubmitRating()
        {
            ratingSubmitted?.Invoke(CurrentRating);
            CloseFeedbackScreen();
        }

        public void CloseFeedbackScreen()
        {
            gameObject.SetActive(false);
        }

        private void RefreshStars()
        {
            if (starImages == null)
            {
                return;
            }

            for (var i = 0; i < starImages.Length; i++)
            {
                var star = starImages[i];
                if (star == null)
                {
                    continue;
                }

                var isFilled = i < CurrentRating;
                star.color = isFilled ? filledStarColor : emptyStarColor;

                if (star.type == Image.Type.Filled)
                {
                    star.fillAmount = isFilled ? 1f : 0f;
                }
            }
        }

        private void WireStarButtons()
        {
            if (!autoWireStarButtons || starImages == null)
            {
                return;
            }

            for (var i = 0; i < starImages.Length; i++)
            {
                var star = starImages[i];
                if (star == null)
                {
                    continue;
                }

                var button = star.GetComponent<Button>();
                if (button == null)
                {
                    continue;
                }

                var rating = i + 1;
                button.onClick.AddListener(() => SetRating(rating));
            }
        }
    }
}
