using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Score")]
    [SerializeField] private int score = 0;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    private float timer = 0f;
    private bool isScoring = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateScoreUI();
    }

    private void Update()
    {
        if (!isScoring)
            return;

        timer += Time.deltaTime;

        if (timer >= 1f)
        {
            timer -= 1f;

            score += 1;

            UpdateScoreUI();
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + score.ToString();
        }
    }

    public int GetScore()
    {
        return score;
    }

    public void StopScoring()
    {
        isScoring = false;
    }

    public void StartScoring()
    {
        isScoring = true;
    }

    public void ResetScore()
    {
        score = 0;
        timer = 0f;
        isScoring = true;

        UpdateScoreUI();
    }
}