using LgTyLib.Core;
using LgTyLib.SceneManagement;
using System.Collections.Generic;
using UnityEngine;

public class LevelTreeUI : BaseSingleton<LevelTreeUI>
{
    [SerializeField]
    private List<LevelSO> levelList;

    public void LoadGamePlayScene(string levelID)
    {
        foreach(var level in levelList)
        {
            if(level.levelID == levelID)
            {
                GameManager.Instance.gameplaySceneDataSO.toLoad = false;
                GameManager.Instance.gameplaySceneDataSO.level = level;
                SceneLoader.Instance.LoadScene("GameplayScene");
            }
        }
    }
}