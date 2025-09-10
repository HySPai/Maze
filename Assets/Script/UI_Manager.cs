using UnityEngine;
using UnityEngine.UI;

public class UI_Manager : SingletonBehaviour<UI_Manager>
{
    public Button spawnMapButton;
    public Button startButton;

    protected override void Awake()
    {
        MakeSingleton(false);
    }

    void Start()
    {
        spawnMapButton.onClick.AddListener(OnSpawnMapClicked);
        startButton.onClick.AddListener(OnStartClicked);
    }

    void OnSpawnMapClicked()
    {
        GridManager.instance.GenerateGrid();
    }

    void OnStartClicked()
    {
        NPCController npc = FindAnyObjectByType<NPCController>();
        if (npc != null)
        {
            npc.StartMove();
        }
    }
}