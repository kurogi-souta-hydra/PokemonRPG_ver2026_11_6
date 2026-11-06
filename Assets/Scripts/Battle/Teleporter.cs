using UnityEngine;
using System.Collections;

public class Teleporter : MonoBehaviour
{
    [Header("出現管理")]
    [SerializeField] private bool isController = false;
    [SerializeField] private GameObject teleporterA;
    [SerializeField] private GameObject teleporterB;

    [Header("移動先のテレポーター")]
    public Transform target;

    private bool isUsed = false;

    private void Start()
    {
        // 出現管理用の場合
        if (isController)
        {
            if (PlayerPrefs.GetInt("TeleportCreated", 0) == 1)
            {
                teleporterA.SetActive(true);
                teleporterB.SetActive(true);

                Debug.Log("テレポーターが出現しました");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isController)
            return;

        if (isUsed || !other.CompareTag("Player"))
            return;

        if (target == null)
            return;

        isUsed = true;

        Debug.Log("テレポーターで移動");

        other.transform.position = target.position;

        isUsed = false;
    }
}
