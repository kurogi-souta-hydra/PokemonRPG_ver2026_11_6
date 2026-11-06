
using UnityEngine;

public class GrayscaleManager : MonoBehaviour
{
    [Header("グレースケールを解除するオブジェクト")]
    [SerializeField] private Renderer[] grayscaleObjects;

    [Header("元のマテリアル")]
    [SerializeField] private Material[] originalMaterials;

    private bool grayscaleDisabled = false;

    private void Start()
    {
        // 保存されている累計撃破数を確認
        int defeatedCount = PlayerPrefs.GetInt("DefeatedCount", 0);

        Debug.Log("現在の累計撃破数: " + defeatedCount);

        // すでに5体以上倒していたら解除
        if (defeatedCount >= 5)
        {
            DisableGrayscale();
        }
    }

    public void EnemyDefeated()
    {
        // BattleSystem側で保存された撃破数を取得
        int defeatedCount = PlayerPrefs.GetInt("DefeatedCount", 0);

        Debug.Log("現在の累計撃破数: " + defeatedCount);

        if (defeatedCount >= 5)
        {
            DisableGrayscale();
        }
    }

    private void DisableGrayscale()
    {
        if (grayscaleDisabled) return;

        for (int i = 0; i < grayscaleObjects.Length; i++)
        {
            if (grayscaleObjects[i] == null)
                continue;

            // マテリアルが1つなら全オブジェクトに適用
            // 4つあるなら各オブジェクトに対応させる
            int materialIndex =
                originalMaterials.Length == 1 ? 0 : i;

            if (materialIndex < originalMaterials.Length &&
                originalMaterials[materialIndex] != null)
            {
                grayscaleObjects[i].sharedMaterial =
                    originalMaterials[materialIndex];
            }
        }

        grayscaleDisabled = true;

        Debug.Log("5体撃破を確認：グレースケールを解除しました");
    }
}
