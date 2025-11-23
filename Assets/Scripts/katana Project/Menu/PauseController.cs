using UnityEngine;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject backgroundPanel;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject controlPanel;

    private enum PanelState { None, Menu, Control }
    private PanelState currentState = PanelState.None;

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused)
            {
                ShowMenu();
            }
            else
            {
                if (currentState == PanelState.Menu)
                    Resume();
                else if (currentState == PanelState.Control)
                    BackToMenu();
            }
        }
    }

    public void ShowMenu()
    {
        backgroundPanel.SetActive(true);
        menuPanel.SetActive(true);
        controlPanel.SetActive(false);

        Time.timeScale = 0f;
        isPaused = true;
        currentState = PanelState.Menu;
    }

    public void ShowControlPanel()
    {
        menuPanel.SetActive(false);
        controlPanel.SetActive(true);
        currentState = PanelState.Control;
    }

    public void BackToMenu()
    {
        controlPanel.SetActive(false);
        menuPanel.SetActive(true);
        currentState = PanelState.Menu;
    }

    public void Resume()
    {
        backgroundPanel.SetActive(false);
        menuPanel.SetActive(false);
        controlPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
        currentState = PanelState.None;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
