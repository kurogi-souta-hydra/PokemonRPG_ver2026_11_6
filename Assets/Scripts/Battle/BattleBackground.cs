using UnityEngine;
using UnityEngine.UI;

public class BattleBackground : MonoBehaviour
{
    [SerializeField] Image backgroundImage;


    [System.Serializable]
    public class BackgroundData
    {
        public string enemyID;
        public Sprite background;
    }


    [SerializeField] BackgroundData[] backgrounds;


    public void SetBackground(string enemyID)
    {
        foreach (var data in backgrounds)
        {
            if(data.enemyID == enemyID)
            {
                backgroundImage.sprite = data.background;
                return;
            }
        }

        Debug.Log("背景が登録されていません EnemyID:" + enemyID);
    }
}