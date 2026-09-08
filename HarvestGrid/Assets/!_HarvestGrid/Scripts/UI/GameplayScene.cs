using UnityEngine;

public class GameplayScene : MonoBehaviour
{
    [SerializeField]
    private GameplaySceneDataSO gameplaySceneData;
    [SerializeField]
    private GameObject farmPlaceHolder;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instantiate(gameplaySceneData.level.farmMono, farmPlaceHolder.transform);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
