using System;
using System.Collections.Generic;
using NavAR.Core.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace NavAR.Presentation.Controllers
{
    public static class ScreenBinders
    {
        private const string CategoryActiveClass = "category-pill-active";
        private const string FeedbackStarOnClass = "feedback-star-on";
        private const string FeedbackChipActiveClass = "feedback-chip-active";

        public sealed class DestinationFilterState
        {
            public string Category { get; set; } = "All";
            public string SearchText { get; set; } = string.Empty;
        }

        public static DestinationFilterState DestinationFilter { get; } = new DestinationFilterState();

        public sealed class FeedbackState
        {
            public int Rating { get; set; }
            public HashSet<string> SelectedChips { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public string Comment { get; set; } = string.Empty;
        }

        public static FeedbackState Feedback { get; } = new FeedbackState();

        public sealed class SettingsState
        {
            public bool VoiceGuidanceEnabled { get; set; } = true;
            public bool HighContrastEnabled { get; set; }
            public bool ElevatorRoutingEnabled { get; set; } = true;
            public int TextScalePercent { get; set; } = 115;
        }

        public static SettingsState Settings { get; } = new SettingsState();

        public static void WireHome(
            VisualElement content,
            Action<AppState> setState,
            Action onViewHelp,
            Action onLaunchTutorial,
            Action onOutdoorNavigation
        )
        {
            var startBtn = content.Q<Button>("BtnStartNavigation");
            var outdoorButton = content.Q<Button>("BtnOutdoorNavigation");
            var helpButton = content.Q<Button>("BtnViewHelp");
            var tutorialButton = content.Q<Button>("BtnLaunchTutorial");

            if (startBtn != null)
            {
                startBtn.clicked += () => setState(AppState.Explore); // Changed from QrScanning to Explore/DestinationSelection
            }

            if (outdoorButton != null)
            {
                outdoorButton.RegisterCallback<PointerUpEvent>(_ => onOutdoorNavigation?.Invoke());
                outdoorButton.clicked += () => onOutdoorNavigation?.Invoke();
            }

            if (helpButton != null)
            {
                helpButton.clicked += () => onViewHelp?.Invoke();
            }

            if (tutorialButton != null)
            {
                tutorialButton.clicked += () => onLaunchTutorial?.Invoke();
            }
        }

        public static void WireExplore(VisualElement content, Action<AppState> setState)
        {
            var searchInput = content.Q<TextField>("SearchInput");
            var categoryAll = content.Q<Button>("BtnCategoryAll");
            var categoryOffices = content.Q<Button>("BtnCategoryOffices");
            var categoryLabs = content.Q<Button>("BtnCategoryLabs");
            var categoryRestrooms = content.Q<Button>("BtnCategoryRestrooms");
            var clearButton = content.Q<Button>("BtnClearRecent");
            var quickPrint = content.Q<Button>("BtnQuickPrint");
            var quickExit = content.Q<Button>("BtnQuickExit");
            var backButton = content.Q<Button>("BackButton");

            var categoryButtons = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase)
            {
                { "All", categoryAll },
                { "Offices", categoryOffices },
                { "Labs", categoryLabs },
                { "Restrooms", categoryRestrooms }
            };

            if (searchInput != null)
            {
                if (!string.IsNullOrWhiteSpace(DestinationFilter.SearchText))
                {
                    searchInput.SetValueWithoutNotify(DestinationFilter.SearchText);
                }

                searchInput.RegisterValueChangedCallback(evt =>
                {
                    DestinationFilter.SearchText = evt.newValue ?? string.Empty;
                });
            }

            var activeCategory = ResolveActiveCategory(categoryButtons, DestinationFilter.Category);
            DestinationFilter.Category = activeCategory;
            SetCategoryActive(categoryButtons, activeCategory);

            foreach (var pair in categoryButtons)
            {
                var categoryName = pair.Key;
                var button = pair.Value;
                if (button == null)
                {
                    continue;
                }

                button.clicked += () =>
                {
                    DestinationFilter.Category = categoryName;
                    SetCategoryActive(categoryButtons, categoryName);
                };
            }

            if (clearButton != null)
            {
                clearButton.clicked += () =>
                {
                    var listContainer = content.Q<ScrollView>("DestinationListContainer");
                    listContainer?.Clear();
                };
            }

            if (quickPrint != null)
            {
                quickPrint.clicked += () => setState(AppState.Navigating);
            }

            if (quickExit != null)
            {
                quickExit.clicked += () => setState(AppState.Navigating);
            }

            if (backButton != null)
            {
                backButton.clicked += () => setState(AppState.Home);
            }
        }

        private static string ResolveActiveCategory(Dictionary<string, Button> categoryButtons, string preferredCategory)
        {
            if (!string.IsNullOrWhiteSpace(preferredCategory)
                && categoryButtons.TryGetValue(preferredCategory, out var preferredButton)
                && preferredButton != null)
            {
                return preferredCategory;
            }

            foreach (var pair in categoryButtons)
            {
                if (pair.Value != null && pair.Value.ClassListContains(CategoryActiveClass))
                {
                    return pair.Key;
                }
            }

            return "All";
        }

        private static void SetCategoryActive(Dictionary<string, Button> categoryButtons, string activeCategory)
        {
            foreach (var pair in categoryButtons)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                if (string.Equals(pair.Key, activeCategory, StringComparison.OrdinalIgnoreCase))
                {
                    pair.Value.AddToClassList(CategoryActiveClass);
                }
                else
                {
                    pair.Value.RemoveFromClassList(CategoryActiveClass);
                }
            }
        }

        public static void WireQrScanner(VisualElement content, Action<AppState> setState)
        {
            var closeButton = content.Q<Button>("BtnCloseScanner");
            if (closeButton != null)
            {
                closeButton.clicked += () => setState(AppState.Home);
            }
        }

        public static void WirePermission(
            VisualElement content,
            Action<AppState> setState,
            Action requestPermissionAction
        )
        {
            var allowButton = content.Q<Button>("AllowButton");
            var cancelButton = content.Q<Button>("CancelButton");
            var backButton = content.Q<Button>("BtnPermissionBack");

            if (allowButton != null)
            {
                allowButton.clicked += () =>
                {
                    requestPermissionAction?.Invoke();
                    setState(AppState.QrScanning);
                };
            }

            if (cancelButton != null)
            {
                cancelButton.clicked += () => setState(AppState.Home);
            }

            if (backButton != null)
            {
                backButton.clicked += () => setState(AppState.Home);
            }
        }

        public static void WireArNavigation(
            VisualElement content,
            Action<AppState> setState,
            Func<AppState> getLastNonOverlayState,
            Action onToggleVoice,
            Action onOpenMap,
            Action onEndNavigation
        )
        {
            var backButton = content.Q<Button>("BtnArBack");
            var rescanButton = content.Q<Button>("BtnRescan");
            var endButton = content.Q<Button>("BtnEnd");
            var helpButton = content.Q<Button>("BtnHelp");
            var audioButton = content.Q<Button>("BtnAudio");
            var mapButton = content.Q<Button>("BtnMap");

            if (backButton != null)
            {
                backButton.clicked += () => setState(getLastNonOverlayState());
            }

            if (rescanButton != null)
            {
                rescanButton.clicked += () => setState(AppState.QrScanning);
            }

            if (endButton != null)
            {
                endButton.clicked += () =>
                {
                    if (onEndNavigation != null)
                    {
                        onEndNavigation();
                    }
                    else
                    {
                        setState(AppState.Feedback);
                    }
                };
            }

            if (helpButton != null)
            {
                helpButton.clicked += () => setState(AppState.Feedback);
            }

            if (audioButton != null)
            {
                audioButton.clicked += () => onToggleVoice?.Invoke();
            }

            if (mapButton != null)
            {
                mapButton.clicked += () => onOpenMap?.Invoke();
            }
        }

        public static void SetArInstruction(VisualElement content, string text)
        {
            var instructionLabel = content?.Q<Label>("InstructionText");
            if (instructionLabel == null)
            {
                return;
            }

            instructionLabel.text = string.IsNullOrWhiteSpace(text) ? "Continue." : text;
        }

        public static void SetArTargetName(VisualElement content, string targetName)
        {
            var targetLabel = content?.Q<Label>("TargetNameText");
            if (targetLabel == null)
            {
                return;
            }

            targetLabel.text = string.IsNullOrWhiteSpace(targetName) ? "Destination" : targetName;
        }

        public static void WirePositionLost(VisualElement content, Action<AppState> setState)
        {
            var scanButton = content.Q<Button>("BtnScanRecovery");
            var resumeButton = content.Q<Button>("BtnResumeNavigation");
            var cancelButton = content.Q<Button>("BtnCancelRecovery");

            if (scanButton != null)
            {
                scanButton.clicked += () => setState(AppState.QrScanning);
            }

            if (resumeButton != null)
            {
                resumeButton.clicked += () => setState(AppState.Navigating);
            }

            if (cancelButton != null)
            {
                cancelButton.clicked += () => setState(AppState.Home);
            }
        }

        public static void WireFeedback(
            VisualElement content,
            Action<AppState> setState,
            Func<AppState> getLastNonOverlayState,
            Action onSubmitFeedback
        )
        {
            var backButton = content.Q<Button>("BtnBackFeedback");
            var submitButton = content.Q<Button>("BtnSubmitFeedback");
            var commentInput = content.Q<TextField>("FeedbackCommentInput");

            var starButtons = new List<Button>
            {
                content.Q<Button>("Star1"),
                content.Q<Button>("Star2"),
                content.Q<Button>("Star3"),
                content.Q<Button>("Star4"),
                content.Q<Button>("Star5")
            };

            var chipButtons = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase)
            {
                { "wrong_direction", content.Q<Button>("ChipWrongDirection") },
                { "ar_drift", content.Q<Button>("ChipArDrift") },
                { "map_inaccuracy", content.Q<Button>("ChipMapInaccuracy") },
                { "lagging_view", content.Q<Button>("ChipLaggingView") },
                { "poor_lighting", content.Q<Button>("ChipPoorLighting") },
                { "other", content.Q<Button>("ChipOther") }
            };

            if (Feedback.Rating <= 0)
            {
                Feedback.Rating = ResolveStarRating(starButtons);
            }

            SetStarRating(starButtons, Feedback.Rating);

            if (Feedback.SelectedChips.Count == 0)
            {
                foreach (var pair in chipButtons)
                {
                    if (pair.Value != null && pair.Value.ClassListContains(FeedbackChipActiveClass))
                    {
                        Feedback.SelectedChips.Add(pair.Key);
                    }
                }
            }

            SetChipSelection(chipButtons, Feedback.SelectedChips);

            if (commentInput != null)
            {
                if (!string.IsNullOrWhiteSpace(Feedback.Comment))
                {
                    commentInput.SetValueWithoutNotify(Feedback.Comment);
                }

                commentInput.RegisterValueChangedCallback(evt =>
                {
                    Feedback.Comment = evt.newValue ?? string.Empty;
                });
            }

            for (var i = 0; i < starButtons.Count; i++)
            {
                var ratingValue = i + 1;
                var button = starButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.clicked += () =>
                {
                    Feedback.Rating = ratingValue;
                    SetStarRating(starButtons, ratingValue);
                };
            }

            foreach (var pair in chipButtons)
            {
                var chipKey = pair.Key;
                var button = pair.Value;
                if (button == null)
                {
                    continue;
                }

                button.clicked += () =>
                {
                    if (Feedback.SelectedChips.Contains(chipKey))
                    {
                        Feedback.SelectedChips.Remove(chipKey);
                    }
                    else
                    {
                        Feedback.SelectedChips.Add(chipKey);
                    }

                    SetChipSelection(chipButtons, Feedback.SelectedChips);
                };
            }

            if (backButton != null)
            {
                backButton.clicked += () => setState(getLastNonOverlayState());
            }

            if (submitButton != null)
            {
                submitButton.clicked += () =>
                {
                    onSubmitFeedback?.Invoke();
                    setState(AppState.Home);
                };
            }
        }

        private static int ResolveStarRating(IReadOnlyList<Button> starButtons)
        {
            var rating = 0;
            foreach (var button in starButtons)
            {
                if (button != null && button.ClassListContains(FeedbackStarOnClass))
                {
                    rating++;
                }
            }

            return rating;
        }

        private static void SetStarRating(IReadOnlyList<Button> starButtons, int rating)
        {
            for (var i = 0; i < starButtons.Count; i++)
            {
                var button = starButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i < rating)
                {
                    button.AddToClassList(FeedbackStarOnClass);
                }
                else
                {
                    button.RemoveFromClassList(FeedbackStarOnClass);
                }
            }
        }

        private static void SetChipSelection(Dictionary<string, Button> chipButtons, HashSet<string> selected)
        {
            foreach (var pair in chipButtons)
            {
                var button = pair.Value;
                if (button == null)
                {
                    continue;
                }

                if (selected.Contains(pair.Key))
                {
                    button.AddToClassList(FeedbackChipActiveClass);
                }
                else
                {
                    button.RemoveFromClassList(FeedbackChipActiveClass);
                }
            }
        }

        public static void WireSettings(
            VisualElement content,
            Action<AppState> setState,
            Action onSignOut,
            Action onAbout,
            Action onHelpCenter,
            Action<SettingsState> onSettingsChanged
        )
        {
            var slider = content.Q<SliderInt>("TextSizeSlider");
            var valueLabel = content.Q<Label>("TextSizeValueLabel");
            var signOut = content.Q<Button>("BtnSignOut");
            var helpCenter = content.Q<Button>("BtnHelpCenter");
            var about = content.Q<Button>("BtnAboutApp");
            var voiceToggle = content.Q<Toggle>("ToggleVoiceGuidance");
            var highContrastToggle = content.Q<Toggle>("ToggleHighContrast");
            var elevatorToggle = content.Q<Toggle>("ToggleElevatorRouting");

            if (voiceToggle != null)
            {
                voiceToggle.SetValueWithoutNotify(Settings.VoiceGuidanceEnabled);
                voiceToggle.RegisterValueChangedCallback(evt =>
                {
                    Settings.VoiceGuidanceEnabled = evt.newValue;
                    onSettingsChanged?.Invoke(Settings);
                });
            }

            if (highContrastToggle != null)
            {
                highContrastToggle.SetValueWithoutNotify(Settings.HighContrastEnabled);
                highContrastToggle.RegisterValueChangedCallback(evt =>
                {
                    Settings.HighContrastEnabled = evt.newValue;
                    onSettingsChanged?.Invoke(Settings);
                });
            }

            if (elevatorToggle != null)
            {
                elevatorToggle.SetValueWithoutNotify(Settings.ElevatorRoutingEnabled);
                elevatorToggle.RegisterValueChangedCallback(evt =>
                {
                    Settings.ElevatorRoutingEnabled = evt.newValue;
                    onSettingsChanged?.Invoke(Settings);
                });
            }

            if (slider != null && valueLabel != null)
            {
                slider.SetValueWithoutNotify(Settings.TextScalePercent);
                valueLabel.text = $"{Settings.TextScalePercent}%";
                slider.RegisterValueChangedCallback(evt =>
                {
                    Settings.TextScalePercent = evt.newValue;
                    valueLabel.text = $"{evt.newValue}%";
                    onSettingsChanged?.Invoke(Settings);
                });
            }

            if (signOut != null)
            {
                signOut.clicked += () => onSignOut?.Invoke();
            }

            if (helpCenter != null)
            {
                helpCenter.clicked += () => onHelpCenter?.Invoke();
            }

            if (about != null)
            {
                about.clicked += () => onAbout?.Invoke();
            }
        }

        public static void WireComingSoon(
            VisualElement content,
            Action<AppState> setState,
            Func<AppState> getLastNonOverlayState
        )
        {
            var backButton = content.Q<Button>("BtnComingSoonBack");
            var returnButton = content.Q<Button>("BtnComingSoonReturn");

            void GoBack()
            {
                if (getLastNonOverlayState != null)
                {
                    setState(getLastNonOverlayState());
                }
            }

            if (backButton != null)
            {
                backButton.clicked += GoBack;
            }

            if (returnButton != null)
            {
                returnButton.clicked += GoBack;
            }
        }
    }
}
