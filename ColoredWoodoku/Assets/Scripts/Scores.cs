using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Scores : MonoBehaviour
{
    [System.Serializable]
    private class BestScoreData
    {
        public int Score;
    }

    private BestScoreData bestScores_ = new BestScoreData();
    private int currentScores_;
    private string bestScoreKey_ = "bsdat";

    public Text scoreText;

    public static Scores Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        
        if (BinaryDataSystem.Exist(bestScoreKey_))
        {
            StartCoroutine(ReadDataFile());
        }
    }

    private IEnumerator ReadDataFile()
    {
        bestScores_ = BinaryDataSystem.Read<BestScoreData>(bestScoreKey_);
        yield return new WaitForEndOfFrame();
    }

    void Start()
    {
        currentScores_ = 0;
        UpdateScoreText();
    }

    private void OnEnable()
    {
        GameEvents.AddScores += AddScores;
        GameEvents.GameOver += SaveBestScore;
    }

    private void OnDisable()
    {
        GameEvents.AddScores -= AddScores;
        GameEvents.GameOver -= SaveBestScore;
    }

    private void AddScores(int score)
    {
        currentScores_ += score;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        scoreText.text = currentScores_.ToString();
    }

    private void SaveBestScore(bool newBestScore)
    {
        BinaryDataSystem.Save<BestScoreData>(bestScores_, bestScoreKey_);
    }

    public bool HasEnoughPoints(int cost)
    {
        return currentScores_ >= cost;
    }

    public void SpendPoints(int points)
    {
        if (HasEnoughPoints(points))
        {
            currentScores_ -= points;
            UpdateScoreText();
        }
    }
}
