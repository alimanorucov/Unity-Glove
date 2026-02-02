using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button exitButton;

    private bool isPaused = false;

    void Start()
    {
        // Baþlangýçta panel aktif mi kontrol et
        if (pausePanel != null)
        {
            isPaused = pausePanel.activeSelf;
            Time.timeScale = isPaused ? 0f : 1f;
        }

        // Butonlara event ekle
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (exitButton != null)
            exitButton.onClick.AddListener(Exit);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        SetPauseState(isPaused);
    }

    public void Resume()
    {
        SetPauseState(false);
    }

    private void SetPauseState(bool paused)
    {
        isPaused = paused;
        if (pausePanel != null)
            pausePanel.SetActive(paused);

        Time.timeScale = paused ? 0f : 1f;
    }

    public void Exit()
    {
        Application.Quit();
        
    }
}
