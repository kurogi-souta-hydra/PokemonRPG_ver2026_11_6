using UnityEngine;

public class Enemy : MonoBehaviour
{
    public PokemonBase pokemonBase;
    public int level = 5;

    [Header("敵識別ID")]
    public string enemyID;

    void Start()
	{
	    Debug.Log("Enemy Start : " + enemyID);

	    if (PlayerPrefs.GetInt(enemyID, 0) == 1)
	    {
	        Debug.Log("倒した敵なので消します : " + enemyID);
	        gameObject.SetActive(false);
	    }
	}

    public void Die()
    {
        // 倒した記録
        PlayerPrefs.SetInt(enemyID, 1);
        PlayerPrefs.Save();

        // InspectorのチェックをOFFにするのと同じ
        gameObject.SetActive(false);
    }
}