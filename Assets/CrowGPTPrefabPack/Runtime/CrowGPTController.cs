using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrowGPT
{
    public sealed class CrowGPTController : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private TMP_Text speechText;
        [SerializeField] private TMP_Text selectedQuestionLabel;
        [SerializeField] private GameObject smile;

        [Header("Controls")]
        [SerializeField] private Button[] questionButtons;
        [SerializeField] private Button replayButton;
        [SerializeField] private Image replayBackground;
        [SerializeField] private Image replayIcon;
        [SerializeField] private Sprite replayEnabledSprite;
        [SerializeField] private Sprite replayDisabledSprite;
        [SerializeField] private Sprite arrowEnabledSprite;
        [SerializeField] private Sprite arrowDisabledSprite;

        [Header("Menu")]
        [SerializeField] private GameObject drawer;
        [SerializeField] private GameObject scrim;

        private readonly string[] questions =
        {
            "Who are you?",
            "What is an LLM?",
            "Why are you a crow?"
        };

        private readonly string[] answers =
        {
            "I’m CrowGPT — your curious, feathered guide. I’m here to help you explore ideas and find clear answers.",
            "An LLM is a large language model. It learns patterns from lots of text so it can understand and create language.",
            "Crows are clever, curious, and great problem-solvers. That sounded like the perfect personality for me!"
        };

        private int selectedIndex = -1;
        private Coroutine responseRoutine;

        private void Awake()
        {
            BeginAgain();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                SelectQuestion1();
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                SelectQuestion2();
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                SelectQuestion3();
        }

        public void SelectQuestion1() => SelectQuestion(0);
        public void SelectQuestion2() => SelectQuestion(1);
        public void SelectQuestion3() => SelectQuestion(2);

        public void ReplaySelected()
        {
            if (selectedIndex >= 0)
                SelectQuestion(selectedIndex);
        }

        public void BeginAgain()
        {
            if (responseRoutine != null)
                StopCoroutine(responseRoutine);
            responseRoutine = null;
            selectedIndex = -1;
            speechText.text =
                "Hey there! My name is <color=#C95333>CrowGPT.</color>\nAsk me anything!";
            selectedQuestionLabel.text = "Ask CrowGPT";
            selectedQuestionLabel.color = new Color32(103, 109, 103, 255);
            smile.SetActive(true);
            SetThinking(false);
            CloseMenu();
        }

        public void OpenMenu()
        {
            scrim.SetActive(true);
            drawer.SetActive(true);
            drawer.transform.SetAsLastSibling();
        }

        public void CloseMenu()
        {
            if (drawer != null)
                drawer.SetActive(false);
            if (scrim != null)
                scrim.SetActive(false);
        }

        private void SelectQuestion(int index)
        {
            if (responseRoutine != null)
                return;
            selectedIndex = index;
            selectedQuestionLabel.text = questions[index];
            selectedQuestionLabel.color = new Color32(32, 37, 33, 255);
            responseRoutine = StartCoroutine(Respond(index));
        }

        private IEnumerator Respond(int index)
        {
            SetThinking(true);
            speechText.text = "Let me think…";
            smile.SetActive(false);
            yield return new WaitForSeconds(0.65f);
            speechText.text = answers[index];
            smile.SetActive(true);
            SetThinking(false);
            responseRoutine = null;
        }

        private void SetThinking(bool thinking)
        {
            foreach (Button button in questionButtons)
                button.interactable = !thinking;

            bool replayEnabled = selectedIndex >= 0 && !thinking;
            replayButton.interactable = replayEnabled;
            replayBackground.sprite =
                replayEnabled ? replayEnabledSprite : replayDisabledSprite;
            replayIcon.sprite = replayEnabled ? arrowEnabledSprite : arrowDisabledSprite;
        }
    }
}
