
using UnityEngine;
using TMPro;

public enum GameMode
{
    idle,
    playing,
    levelEnd
}

public class MissionDemolition : MonoBehaviour
{
    static private MissionDemolition S;

    public static bool GAME_OVER { get; private set; }

    private const int MAX_SHOTS = 10;
    public static int SHOTS_LEFT() { return S != null ? S.shotsLeft : 0; }

    [Header("Inscribed")]
    public TextMeshProUGUI uitLevel;
    public TextMeshProUGUI uitShots;
    public Vector3 castlePos;
    public GameObject[] castles;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;

    [Header("Dynamic")]
    public int level;
    public int levelMax;
    public int shotsLeft;
    public GameObject castle;
    public GameMode mode = GameMode.idle;
    public string showing = "Show Slingshot";

    void Start()
    {
        S = this;

        level = 0;
        shotsLeft = MAX_SHOTS;
        levelMax = castles.Length;
        GAME_OVER = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        StartLevel();
    }

    void StartLevel()
    {
        if (GAME_OVER)
            return;

        if (castle != null)
            Destroy(castle);

        Projectile.DESTROY_PROJECTILES();

        castle = Instantiate<GameObject>(castles[level]);
        castle.transform.position = castlePos;

        Goal.goalMet = false;

        UpdateGUI();
        mode = GameMode.playing;

        FollowCam.SWITCH_VIEW(FollowCam.eView.both);
    }

    void UpdateGUI()
    {
        if (uitLevel != null)
            uitLevel.text = "Level: " + (level + 1) + " of " + levelMax;

        if (uitShots != null)
            uitShots.text = "Shots Left: " + shotsLeft;
    }

    void Update()
    {
        UpdateGUI();

        if (GAME_OVER)
            return;

        // Check whether the current castle was cleared first.
        if (mode == GameMode.playing && Goal.goalMet)
        {
            mode = GameMode.levelEnd;

            FollowCam.SWITCH_VIEW(FollowCam.eView.both);

            // Allow the final shot to finish before advancing.
            CancelInvoke(nameof(CheckOutOfShots));
            Invoke(nameof(NextLevel), 2f);
            return;
        }

        // Give the last projectile time to reach its target.
        if (mode == GameMode.playing && shotsLeft <= 0)
        {
            if (!IsInvoking(nameof(CheckOutOfShots)))
                Invoke(nameof(CheckOutOfShots), 3f);
        }
    }

    void CheckOutOfShots()
    {
        if (GAME_OVER || mode != GameMode.playing)
            return;

        if (Goal.goalMet)
        {
            NextLevel();
            return;
        }

        EndGame(
            "GAME OVER!\n" +
            "Out of shots!\n" +
            "You ended on Level " + (level + 1) +
            " of " + levelMax + "."
        );
    }

    void NextLevel()
    {
        if (GAME_OVER)
            return;

        CancelInvoke(nameof(CheckOutOfShots));

        level++;

        // All castles have been completed.
        if (level > levelMax)
        {
            EndGame(
                "YOU WIN!\n" +
                "You conquered all " + levelMax + " levels!"
            );
            return;
        }

        // No shots remain to attempt another castle.
        if (shotsLeft <= 0)
        {
            EndGame(
                "GAME OVER!\n" +
                "You ran out of shots!\n" +
                "You ended on Level " + level +
                " of " + levelMax + "."
            );
            return;
        }

        StartLevel();
    }

    void EndGame(string message)
    {
        GAME_OVER = true;
        mode = GameMode.levelEnd;

        CancelInvoke();

        if (gameOverText != null)
            gameOverText.text = message;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void PlayAgain()
    {
        CancelInvoke();

        GAME_OVER = false;
        level = 0;
        shotsLeft = MAX_SHOTS;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        StartLevel();
    }

    static public void SHOT_FIRED()
    {
        if (S == null || GAME_OVER || S.shotsLeft <= 0)
            return;

        S.shotsLeft--;
        S.UpdateGUI();
    }

    static public GameObject GET_CASTLE()
    {
        return S.castle;
    }
}
