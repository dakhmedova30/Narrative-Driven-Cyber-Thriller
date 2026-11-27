using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

public class HackingQTEController : MonoBehaviour
{
    public enum HackState
    {
        Playing,
        WinProfile,
        WinReward,
        LoseLockout,
        LoseRetry,
        Finished
    }

    [Header("Five Rows - Assign in order")]
    public RectTransform[] tracks = new RectTransform[5];
    public RectTransform[] targets = new RectTransform[5];
    public RectTransform[] markers = new RectTransform[5];

    [Header("Gameplay UI")]
    public GameObject gameplayUI;
    public TMP_Text livesText;
    public TMP_Text statusText;

    [Header("Win Panels")]
    public GameObject winProfilePanel;
    public GameObject winRewardPanel;

    [Header("Lose Panels")]
    public GameObject loseLockoutPanel;
    public GameObject loseRetryPanel;

    [Header("Profile UI Text")]
    public TMP_Text profileNameText;
    public TMP_Text profileAgeText;
    public TMP_Text profileOccupationText;
    public TMP_Text profileStoryText;

    [Header("Reward UI Text")]
    public TMP_Text rewardKnowledgeText;
    public TMP_Text rewardSkillText;
    public TMP_Text rewardMoneyText;

    [Header("Lose Retry UI Text")]
    public TMP_Text loseInfoText;
    public TMP_Text loseMoneyText;
    public TMP_Text loseRetryQuestionText;

    [Header("Node Data")]
    public string personName = "Maya Chen";
    public string personAge = "34";
    public string personOccupation = "Delivery Dispatcher";
    [TextArea(3, 6)]
    public string personStory =
        "RAVEN's predictive system replaced her route planning role. " +
        "She was reassigned, underpaid, and now works unstable shifts while " +
        "the company claims the system is improving efficiency.";

    public string unlockedSkill = "Vulnerability Scanning";
    public int rewardMoney = 100;

    [Header("Game Rules")]
    public int startingLives = 4;
    public float markerSpeed = 350f;

    [Header("Events")]
    public UnityEvent onHackFinishedSuccessfully;
    public UnityEvent onHackClosedAfterFailure;

    private int currentRow;
    private int lives;
    private float markerX;
    private int direction = 1;
    private HackState currentState = HackState.Playing;

    void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        currentRow = 0;
        lives = startingLives;
        markerX = 0f;
        direction = 1;
        currentState = HackState.Playing;

        HideAllPanels();

        if (gameplayUI != null)
            gameplayUI.SetActive(true);

        UpdateUI();
    }

    void Update()
    {
        switch (currentState)
        {
            case HackState.Playing:
                UpdateGameplay();
                break;

            case HackState.WinProfile:
                if (ContinuePressed())
                {
                    ShowWinRewardPanel();
                }
                break;

            case HackState.WinReward:
                if (ContinuePressed())
                {
                    CloseAfterSuccess();
                }
                break;

            case HackState.LoseLockout:
                if (ContinuePressed())
                {
                    ShowLoseRetryPanel();
                }
                break;

            case HackState.LoseRetry:
                HandleRetryInput();
                break;
        }
    }

    void UpdateGameplay()
    {
        RectTransform track = tracks[currentRow];
        RectTransform marker = markers[currentRow];

        float limit = Mathf.Max(
            0f,
            (track.rect.width - marker.rect.width) / 2f
        );

        markerX += direction * markerSpeed * Time.deltaTime;

        if (markerX >= limit)
        {
            markerX = limit;
            direction = -1;
        }
        else if (markerX <= -limit)
        {
            markerX = -limit;
            direction = 1;
        }

        marker.anchoredPosition = new Vector2(
            markerX,
            marker.anchoredPosition.y
        );

        if (ActionPressed())
        {
            AttemptHit();
        }
    }

    void AttemptHit()
    {
        RectTransform target = targets[currentRow];

        float targetX = target.anchoredPosition.x;

        float tolerance = Mathf.Max(
            0f,
            (target.rect.width - markers[currentRow].rect.width) / 2f
        );

        bool hit = Mathf.Abs(markerX - targetX) <= tolerance;

        if (hit)
        {
            currentRow++;

            if (currentRow >= tracks.Length)
            {
                Win();
                return;
            }

            markerX = 0f;
            direction = 1;
        }
        else
        {
            lives--;

            if (lives <= 0)
            {
                Lose();
                return;
            }

            currentRow = 0;
            markerX = 0f;
            direction = 1;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (livesText != null)
            livesText.text = "LIVES: " + lives;

        if (statusText != null)
            statusText.text =
                "LINE " + (currentRow + 1) + " / " + tracks.Length +
                " - PRESS SPACE OR LEFT CLICK";

        for (int i = 0; i < markers.Length; i++)
        {
            markers[i].gameObject.SetActive(i == currentRow);

            if (i == currentRow)
            {
                markers[i].anchoredPosition = new Vector2(0f, 0f);
            }
        }
    }

    void Win()
    {
        currentState = HackState.WinProfile;

        if (gameplayUI != null)
            gameplayUI.SetActive(false);

        HideAllPanels();
        PopulateProfilePanel();

        if (winProfilePanel != null)
            winProfilePanel.SetActive(true);
    }

    void Lose()
    {
        currentState = HackState.LoseLockout;

        if (gameplayUI != null)
            gameplayUI.SetActive(false);

        HideAllPanels();

        if (loseLockoutPanel != null)
            loseLockoutPanel.SetActive(true);
    }

    void ShowWinRewardPanel()
    {
        currentState = HackState.WinReward;

        HideAllPanels();
        PopulateRewardPanel();

        if (winRewardPanel != null)
            winRewardPanel.SetActive(true);
    }

    void ShowLoseRetryPanel()
    {
        currentState = HackState.LoseRetry;

        HideAllPanels();
        PopulateLoseRetryPanel();

        if (loseRetryPanel != null)
            loseRetryPanel.SetActive(true);
    }

    void CloseAfterSuccess()
    {
        currentState = HackState.Finished;
        HideAllPanels();
        onHackFinishedSuccessfully?.Invoke();

        // Optional:
        // gameObject.SetActive(false);
    }

    void CloseAfterFailure()
    {
        currentState = HackState.Finished;
        HideAllPanels();
        onHackClosedAfterFailure?.Invoke();

        // Optional:
        // gameObject.SetActive(false);
    }

    void PopulateProfilePanel()
    {
        if (profileNameText != null)
            profileNameText.text = "Name: " + personName;

        if (profileAgeText != null)
            profileAgeText.text = "Age: " + personAge;

        if (profileOccupationText != null)
            profileOccupationText.text = "Occupation: " + personOccupation;

        if (profileStoryText != null)
            profileStoryText.text = personStory;
    }

    void PopulateRewardPanel()
    {
        if (rewardKnowledgeText != null)
            rewardKnowledgeText.text = "Knowledge Obtained";

        if (rewardSkillText != null)
            rewardSkillText.text = "New Skill Unlocked: " + unlockedSkill;

        if (rewardMoneyText != null)
            rewardMoneyText.text = "$" + rewardMoney + " Earned!";
    }

    void PopulateLoseRetryPanel()
    {
        if (loseInfoText != null)
            loseInfoText.text = "No Information Obtained";

        if (loseMoneyText != null)
            loseMoneyText.text = "$0 Earned!";

        if (loseRetryQuestionText != null)
            loseRetryQuestionText.text = "Retry?";
    }

    void HideAllPanels()
    {
        if (winProfilePanel != null) winProfilePanel.SetActive(false);
        if (winRewardPanel != null) winRewardPanel.SetActive(false);
        if (loseLockoutPanel != null) loseLockoutPanel.SetActive(false);
        if (loseRetryPanel != null) loseRetryPanel.SetActive(false);
    }

    bool ActionPressed()
    {
        bool spacePressed =
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame;

        bool mousePressed =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        return spacePressed || mousePressed;
    }

    bool ContinuePressed()
    {
        bool spacePressed =
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame;

        bool enterPressed =
            Keyboard.current != null &&
            Keyboard.current.enterKey.wasPressedThisFrame;

        bool mousePressed =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        return spacePressed || enterPressed || mousePressed;
    }
    
    void HandleRetryInput()
    {
        if (Keyboard.current == null)
            return;

        bool yesPressed =
            Keyboard.current.digit1Key.wasPressedThisFrame ||
            Keyboard.current.numpad1Key.wasPressedThisFrame;

        bool noPressed =
            Keyboard.current.digit2Key.wasPressedThisFrame ||
            Keyboard.current.numpad2Key.wasPressedThisFrame;

        if (yesPressed)
        {
            StartGame();
        }
        else if (noPressed)
        {
            CloseAfterFailure();
        }
    }

    // Optional button methods if you later use UI buttons:
    public void RetryYes()
    {
        StartGame();
    }

    public void RetryNo()
    {
        CloseAfterFailure();
    }
}