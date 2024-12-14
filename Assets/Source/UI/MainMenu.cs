using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button HostButton;
    public Button JoinButton;
    private Engine engine;

    public void Start()
    {
        engine = GameObject.Find("Engine").GetComponent<Engine>();
        HostButton.onClick.AddListener(OnHostClicked);
        JoinButton.onClick.AddListener(OnJoinClicked);
    }

    private void OnHostClicked()
    {
        GameStartData gameData = new GameStartData();
        gameData.IsHost = true;
        gameData.LevelName = "TestArenaPrefab";
        engine.StartGame(gameData);
    }

    private void OnJoinClicked()
    {
        GameStartData gameData = new GameStartData();
        gameData.IsHost = false;
        engine.StartGame(gameData);
    }
}
