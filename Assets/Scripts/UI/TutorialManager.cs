using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbyssalReach.Core;

public class TutorialManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private GameObject tutorialPanel;

    [Header("Question Panel Buttons")]
    [SerializeField] private Button btnYes;
    [SerializeField] private Button btnNo;

    [Header("Tutorial Panel References")]
    [SerializeField] private TextMeshProUGUI pageText;
    [SerializeField] private Button btnPrev;
    [SerializeField] private Button btnNext;
    [SerializeField] private Button btnFinish;

    [Header("Tutorial Pages")]
    [Tooltip("Escribe aqui el texto de cada pagina del tutorial.")]
    [TextArea(4, 8)]
    [SerializeField] private string[] pages;

    private int currentPage;

    private void Start()
    {
        bool isNewGame = SceneLoader.Instance != null && SceneLoader.Instance.IsNewGame;

        if (!isNewGame)
        {
            gameObject.SetActive(false);
            return;
        }

        Time.timeScale = 0f;

        questionPanel.SetActive(true);
        tutorialPanel.SetActive(false);

        btnYes.onClick.AddListener(BeginTutorial);
        btnNo.onClick.AddListener(CloseTutorial);
        btnPrev.onClick.AddListener(GoPrev);
        btnNext.onClick.AddListener(GoNext);
        btnFinish.onClick.AddListener(CloseTutorial);
    }

    private void BeginTutorial()
    {
        questionPanel.SetActive(false);
        tutorialPanel.SetActive(true);
        currentPage = 0;
        RefreshPage();
    }

    private void GoPrev()
    {
        currentPage = Mathf.Max(0, currentPage - 1);
        RefreshPage();
    }

    private void GoNext()
    {
        currentPage = Mathf.Min(pages.Length - 1, currentPage + 1);
        RefreshPage();
    }

    private void RefreshPage()
    {
        if (pages == null || pages.Length == 0)
        {
            CloseTutorial();
            return;
        }

        pageText.text = pages[currentPage];

        bool isFirst = currentPage == 0;
        bool isLast  = currentPage == pages.Length - 1;

        btnPrev.gameObject.SetActive(!isFirst);
        btnNext.gameObject.SetActive(!isLast);
        btnFinish.gameObject.SetActive(isLast);
    }

    private void CloseTutorial()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }
}
